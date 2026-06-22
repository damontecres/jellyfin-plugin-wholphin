using MediaBrowser.Model.Plugins;
using Jellyfin.Plugin.Wholphin.Models;

namespace Jellyfin.Plugin.Wholphin.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
  public int Version { get; set; } = 1;
  public HomeConfig HomeConfig { get; set; } = new();
  public SeerrConfig SeerrConfig { get; set; } = new();
  public PagesConfig PagesConfig { get; set; } = new();
}

public class HomeConfig
{
  public HomePageSettings? HomePageSettings { get; set; }
}

public class SeerrConfig
{
  public string defaultSeerrUrl { get; set; }
}
