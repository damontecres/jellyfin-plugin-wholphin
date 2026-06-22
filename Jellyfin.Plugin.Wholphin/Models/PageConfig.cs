using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Wholphin.Models;

[JsonConverter(typeof(JsonStringEnumConverter<PagePosition>))]
public enum PagePosition
{
  AfterHome,
  AfterFavorites,
  AfterDiscover,
  AfterLibraries,
  End,
}

public class PagesConfig
{
  [JsonPropertyName("version")]
  public int Version { get; set; } = 1;

  [JsonPropertyName("pages")]
  public List<PageConfig> Pages { get; set; } = new();
}

public class PageConfig
{
  [JsonPropertyName("id")]
  public string Id { get; set; } = default!;

  [JsonPropertyName("title")]
  public string Title { get; set; } = default!;

  // Optional Material-Icon name; client falls back to a default icon when missing or unknown.
  [JsonPropertyName("icon")]
  public string? Icon { get; set; }

  [JsonPropertyName("position")]
  public PagePosition Position { get; set; } = PagePosition.AfterHome;

  [JsonPropertyName("rows")]
  public List<HomeRowConfig> Rows { get; set; } = new();
}
