using System.Diagnostics;
using Lyo.Video.Enums;

namespace Lyo.Video;

/// <summary>Another name for a title.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoAlternateTitle
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Language { get; set; }

    public VideoAlternateTitleKind Kind { get; set; }

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"VideoAlternateTitle(\"{Title}\", {Kind})";
}
