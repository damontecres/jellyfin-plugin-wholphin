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

  public WholphinController(
    ILoggerFactory loggerFactory
  )
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

  [Authorize]
  [HttpGet("pages")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public ActionResult<PagesResponse> GetPages()
  {
    var pages = WholphinPlugin.Instance!.Configuration.PagesConfig.Pages;
    return Ok(new PagesResponse
    {
      Pages = pages.Select(p => new PageSummary
      {
        Id = p.Id,
        Title = p.Title,
        Icon = p.Icon,
        Position = p.Position,
      }),
    });
  }

  [Authorize]
  [HttpGet("pages/{id}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public ActionResult<PageConfig> GetPage([FromRoute, Required] string id)
  {
    var page = WholphinPlugin.Instance!.Configuration.PagesConfig.Pages.FirstOrDefault(p => p.Id == id);
    if (page == null)
    {
      return NotFound();
    }
    return page;
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
  public ActionResult<Response> SaveHomeSettingsConfig(
    [FromBody, Required] ConfigValue config
  )
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

// Lightweight metadata for the page list endpoint; clients use this to build
// nav-drawer entries before opening the page itself (which loads the full
// PageConfig including rows).
public class PagesResponse
{
  [JsonPropertyName("pages")]
  public IEnumerable<PageSummary> Pages { get; set; } = Array.Empty<PageSummary>();
}

public class PageSummary
{
  [JsonPropertyName("id")]
  public string Id { get; set; } = default!;

  [JsonPropertyName("title")]
  public string Title { get; set; } = default!;

  [JsonPropertyName("icon")]
  public string? Icon { get; set; }

  [JsonPropertyName("position")]
  public PagePosition Position { get; set; }
}
