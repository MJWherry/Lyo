using System.Diagnostics;

namespace Lyo.Video;

/// <summary>An edition of a movie, such as theatrical or director's cut. One cut per title is primary.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoCut
{
    public Guid Id { get; set; }

    public Guid TitleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? RuntimeSeconds { get; set; }

    public string? VideoFileRef { get; set; }

    public bool IsPrimary { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"VideoCut(Name=\"{Name}\", Primary={IsPrimary})";
}
