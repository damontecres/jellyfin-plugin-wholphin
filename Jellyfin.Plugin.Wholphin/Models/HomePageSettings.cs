using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.Wholphin.Models;

[XmlInclude(typeof(NextUp))]
[XmlInclude(typeof(ContinueWatching))]
[XmlInclude(typeof(ContinueWatching))]
[XmlInclude(typeof(NextUp))]
[XmlInclude(typeof(ContinueWatchingCombined))]
[XmlInclude(typeof(RecentlyAdded))]
[XmlInclude(typeof(RecentlyReleased))]
[XmlInclude(typeof(Genres))]
[XmlInclude(typeof(Favorite))]
[XmlInclude(typeof(Recordings))]
[XmlInclude(typeof(TvPrograms))]
[XmlInclude(typeof(TvChannels))]
[XmlInclude(typeof(Suggestions))]
[XmlInclude(typeof(ByParent))]
[XmlInclude(typeof(GetItems))]
public class HomePageSettings
{
  [JsonPropertyName("version")]
  public int Version {get; set;} = 1;

  [JsonPropertyName("rows")]
  public HomeRowConfig[] Rows {get;set;} = [];
}
