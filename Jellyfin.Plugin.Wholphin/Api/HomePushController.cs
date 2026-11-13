using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Wholphin.Api;

/// <summary>
/// Writes the plugin's home settings into every user's server-side display preferences, the same place
/// the Wholphin app's "save to server" uses, so users get them without doing anything in the app.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("wholphin/config/home/push")]
public class HomePushController : ControllerBase
{
    // Where the app reads and writes its server profile: client name, display preferences id and custom pref key.
    private static readonly string[] Clients = ["Wholphin", "Wholphin (Debug)"];
    private const string PreferencesId = "default";
    private const string CustomPrefKey = "home_settings";

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IUserManager userManager;
    private readonly IDisplayPreferencesManager displayPreferences;
    private readonly ILogger<HomePushController> logger;

    public HomePushController(
        IUserManager userManager,
        IDisplayPreferencesManager displayPreferences,
        ILogger<HomePushController> logger
    )
    {
        this.userManager = userManager;
        this.displayPreferences = displayPreferences;
        this.logger = logger;
    }

    /// <summary>Overwrites every user's saved Wholphin home layout with the plugin's settings.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PushResult> Push()
    {
        var settings = WholphinPlugin.Instance!.Configuration.HomeConfig.HomePageSettings;
        var problem = WholphinController.Validate(settings);
        if (problem != null)
        {
            return BadRequest(problem);
        }

        var json = JsonSerializer.Serialize(settings, Json);

        var itemId = PreferencesId.GetMD5();
        var users = 0;
        foreach (var userId in userManager.GetUsersIds())
        {
            foreach (var client in Clients)
            {
                // Setting replaces the whole set for this user/client, so keep the other prefs.
                var prefs = new Dictionary<string, string?>(
                    displayPreferences.ListCustomItemDisplayPreferences(userId, itemId, client)
                )
                {
                    [CustomPrefKey] = json,
                };
                displayPreferences.SetCustomItemDisplayPreferences(userId, itemId, client, prefs);
            }
            users++;
        }

        logger.LogInformation("Pushed Wholphin home settings to {Users} users", users);
        return new PushResult { Users = users };
    }
}

public class PushResult
{
    public int Users { get; set; }
}
