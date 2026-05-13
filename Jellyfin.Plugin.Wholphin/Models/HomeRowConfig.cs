
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Plugin.Wholphin.Models;

public class SortAndDirection
{
  [JsonPropertyName("sort")]
  public ItemSortBy Sort{ get; set; }

  [JsonPropertyName("direction")]
  public SortOrder Direction { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ContinueWatching), typeDiscriminator: nameof(ContinueWatching))]
[JsonDerivedType(typeof(NextUp), typeDiscriminator: nameof(NextUp))]
[JsonDerivedType(typeof(ContinueWatchingCombined), typeDiscriminator: nameof(ContinueWatchingCombined))]
[JsonDerivedType(typeof(RecentlyAdded), typeDiscriminator: nameof(RecentlyAdded))]
[JsonDerivedType(typeof(RecentlyReleased), typeDiscriminator: nameof(RecentlyReleased))]
[JsonDerivedType(typeof(Genres), typeDiscriminator: nameof(Genres))]
[JsonDerivedType(typeof(Favorite), typeDiscriminator: nameof(Favorite))]
[JsonDerivedType(typeof(Recordings), typeDiscriminator: nameof(Recordings))]
[JsonDerivedType(typeof(TvPrograms), typeDiscriminator: nameof(TvPrograms))]
[JsonDerivedType(typeof(TvChannels), typeDiscriminator: nameof(TvChannels))]
[JsonDerivedType(typeof(Suggestions), typeDiscriminator: nameof(Suggestions))]
[JsonDerivedType(typeof(ByParent), typeDiscriminator: nameof(ByParent))]
[JsonDerivedType(typeof(GetItems), typeDiscriminator: nameof(GetItems))]
public abstract class HomeRowConfig
{
  public string type => this.GetType().Name;

  [JsonPropertyName("viewOptions")]
  public HomeRowViewOptions ViewOptions { get; set; }
}

public class ContinueWatching : HomeRowConfig
{

}

public class NextUp : HomeRowConfig
{

}

public class ContinueWatchingCombined : HomeRowConfig
{

}

public class RecentlyAdded : HomeRowConfig
{
  [JsonPropertyName("parentId")]
  public Guid ParentId {get; set;}
}

public class RecentlyReleased : HomeRowConfig
{
  [JsonPropertyName("parentId")]
  public Guid ParentId {get; set;}
}

public class Genres : HomeRowConfig
{
  [JsonPropertyName("parentId")]
  public Guid ParentId {get; set;}
}

public class Favorite : HomeRowConfig
{
  [JsonPropertyName("kind")]
  public BaseItemKind Kind {get; set;}
}

public class Recordings : HomeRowConfig
{

}

public class TvPrograms : HomeRowConfig
{
}
public class TvChannels : HomeRowConfig
{
}
public class Suggestions : HomeRowConfig
{
  [JsonPropertyName("parentId")]
  public Guid ParentId {get; set;}
}

public class ByParent : HomeRowConfig
{
  [JsonPropertyName("parentId")]
  public Guid ParentId {get; set;}
  [JsonPropertyName("recursive")]
  public bool Recursive {get; set;}

  [JsonPropertyName("sort")]
  public SortAndDirection? Sort {get; set;}
}

// TODO
public class GetItems : HomeRowConfig
{
  [JsonPropertyName("name")]
  public String Name {get; set;}
  // public GetItemsRequest getItems {get; set;}
}
