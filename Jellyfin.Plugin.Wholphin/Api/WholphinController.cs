using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ICU4N.Util;
using Jellyfin.Plugin.Wholphin.Configuration;
using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Wholphin.Api;

/// <summary>
/// API endpoints for Wholphin.
/// </summary>
[ApiController]
[Authorize]
[Route("wholphin")]
public class WholphinController : ControllerBase
{
    private readonly ILogger<WholphinController> logger;
    private readonly ILoggerFactory loggerFactory;

    public WholphinController(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger<WholphinController>();
    }

    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Public()
    {
        // TODO return version?
        return Ok();
    }

    [Authorize]
    [HttpGet("homesettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<HomePageSettings> GetHomeSettings()
    {
        var config = WholphinPlugin.Instance!.Configuration;
        var settings = config.HomeConfig.HomePageSettings;
        if (settings != null)
        {
            // var options = new JsonSerializerOptions {
            //   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // };
            // return new JsonResult(settings, options);
            return settings;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a message for the first row the app would silently skip, or null if all rows are usable.
    /// </summary>
    public static string? Validate(HomePageSettings settings)
    {
        for (var i = 0; i < settings.Rows.Length; i++)
        {
            var row = settings.Rows[i];
            var problem = row switch
            {
                CustomEndpoint c when string.IsNullOrWhiteSpace(c.Title) => "a title is required",
                CustomEndpoint c when string.IsNullOrWhiteSpace(c.Endpoint) => "an endpoint is required",
                CustomEndpoint c when c.Endpoint.Contains("://", StringComparison.Ordinal) =>
                    "the endpoint must be a path relative to the server, like /HomeScreen/Section/Genre",
                GetItems g when string.IsNullOrWhiteSpace(g.Name) => "a row title is required",
                RecentlyAdded { ParentId: var id } when id == Guid.Empty => "choose a library",
                RecentlyReleased { ParentId: var id } when id == Guid.Empty => "choose a library",
                Genres { ParentId: var id } when id == Guid.Empty => "choose a library",
                Studios { ParentId: var id } when id == Guid.Empty => "choose a library",
                Suggestions { ParentId: var id } when id == Guid.Empty => "choose a library",
                ByParent { ParentId: var id } when id == Guid.Empty => "choose a library, collection or playlist",
                _ => null,
            };
            if (problem != null)
            {
                return $"Row {i + 1} ({row.GetType().Name}): {problem}";
            }
        }
        return null;
    }

    [HttpGet("config")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetConfig()
    {
        var config = WholphinPlugin.Instance!.Configuration;
        return Ok(config);
    }

    [HttpGet("config/home")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Response> GetHomeSettingsConfig()
    {
        var config = WholphinPlugin.Instance!.Configuration;
        var settings = config.HomeConfig.HomePageSettings;
        if (settings != null)
        {
            var result = WholphinPlugin.YamlSerializer.Serialize(settings);
            return new Response { Result = result };
        }
        else
        {
            return new Response { };
        }
    }

    [HttpPost("config/home")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Response> SaveHomeSettingsConfig([FromBody, Required] ConfigValue config)
    {
        HomePageSettings p;
        try
        {
            p = WholphinPlugin.YamlDeserializer.Deserialize<HomePageSettings>(config.Value);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error parsing");
            return BadRequest(e.Message);
        }
        var problem = Validate(p);
        if (problem != null)
        {
            return BadRequest(problem);
        }
        var toSave = WholphinPlugin.Instance!.Configuration;
        toSave.HomeConfig.HomePageSettings = p;
        WholphinPlugin.Instance.UpdateConfiguration(toSave);
        return Ok();
    }

    [Authorize]
    [HttpGet("seerrsettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SeerrConfig> GetSeerrSettings()
    {
        var seerr = WholphinPlugin.Instance!.Configuration.SeerrConfig;
        if (string.IsNullOrWhiteSpace(seerr.ServerUrl))
        {
            return NotFound();
        }
        return seerr;
    }

    [HttpGet("config/seerr")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Response> GetSeerrSettingsConfig()
    {
        var seerr = WholphinPlugin.Instance!.Configuration.SeerrConfig;
        var result = WholphinPlugin.YamlSerializer.Serialize(seerr);
        return new Response { Result = result };
    }

    [HttpPost("config/seerr")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Response> SaveSeerrSettingsConfig([FromBody, Required] ConfigValue config)
    {
        SeerrConfig s;
        try
        {
            s = WholphinPlugin.YamlDeserializer.Deserialize<SeerrConfig>(config.Value);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error parsing seerr settings");
            return BadRequest(e.Message);
        }
        var toSave = WholphinPlugin.Instance!.Configuration;
        toSave.SeerrConfig = s;
        WholphinPlugin.Instance.UpdateConfiguration(toSave);
        return Ok();
    }
}

public class ConfigValue
{
    public string Value { get; set; } = default!;
}

public class Response
{
    public string? Error { get; set; } = null;
    public string? Result { get; set; } = null!;
}
