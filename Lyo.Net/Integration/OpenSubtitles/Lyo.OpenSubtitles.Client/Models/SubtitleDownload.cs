namespace Lyo.OpenSubtitles.Client;

/// <summary>Temporary download link from <c>POST /download</c>. Fetch <see cref="Link" /> yourself; that host must not receive the API key.</summary>
public sealed class SubtitleDownload
{
    /// <summary>Temporary file URL, typically on <c>dl.opensubtitles.com</c>.</summary>
    public string? Link { get; set; }

    /// <summary>File name suggested by the API.</summary>
    public string? FileName { get; set; }

    /// <summary>Downloads already used in the current quota window.</summary>
    public int Requests { get; set; }

    /// <summary>Downloads left in the current quota window.</summary>
    public int Remaining { get; set; }
}