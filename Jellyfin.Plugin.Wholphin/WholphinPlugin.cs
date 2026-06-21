using MediaBrowser.Common.Plugins;
using Jellyfin.Plugin.Wholphin.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Runtime.Serialization;
using Jellyfin.Plugin.Wholphin.Models;

namespace Jellyfin.Plugin.Wholphin;

public class WholphinPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
  public override string Name => "Wholphin";
  public override Guid Id => Guid.Parse("00E68C90-8840-48D0-AD65-0A5D58249652");

  public static WholphinPlugin? Instance { get; private set; }

  private static string? ns;

  // private readonly ILogger<WholphinPlugin> _logger;

  public static ISerializer YamlSerializer { get; private set; }

  public static IDeserializer YamlDeserializer { get; private set; }

  public WholphinPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
  {
    // _logger=logger;
    Instance = this;
    ns = GetType().Namespace;
    // _logger.LogError("Wholphin test logging");
    YamlSerializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull)
      .Build();
    YamlDeserializer = new DeserializerBuilder()
      .IgnoreUnmatchedProperties()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .WithTypeDiscriminatingNodeDeserializer(options=>
      {
        options.AddKeyValueTypeDiscriminator<HomeRowConfig>("type", new Dictionary<string, Type>()
        {
          ["NextUp"] = typeof(NextUp),
          ["ContinueWatching"] = typeof(ContinueWatching),
          ["ContinueWatchingCombined"] = typeof(ContinueWatchingCombined),
          ["RecentlyAdded"] = typeof(RecentlyAdded),
          ["RecentlyReleased"] = typeof(RecentlyReleased),
          ["Genres"] = typeof(Genres),
          ["Favorite"] = typeof(Favorite),
          ["Recordings"] = typeof(Recordings),
          ["TvPrograms"] = typeof(TvPrograms),
          ["TvChannels"] = typeof(TvChannels),
          ["Suggestions"] = typeof(Suggestions),
          ["ByParent"] = typeof(ByParent),
          ["GetItems"] = typeof(GetItems),
          ["CustomEndpoint"] = typeof(CustomEndpoint),
        });
      }
      )
      .Build();
  }

  /// <inheritdoc />
  public IEnumerable<PluginPageInfo> GetPages()
  {
    return new List<PluginPageInfo> {
      new() { Name = Name, EmbeddedResourcePath = ns+".Pages.HomeSettings.index.html" },
    };
  }
}
