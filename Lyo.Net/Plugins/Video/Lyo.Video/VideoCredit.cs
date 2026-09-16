using System.Diagnostics;
using Lyo.Video.Enums;

namespace Lyo.Video;

/// <summary>A credited person on a title or episode. <see cref="PersonRef" /> is an opaque EntityRef string, not a People FK.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoCredit
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public VideoCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? CharacterName { get; set; }

    public string? PersonRef { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"VideoCredit(Name=\"{DisplayName}\", Role={Role})";
}
