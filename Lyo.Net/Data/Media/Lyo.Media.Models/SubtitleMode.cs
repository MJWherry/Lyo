namespace Lyo.Media.Models;

/// <summary>How a subtitle file is applied to a video convert.</summary>
public enum SubtitleMode
{
    /// <summary>No subtitle file.</summary>
    None = 0,

    /// <summary>Burn the subtitle into the picture.</summary>
    BurnIn = 1,

    /// <summary>Copy the subtitle as its own stream.</summary>
    Copy = 2
}