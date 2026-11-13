using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Plugin.Wholphin.Models;

public class SortAndDirection
{
    [JsonPropertyName("sort")]
    public ItemSortBy Sort { get; set; }

    [JsonPropertyName("direction")]
    public SortOrder Direction { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ContinueWatching), typeDiscriminator: nameof(ContinueWatching))]
[JsonDerivedType(typeof(NextUp), typeDiscriminator: nameof(NextUp))]
[JsonDerivedType(
    typeof(ContinueWatchingCombined),
    typeDiscriminator: nameof(ContinueWatchingCombined)
)]
[JsonDerivedType(typeof(RecentlyAdded), typeDiscriminator: nameof(RecentlyAdded))]
[JsonDerivedType(typeof(RecentlyReleased), typeDiscriminator: nameof(RecentlyReleased))]
[JsonDerivedType(typeof(Genres), typeDiscriminator: nameof(Genres))]
[JsonDerivedType(typeof(Studios), typeDiscriminator: nameof(Studios))]
[JsonDerivedType(typeof(Favorite), typeDiscriminator: nameof(Favorite))]
[JsonDerivedType(typeof(Recordings), typeDiscriminator: nameof(Recordings))]
[JsonDerivedType(typeof(TvPrograms), typeDiscriminator: nameof(TvPrograms))]
[JsonDerivedType(typeof(TvChannels), typeDiscriminator: nameof(TvChannels))]
[JsonDerivedType(typeof(Suggestions), typeDiscriminator: nameof(Suggestions))]
[JsonDerivedType(typeof(ByParent), typeDiscriminator: nameof(ByParent))]
[JsonDerivedType(typeof(GetItems), typeDiscriminator: nameof(GetItems))]
[JsonDerivedType(typeof(CustomEndpoint), typeDiscriminator: nameof(CustomEndpoint))]
public abstract class HomeRowConfig
{
    // Emitted by the YAML serializer (which has no discriminator support) so rows round-trip with their type.
    // The setter is a no-op: YamlDotNet only serializes read/write properties; JSON uses [JsonPolymorphic].
    [JsonIgnore]
    [XmlIgnore]
    public string Type
    {
        get => GetType().Name;
        set { }
    }

    [JsonPropertyName("viewOptions")]
    public HomeRowViewOptions ViewOptions { get; set; }
}

public class ContinueWatching : HomeRowConfig { }

public class NextUp : HomeRowConfig { }

public class ContinueWatchingCombined : HomeRowConfig { }

public class RecentlyAdded : HomeRowConfig
{
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }
}

public class RecentlyReleased : HomeRowConfig
{
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }
}

public class Genres : HomeRowConfig
{
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }
}

public class Studios : HomeRowConfig
{
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }
}

public class Favorite : HomeRowConfig
{
    [JsonPropertyName("kind")]
    public BaseItemKind Kind { get; set; }
}

public class Recordings : HomeRowConfig { }

public class TvPrograms : HomeRowConfig { }

public class TvChannels : HomeRowConfig { }

public class Suggestions : HomeRowConfig
{
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }
}

public class ByParent : HomeRowConfig
{
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }

    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; }

    [JsonPropertyName("sort")]
    public SortAndDirection? Sort { get; set; }
}

// An arbitrary Jellyfin items query. The filter mirrors the fields of the app's HomeGetItemsFilter;
// the app requires `filter` to be present.
public class GetItems : HomeRowConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("filter")]
    public HomeGetItemsFilter Filter { get; set; } = new();
}

public class HomeGetItemsFilter
{
    [JsonPropertyName("sortBy")]
    public List<ItemSortBy>? SortBy { get; set; }

    [JsonPropertyName("sortOrder")]
    public List<SortOrder>? SortOrder { get; set; }

    [JsonPropertyName("ids")]
    public List<Guid>? Ids { get; set; }

    [JsonPropertyName("minCommunityRating")]
    public double? MinCommunityRating { get; set; }

    [JsonPropertyName("minCriticRating")]
    public double? MinCriticRating { get; set; }

    [JsonPropertyName("recursive")]
    public bool? Recursive { get; set; }

    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }

    [JsonPropertyName("parentId")]
    public Guid? ParentId { get; set; }

    [JsonPropertyName("includeItemTypes")]
    public List<BaseItemKind>? IncludeItemTypes { get; set; }

    [JsonPropertyName("isFavorite")]
    public bool? IsFavorite { get; set; }

    [JsonPropertyName("isPlayed")]
    public bool? IsPlayed { get; set; }

    [JsonPropertyName("officialRatings")]
    public List<string>? OfficialRatings { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("years")]
    public List<int>? Years { get; set; }

    [JsonPropertyName("personIds")]
    public List<Guid>? PersonIds { get; set; }

    [JsonPropertyName("minOfficialRating")]
    public string? MinOfficialRating { get; set; }

    [JsonPropertyName("hasOfficialRating")]
    public bool? HasOfficialRating { get; set; }

    [JsonPropertyName("studioIds")]
    public List<Guid>? StudioIds { get; set; }

    [JsonPropertyName("genreIds")]
    public List<Guid>? GenreIds { get; set; }

    [JsonPropertyName("minPremiereDate")]
    public DateTime? MinPremiereDate { get; set; }

    [JsonPropertyName("maxPremiereDate")]
    public DateTime? MaxPremiereDate { get; set; }
}

public class KeyValueEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = default!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}

// Items are fetched by calling an arbitrary Jellyfin endpoint that returns a
// QueryResult<BaseItemDto>. Enables third-party plugins (e.g. home-sections)
// to inject their own home-screen rows without Wholphin needing to know about
// them.
//
// Headers/Query are List<KeyValueEntry> rather than Dictionary because Jellyfin
// persists plugin config via System.Xml.Serialization, which doesn't support
// IDictionary.
public class CustomEndpoint : HomeRowConfig
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = default!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("headers")]
    public List<KeyValueEntry>? Headers { get; set; }

    [JsonPropertyName("query")]
    public List<KeyValueEntry>? Query { get; set; }
}
