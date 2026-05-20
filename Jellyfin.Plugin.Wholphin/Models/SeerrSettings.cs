using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Wholphin.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SeerrLoginType>))]
public enum SeerrLoginType
{
    None,
    ApiKey,
    Jellyfin,
    Local,
}

public class SeerrLogin
{
    [JsonPropertyName("type")]
    public SeerrLoginType Type { get; set; } = SeerrLoginType.None;

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("jellyfin")]
    public SeerrJellyfinLogin? Jellyfin { get; set; }

    [JsonPropertyName("local")]
    public SeerrLocalLogin? Local { get; set; }
}

public class SeerrJellyfinLogin
{
    [JsonPropertyName("useCurrentUser")]
    public bool UseCurrentUser { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

public class SeerrLocalLogin
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
