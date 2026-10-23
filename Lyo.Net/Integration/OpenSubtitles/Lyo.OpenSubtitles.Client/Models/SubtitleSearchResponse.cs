namespace Lyo.OpenSubtitles.Client;

/// <summary>Page of subtitle matches from <c>GET /subtitles</c>.</summary>
public sealed class SubtitleSearchResponse
{
    /// <summary>Matching subtitle groups.</summary>
    public IReadOnlyList<SubtitleSearchItem>? Data { get; set; }

    /// <summary>Page number returned by the API.</summary>
    public int Page { get; set; }

    /// <summary>Total matches.</summary>
    public int TotalCount { get; set; }

    /// <summary>Total pages.</summary>
    public int TotalPages { get; set; }
}

/// <summary>One subtitle group. Download ids live on <see cref="Attributes" />.</summary>
public sealed class SubtitleSearchItem
{
    /// <summary>Group attributes, including the files that can be downloaded.</summary>
    public SubtitleAttributes? Attributes { get; set; }
}

/// <summary>Attributes object from a subtitle search row.</summary>
public sealed class SubtitleAttributes
{
    /// <summary>Files in this group. <see cref="SubtitleFile.FileId" /> is the id <c>POST /download</c> expects.</summary>
    public IReadOnlyList<SubtitleFile>? Files { get; set; }
}

/// <summary>One downloadable subtitle file.</summary>
public sealed class SubtitleFile
{
    /// <summary>Id passed to <c>POST /download</c>.</summary>
    public int FileId { get; set; }
}