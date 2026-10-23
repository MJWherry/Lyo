using System.Diagnostics;
using Lyo.Music.Enums;

namespace Lyo.Music;

/// <summary>A credited person on a release or track. <see cref="PersonRef" /> is an opaque EntityRef string, not a People FK.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicCredit
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public MusicCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? PersonRef { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"MusicCredit(Id={Id}, Name=\"{DisplayName}\", Role={Role}, Ordinal={Ordinal})";
}