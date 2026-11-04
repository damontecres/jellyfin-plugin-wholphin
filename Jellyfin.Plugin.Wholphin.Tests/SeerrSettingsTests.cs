using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Wholphin.Api;
using Jellyfin.Plugin.Wholphin.Configuration;
using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jellyfin.Plugin.Wholphin.Tests;

public class SeerrSettingsTests
{
    private const string ProxyUrl = "https://jellyfin.example.com/Plugins/SeerrProxy";

    private const string AliceToken = "alice-token";
    private const string BobToken = "bob-token";
    private const string OrphanToken = "orphan-token";
    private const string ServerApiKey = "server-api-key";

    private readonly FakeAuthorizationContext authContext = new();
    private readonly WholphinPlugin plugin;

    public SeerrSettingsTests()
    {
        authContext.AddSession(AliceToken, new User("alice", "provider", "reset"));
        authContext.AddSession(BobToken, new User("bob", "provider", "reset"));
        authContext.AddSession(OrphanToken, null);
        authContext.AddApiKey(ServerApiKey);

        var applicationPaths = Substitute.For<IApplicationPaths>();
        applicationPaths.PluginConfigurationsPath.Returns(
            Path.Combine(Path.GetTempPath(), "wholphin-plugin-tests")
        );
        var xmlSerializer = Substitute.For<IXmlSerializer>();
        xmlSerializer
            .DeserializeFromFile(typeof(PluginConfiguration), Arg.Any<string>())
            .Returns(new PluginConfiguration());
        plugin = new WholphinPlugin(applicationPaths, xmlSerializer);
    }

    [Fact]
    public async Task JellyfinToken_GivesEachUserTheirOwnToken()
    {
        var stored = Configure(JellyfinTokenConfig());

        var aliceController = Controller(AliceToken);
        var alice = (await aliceController.GetSeerrSettings()).Value;
        var bob = (await Controller(BobToken).GetSeerrSettings()).Value;

        AssertApiKeyLogin(alice, AliceToken);
        AssertApiKeyLogin(bob, BobToken);
        Assert.Equal("no-store", aliceController.Response.Headers.CacheControl);
        Assert.Equal("no-cache", aliceController.Response.Headers.Pragma);
        AssertStoredConfigUnchanged(stored);
    }

    [Fact]
    public async Task JellyfinToken_ConcurrentRequestsNeverShareOrLeakTokens()
    {
        var stored = Configure(JellyfinTokenConfig());
        var tokens = Enumerable
            .Range(0, 200)
            .Select(i => i % 2 == 0 ? AliceToken : BobToken)
            .ToList();

        var responses = await Task.WhenAll(
            tokens.Select(token =>
                Task.Run(
                    async () => (token, (await Controller(token).GetSeerrSettings()).Value),
                    TestContext.Current.CancellationToken
                )
            )
        );

        foreach (var (token, response) in responses)
        {
            AssertApiKeyLogin(response, token);
        }
        Assert.Equal(responses.Length, responses.Select(r => r.Value).Distinct().Count());
        AssertStoredConfigUnchanged(stored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("revoked-token")]
    [InlineData(OrphanToken)]
    public async Task JellyfinToken_WithoutValidUserSession_IsUnauthorized(string? token)
    {
        Configure(JellyfinTokenConfig());

        var result = await Controller(token).GetSeerrSettings();

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task JellyfinToken_ServerApiKey_IsForbidden()
    {
        var stored = Configure(JellyfinTokenConfig());

        var result = await Controller(ServerApiKey).GetSeerrSettings();

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Null(result.Value);
        AssertStoredConfigUnchanged(stored);
    }

    [Fact]
    public void SeerrSettings_StillRequiresAnAuthenticatedCaller()
    {
        var method = typeof(WholphinController).GetMethod(
            nameof(WholphinController.GetSeerrSettings)
        )!;

        var authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Null(authorize.Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task JellyfinToken_LooksLikeAnOrdinaryApiKeyLoginOnTheWire()
    {
        Configure(JellyfinTokenConfig());

        var response = (await Controller(AliceToken).GetSeerrSettings()).Value;

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response));
        Assert.Equal(1, json.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(ProxyUrl, json.RootElement.GetProperty("serverUrl").GetString());
        var login = json.RootElement.GetProperty("login");
        Assert.Equal("ApiKey", login.GetProperty("type").GetString());
        Assert.Equal(AliceToken, login.GetProperty("apiKey").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(SeerrLoginType.None)]
    [InlineData(SeerrLoginType.ApiKey)]
    [InlineData(SeerrLoginType.Local)]
    public async Task OtherLoginTypes_ReturnTheStoredConfigAsBefore(SeerrLoginType? type)
    {
        var stored = Configure(
            new SeerrConfig
            {
                ServerUrl = "https://seerr.example.com",
                Login = type switch
                {
                    null => null,
                    SeerrLoginType.ApiKey => new SeerrLogin
                    {
                        Type = SeerrLoginType.ApiKey,
                        ApiKey = "static-seerr-key",
                    },
                    SeerrLoginType.Local => new SeerrLogin
                    {
                        Type = SeerrLoginType.Local,
                        Local = new SeerrLocalLogin { Username = "household", Password = "pw" },
                    },
                    _ => new SeerrLogin { Type = type.Value },
                },
            }
        );
        var controller = Controller(AliceToken);

        var result = await controller.GetSeerrSettings();

        Assert.Same(stored, result.Value);
        Assert.Equal(0, authContext.Calls);
        Assert.False(controller.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", SeerrLoginType.ApiKey)]
    [InlineData(null, SeerrLoginType.JellyfinToken)]
    [InlineData("  ", SeerrLoginType.JellyfinToken)]
    public async Task MissingServerUrl_IsStillNotFound(string? serverUrl, SeerrLoginType? type)
    {
        Configure(
            new SeerrConfig
            {
                ServerUrl = serverUrl,
                Login = type is null ? null : new SeerrLogin { Type = type.Value },
            }
        );

        var result = await Controller(AliceToken).GetSeerrSettings();

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(0, authContext.Calls);
    }

    [Fact]
    public async Task AdminExports_KeepTheMarkerAndNeverAResolvedToken()
    {
        Configure(JellyfinTokenConfig());
        await Controller(AliceToken).GetSeerrSettings();

        var yaml = Controller(null).GetSeerrSettingsConfig().Value?.Result;
        var config = Assert.IsType<OkObjectResult>(Controller(null).GetConfig()).Value;
        var json = JsonSerializer.Serialize(config);

        Assert.Contains("type: JellyfinToken", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain(AliceToken, yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey", yaml, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"JellyfinToken\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain(AliceToken, json, StringComparison.Ordinal);
    }

    [Fact]
    public void YamlImport_RoundTripsTheMarker()
    {
        const string Yaml = """
            version: 1
            serverUrl: https://jellyfin.example.com/Plugins/SeerrProxy
            login:
              type: JellyfinToken

            """;
        var controller = Controller(null);

        var saved = controller.SaveSeerrSettingsConfig(new ConfigValue { Value = Yaml });

        Assert.IsType<OkResult>(saved.Result);
        var stored = plugin.Configuration.SeerrConfig;
        Assert.Equal(ProxyUrl, stored.ServerUrl);
        Assert.Equal(SeerrLoginType.JellyfinToken, stored.Login?.Type);
        Assert.Null(stored.Login?.ApiKey);
        Assert.Equal(
            Yaml.ReplaceLineEndings("\n"),
            controller.GetSeerrSettingsConfig().Value?.Result?.ReplaceLineEndings("\n")
        );
    }

    [Fact]
    public void XmlPersistence_RoundTripsTheMarker()
    {
        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var writer = new StringWriter();
        serializer.Serialize(
            writer,
            new PluginConfiguration { SeerrConfig = JellyfinTokenConfig() }
        );

        using var reader = new StringReader(writer.ToString());
        var loaded = Assert.IsType<PluginConfiguration>(serializer.Deserialize(reader));

        Assert.Equal(SeerrLoginType.JellyfinToken, loaded.SeerrConfig.Login?.Type);
        Assert.Null(loaded.SeerrConfig.Login?.ApiKey);
    }

    private static SeerrConfig JellyfinTokenConfig() =>
        new()
        {
            ServerUrl = ProxyUrl,
            Login = new SeerrLogin { Type = SeerrLoginType.JellyfinToken },
        };

    private SeerrConfig Configure(SeerrConfig seerr)
    {
        plugin.Configuration.SeerrConfig = seerr;
        return seerr;
    }

    private WholphinController Controller(string? token)
    {
        var httpContext = new DefaultHttpContext();
        if (token is not null)
        {
            httpContext.Request.Headers.Authorization = $"MediaBrowser Token=\"{token}\"";
        }

        return new WholphinController(NullLoggerFactory.Instance, authContext)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private static void AssertApiKeyLogin(SeerrConfig? response, string token)
    {
        Assert.NotNull(response);
        Assert.Equal(1, response.Version);
        Assert.Equal(ProxyUrl, response.ServerUrl);
        Assert.Equal(SeerrLoginType.ApiKey, response.Login?.Type);
        Assert.Equal(token, response.Login?.ApiKey);
        Assert.Null(response.Login?.Local);
    }

    private void AssertStoredConfigUnchanged(SeerrConfig stored)
    {
        Assert.Same(stored, plugin.Configuration.SeerrConfig);
        Assert.Equal(SeerrLoginType.JellyfinToken, stored.Login?.Type);
        Assert.Null(stored.Login?.ApiKey);
        Assert.Null(stored.Login?.Local);
    }

    private sealed class FakeAuthorizationContext : IAuthorizationContext
    {
        private const string Prefix = "MediaBrowser Token=\"";
        private readonly Dictionary<string, User?> sessions = [];
        private readonly HashSet<string> apiKeys = [];
        private int calls;

        public int Calls => calls;

        public void AddSession(string token, User? user) => sessions[token] = user;

        public void AddApiKey(string key) => apiKeys.Add(key);

        public Task<AuthorizationInfo> GetAuthorizationInfo(HttpContext requestContext) =>
            GetAuthorizationInfo(requestContext.Request);

        public Task<AuthorizationInfo> GetAuthorizationInfo(HttpRequest requestContext)
        {
            Interlocked.Increment(ref calls);
            var header = requestContext.Headers.Authorization.ToString();
            var token = header.StartsWith(Prefix, StringComparison.Ordinal)
                ? header[Prefix.Length..^1]
                : null;

            var info = new AuthorizationInfo { Token = token };
            if (token is not null && sessions.TryGetValue(token, out var user))
            {
                info.IsAuthenticated = true;
                info.User = user;
            }
            else if (token is not null && apiKeys.Contains(token))
            {
                info.IsAuthenticated = true;
                info.IsApiKey = true;
            }
            return Task.FromResult(info);
        }
    }
}
