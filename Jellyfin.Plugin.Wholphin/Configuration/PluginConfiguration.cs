using System.Text.Json.Serialization;
using MediaBrowser.Model.Plugins;
using Jellyfin.Plugin.Wholphin.Models;

namespace Jellyfin.Plugin.Wholphin.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
  public int Version { get; set; } = 1;
  public HomeConfig HomeConfig { get; set; } = new();
  public SeerrConfig SeerrConfig { get; set; } = new();

}

public class HomeConfig
{
  public HomePageSettings? HomePageSettings { get; set; }
}

public class SeerrConfig
{
  [JsonPropertyName("version")]
  public int Version { get; set; } = 1;

  [JsonPropertyName("serverUrl")]
  public string? ServerUrl { get; set; }

  [JsonPropertyName("login")]
  public SeerrLogin? Login { get; set; }
}
