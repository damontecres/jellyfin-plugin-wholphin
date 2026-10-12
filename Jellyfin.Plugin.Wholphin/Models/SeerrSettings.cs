using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Wholphin.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SeerrLoginType>))]
public enum SeerrLoginType
{
    None,
    ApiKey,
    Local,

    /// <summary>
    /// Server-side only: answered as an ApiKey login holding the caller's own Jellyfin token.
    /// </summary>
    JellyfinToken,
}

public class SeerrLogin
{
    [JsonPropertyName("type")]
    public SeerrLoginType Type { get; set; } = SeerrLoginType.None;

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("local")]
    public SeerrLocalLogin? Local { get; set; }
}

public class SeerrLocalLogin
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
