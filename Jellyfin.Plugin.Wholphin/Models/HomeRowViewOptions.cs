namespace Jellyfin.Plugin.Wholphin.Models;

public record HomeRowViewOptions
{
  public int HeightDp { get; set; }

  public int Spacing { get; set; }

  public PrefContentScale ContentScale { get; set; }

  public AspectRatio AspectRatio { get; set; }

  public ViewOptionImageType ImageType { get; set; }

  public bool ShowTitles { get; set; }

  public bool UseSeries { get; set; }

  public PrefContentScale EpisodeContentScale { get; set; }

  public AspectRatio EpisodeAspectRatio { get; set; }

  public ViewOptionImageType EpisodeImageType { get; set; }


}

public enum PrefContentScale
{
  FIT = 0,
  NONE = 1,
  CROP = 2,
  FILL = 3,
  Fill_WIDTH = 4,
  FILL_HEIGHT = 5,
  UNRECOGNIZED = -1
}

public enum AspectRatio
{
  TALL = 0,
  WIDE = 1,
  FOUR_THREE = 2,
  SQUARE = 3
}

public enum ViewOptionImageType
{
  PRIMARY = 0,
  THUMB = 1
}
