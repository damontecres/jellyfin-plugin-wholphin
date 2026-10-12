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
using MediaBrowser.Controller;
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
    // Jellyfin tokens are only provided for the SeerrProxy plugin's endpoint on this server
    private const string SeerrProxyPath = "/Plugins/SeerrProxy";

    private readonly ILogger<WholphinController> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly IAuthorizationContext authorizationContext;
    private readonly IServerApplicationHost serverApplicationHost;

    public WholphinController(
        ILoggerFactory loggerFactory,
        IAuthorizationContext authorizationContext,
        IServerApplicationHost serverApplicationHost
    )
    {
        this.loggerFactory = loggerFactory;
        this.authorizationContext = authorizationContext;
        this.serverApplicationHost = serverApplicationHost;
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
        if (seerr.Login?.Type != SeerrLoginType.JellyfinToken)
        {
            return seerr;
        }

        // Only a token that Jellyfin validated for a user is handed out, never a server API key
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

        var jellyfinUrls = GetJellyfinUrls();
        if (!IsSeerrProxyUrl(seerr.ServerUrl, jellyfinUrls))
        {
            logger.LogWarning(
                "Not providing a Jellyfin token for Seerr: serverUrl must be this server's SeerrProxy endpoint, e.g. {Expected}",
                string.Join(
                    " or ",
                    jellyfinUrls.Select(url => url.TrimEnd('/') + SeerrProxyPath).Distinct()
                )
            );
            return Forbid();
        }

        // The response holds the caller's own token, so it is built per request and never cached
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return new SeerrConfig
        {
            Version = seerr.Version,
            ServerUrl = seerr.ServerUrl,
            Login = new SeerrLogin { Type = SeerrLoginType.ApiKey, ApiKey = auth.Token },
        };
    }

    // This server's URL as the client reached it (Jellyfin has already applied its known-proxy
    // forwarded headers and maps its base URL into PathBase), and as Jellyfin advertises itself
    // to this client (published server URL)
    private List<string> GetJellyfinUrls()
    {
        var urls = new List<string>();
        if (Request.Host.HasValue)
        {
            urls.Add(
                $"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.PathBase.ToUriComponent()}"
            );
        }
        urls.Add(serverApplicationHost.GetSmartApiUrl(Request));
        return urls;
    }

    // Whether serverUrl is exactly the SeerrProxy endpoint under one of the Jellyfin URLs
    private static bool IsSeerrProxyUrl(string serverUrl, IEnumerable<string> jellyfinUrls)
    {
        // Refuse anything a client's URL parser might read differently than System.Uri does,
        // e.g. "\", user info, encoded dot segments ("%2e%2e") or embedded whitespace
        if (
            serverUrl.Any(c => c is '\\' or '@' or '%' || char.IsWhiteSpace(c) || char.IsControl(c))
            || !Uri.TryCreate(serverUrl, UriKind.Absolute, out var target)
            || (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps)
            || target.Query.Length > 0
            || target.Fragment.Length > 0
        )
        {
            return false;
        }

        return jellyfinUrls.Any(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var jellyfin)
            && target.Scheme == jellyfin.Scheme
            && string.Equals(target.IdnHost, jellyfin.IdnHost, StringComparison.OrdinalIgnoreCase)
            && target.Port == jellyfin.Port
            && string.Equals(
                target.AbsolutePath.TrimEnd('/'),
                jellyfin.AbsolutePath.TrimEnd('/') + SeerrProxyPath,
                StringComparison.Ordinal
            )
        );
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
