using Jellyfin.Plugin.Wholphin.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Wholphin.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public int Version { get; set; } = 1;
    public HomeConfig HomeConfig { get; set; } = new();
    public SeerrConfig SeerrConfig { get; set; } = new();
}

public class HomeConfig
{
    public HomePageSettings HomePageSettings { get; set; } = new();
}

public class SeerrConfig
{
    public string defaultSeerrUrl { get; set; }
}
