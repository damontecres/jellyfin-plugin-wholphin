using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Wholphin.Models;

// Defaults match the Wholphin app's, so a partially specified config never produces zero sizes.
// [DefaultValue] keeps YAML export from dropping values that equal the default and then re-importing them as something else.
// Explicit camelCase names and string enums keep the JSON the app reads independent of the server's JSON options.
public record HomeRowViewOptions
{
    [JsonPropertyName("heightDp")]
    [DefaultValue(172)]
    public int HeightDp { get; set; } = 172;

    [JsonPropertyName("spacing")]
    [DefaultValue(16)]
    public int Spacing { get; set; } = 16;

    [JsonPropertyName("contentScale")]
    [DefaultValue(PrefContentScale.FILL)]
    public PrefContentScale ContentScale { get; set; } = PrefContentScale.FILL;

    [JsonPropertyName("aspectRatio")]
    [DefaultValue(AspectRatio.TALL)]
    public AspectRatio AspectRatio { get; set; } = AspectRatio.TALL;

    [JsonPropertyName("imageType")]
    [DefaultValue(ViewOptionImageType.PRIMARY)]
    public ViewOptionImageType ImageType { get; set; } = ViewOptionImageType.PRIMARY;

    [JsonPropertyName("showTitles")]
    public bool ShowTitles { get; set; }

    [JsonPropertyName("useSeries")]
    [DefaultValue(true)]
    public bool UseSeries { get; set; } = true;

    [JsonPropertyName("episodeContentScale")]
    [DefaultValue(PrefContentScale.FILL)]
    public PrefContentScale EpisodeContentScale { get; set; } = PrefContentScale.FILL;

    [JsonPropertyName("episodeAspectRatio")]
    [DefaultValue(AspectRatio.TALL)]
    public AspectRatio EpisodeAspectRatio { get; set; } = AspectRatio.TALL;

    [JsonPropertyName("episodeImageType")]
    [DefaultValue(ViewOptionImageType.PRIMARY)]
    public ViewOptionImageType EpisodeImageType { get; set; } = ViewOptionImageType.PRIMARY;
}

[JsonConverter(typeof(JsonStringEnumConverter<PrefContentScale>))]
public enum PrefContentScale
{
    FIT = 0,
    NONE = 1,
    CROP = 2,
    FILL = 3,
    Fill_WIDTH = 4,
    FILL_HEIGHT = 5,
    UNRECOGNIZED = -1,
}

[JsonConverter(typeof(JsonStringEnumConverter<AspectRatio>))]
public enum AspectRatio
{
    TALL = 0,
    WIDE = 1,
    FOUR_THREE = 2,
    SQUARE = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter<ViewOptionImageType>))]
public enum ViewOptionImageType
{
    PRIMARY = 0,
    THUMB = 1,
}
