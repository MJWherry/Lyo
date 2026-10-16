namespace Lyo.OpenSubtitles.Client;

/// <summary>Filename parse from <c>GET /utilities/guessit</c>.</summary>
public sealed class FilenameGuess
{
    /// <summary>Parsed title.</summary>
    public string? Title { get; set; }

    /// <summary>Parsed release year, when the filename has one.</summary>
    public int? Year { get; set; }

    /// <summary>Parsed season, when the filename is an episode.</summary>
    public int? Season { get; set; }

    /// <summary>Parsed episode number, when the filename is an episode.</summary>
    public int? Episode { get; set; }

    /// <summary>Parsed kind, such as <c>movie</c> or <c>episode</c>.</summary>
    public string? Type { get; set; }
}
