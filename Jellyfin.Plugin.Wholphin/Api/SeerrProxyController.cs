using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Wholphin.Configuration;
using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Wholphin.Api;

/// <summary>
/// Finds the Seerr account linked to a Jellyfin user, using the admin API key.
/// </summary>
public static class SeerrUsers
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly ConcurrentDictionary<Guid, (int Id, DateTime At)> Cache = new();

    /// <summary>Returns the Seerr user id linked to the Jellyfin user, or null if none (or Seerr is unreachable).</summary>
    public static async Task<int?> FindAsync(SeerrConfig cfg, Guid jellyfinUserId, CancellationToken ct)
    {
        if (Cache.TryGetValue(jellyfinUserId, out var hit) && DateTime.UtcNow - hit.At < TimeSpan.FromMinutes(10))
        {
            return hit.Id;
        }

        try
        {
            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                cfg.ServerUrl!.TrimEnd('/') + "/api/v1/user?take=1000" // ponytail: first 1000 users only, page if a server has more
            );
            req.Headers.Add("X-Api-Key", cfg.Login!.ApiKey);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = await JsonDocument
                .ParseAsync(await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false), cancellationToken: ct)
                .ConfigureAwait(false);
            var want = jellyfinUserId.ToString("N");
            foreach (var u in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                if (
                    u.TryGetProperty("jellyfinUserId", out var j)
                    && j.ValueKind == JsonValueKind.String
                    && string.Equals(j.GetString()?.Replace("-", string.Empty), want, StringComparison.OrdinalIgnoreCase)
                )
                {
                    var id = u.GetProperty("id").GetInt32();
                    Cache[jellyfinUserId] = (id, DateTime.UtcNow);
                    return id;
                }
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            // Treated as "no linked account": the app falls back to the user's own Seerr login.
        }

        return null;
    }
}

/// <summary>
/// Forwards Seerr API calls from Wholphin as the caller's linked Seerr user. The app sends its
/// Jellyfin token as <c>X-Api-Key</c>; the Seerr admin API key never leaves the server.
/// </summary>
[ApiController]
[Route("wholphin/seerr")]
public class SeerrProxyController : ControllerBase
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly IAuthorizationContext authorizationContext;

    public SeerrProxyController(IAuthorizationContext authorizationContext)
    {
        this.authorizationContext = authorizationContext;
    }

    /// <summary>Only the Seerr REST API is reachable, and never outside it.</summary>
    public static bool IsAllowedPath(string? path) =>
        path != null
        && path.StartsWith("api/v1/", StringComparison.Ordinal)
        && !path.Contains("..", StringComparison.Ordinal)
        && path.IndexOfAny(['\\', ':', '#', '@']) < 0;

    // AllowAnonymous: Jellyfin's own auth doesn't read X-Api-Key, so the token is handed to Jellyfin's own auth below.
    [AllowAnonymous]
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("{**path}")]
    public async Task<IActionResult> Proxy(string path)
    {
        var cfg = WholphinPlugin.Instance!.Configuration.SeerrConfig;
        if (
            cfg.Login?.Type != SeerrLoginType.JellyfinAccount
            || string.IsNullOrWhiteSpace(cfg.ServerUrl)
            || string.IsNullOrWhiteSpace(cfg.Login.ApiKey)
        )
        {
            return NotFound();
        }
        if (!IsAllowedPath(path))
        {
            return BadRequest();
        }

        var token = Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        Guid userId;
        try
        {
            // Jellyfin 12 only reads the token from the Authorization header (X-Emby-Token is off by default).
            Request.Headers.Authorization = $"MediaBrowser Token=\"{token}\"";
            var auth = await authorizationContext.GetAuthorizationInfo(HttpContext).ConfigureAwait(false);
            if (auth.IsApiKey || !auth.IsAuthenticated || auth.User is null)
            {
                return Unauthorized();
            }
            userId = auth.User.Id;
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        var seerrUserId = await SeerrUsers.FindAsync(cfg, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (seerrUserId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "No Seerr account is linked to this Jellyfin user.");
        }

        using var req = new HttpRequestMessage(
            new HttpMethod(Request.Method),
            cfg.ServerUrl.TrimEnd('/') + "/" + path + Request.QueryString
        );
        req.Headers.Add("X-Api-Key", cfg.Login.ApiKey);
        req.Headers.Add("X-Api-User", seerrUserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        req.Headers.Accept.ParseAdd("application/json");
        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            req.Content = new StreamContent(Request.Body);
            if (Request.ContentType != null)
            {
                req.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
            }
        }

        try
        {
            using var resp = await Http
                .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            Response.StatusCode = (int)resp.StatusCode;
            if (resp.Content.Headers.ContentType != null)
            {
                Response.ContentType = resp.Content.Headers.ContentType.ToString();
            }
            await resp.Content.CopyToAsync(Response.Body, HttpContext.RequestAborted).ConfigureAwait(false);
            return new EmptyResult();
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Could not reach Seerr.");
        }
    }
}
