using System.Diagnostics;
using Lyo.Podcast.Enums;

namespace Lyo.Podcast;

/// <summary>A credited person on a show or episode. <see cref="PersonRef" /> is an opaque EntityRef string, not a People FK.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastCredit
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public PodcastCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? PersonRef { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"PodcastCredit(Name=\"{DisplayName}\", Role={Role})";
}