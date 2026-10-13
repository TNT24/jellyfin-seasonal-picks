namespace Jellyfin.Plugin.SeasonalPicks.Configuration;

/// <summary>How a generated cover is laid out. The defaults give the standard design.</summary>
public class CoverStyle
{
    /// <summary>Big text in the middle. Empty = the season's name.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Small text above the title. Empty = the playlist or library name.</summary>
    public string Label { get; set; } = string.Empty;

    public bool ShowLabel { get; set; } = true;

    /// <summary>Title size in percent of the standard size (50-150).</summary>
    public int TitleSize { get; set; } = 100;

    /// <summary>Top, Middle or Bottom.</summary>
    public string TitlePosition { get; set; } = "Middle";

    public string TitleColor { get; set; } = "#FFFFFF";

    /// <summary>The short line under the title, in a lighter season colour.</summary>
    public bool ShowAccent { get; set; } = true;

    /// <summary>What's behind the decorations: Posters (the picks), Picture (an uploaded background) or Plain (the season colour).</summary>
    public string Background { get; set; } = "Posters";

    /// <summary>Season-colour wash over the background, 0-100.</summary>
    public int ColourWash { get; set; } = 50;

    /// <summary>Darkening over the background, 0-100.</summary>
    public int Darkness { get; set; } = 50;

    /// <summary>Opacity of the decorations, 0-100.</summary>
    public int Decorations { get; set; } = 100;

    /// <summary>Which of the decoration's scenes to draw: 0 = a different one each week of the season, 1+ = always that one.</summary>
    public int Scene { get; set; }
}
