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
[XmlInclude(typeof(CustomEndpoint))]
public class HomePageSettings
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("rows")]
    public HomeRowConfig[] Rows { get; set; } = [];

    internal void ValidateRanges()
    {
        for (var i = 0; i < Rows.Length; i++)
        {
            if (!Rows[i].TryGetDateRange(out _, out _))
            {
                throw new ArgumentException(
                    $"Home row {i + 1} has an invalid range '{Rows[i].Range}'. "
                        + "Use a date such as oct15 or a range such as oct15-oct31."
                );
            }
        }
    }
}
