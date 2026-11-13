using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ICU4N.Util;
using Jellyfin.Plugin.Wholphin.Configuration;
using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
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

    private readonly IAuthorizationContext authorizationContext;

    public WholphinController(
        ILoggerFactory loggerFactory,
        IAuthorizationContext authorizationContext
    )
    {
        this.loggerFactory = loggerFactory;
        this.authorizationContext = authorizationContext;
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
            return BadRequest(e.Data.ToString());
        }
        var toSave = WholphinPlugin.Instance!.Configuration;
        toSave.HomeConfig.HomePageSettings = p;
        WholphinPlugin.Instance.UpdateConfiguration(toSave);
        return Ok();
    }

    /// <summary>
    /// Gets the Seerr settings for the calling user. A <c>JellyfinAccount</c> login is answered with an
    /// <c>ApiKey</c> login holding the caller's own Jellyfin token, pointed at this plugin's Seerr proxy.
    /// </summary>
    [Authorize]
    [HttpGet("seerrsettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeerrConfig>> GetSeerrSettings()
    {
        var seerr = WholphinPlugin.Instance!.Configuration.SeerrConfig;
        if (string.IsNullOrWhiteSpace(seerr.ServerUrl))
        {
            return NotFound();
        }

        var login = seerr.Login;
        if (login?.Type == SeerrLoginType.JellyfinAccount)
        {
            var auth = await authorizationContext
                .GetAuthorizationInfo(HttpContext)
                .ConfigureAwait(false);
            if (auth.IsApiKey)
            {
                return Forbid();
            }
            if (!auth.IsAuthenticated || auth.User is null || !auth.HasToken)
            {
                return Unauthorized();
            }

            // Auto-link: only users with a linked Seerr account get logged in, through our proxy
            // with their own Jellyfin token. Everyone else gets no login, so the app falls back to
            // their own saved Seerr login. The admin API key is never returned.
            var linked = await SeerrUsers
                .FindAsync(seerr, auth.User.Id, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            return new SeerrConfig
            {
                Version = seerr.Version,
                ServerUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/wholphin/seerr",
                Login =
                    linked is null
                        ? new SeerrLogin { Type = SeerrLoginType.None }
                        : new SeerrLogin { Type = SeerrLoginType.ApiKey, ApiKey = auth.Token },
            };
        }

        return new SeerrConfig
        {
            Version = seerr.Version,
            ServerUrl = seerr.ServerUrl,
            Login = login,
        };
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
