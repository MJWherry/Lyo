using System.Diagnostics;

namespace Lyo.Audiobook;

/// <summary>A transcript full-text hit. <see cref="ChapterId" /> is set for a chapter and null for a work-level transcript.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AudiobookTranscriptHit
{
    public Guid WorkId { get; set; }

    public Guid? ChapterId { get; set; }

    public string? Snippet { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"AudiobookTranscriptHit(WorkId={WorkId}, ChapterId={ChapterId})";
}