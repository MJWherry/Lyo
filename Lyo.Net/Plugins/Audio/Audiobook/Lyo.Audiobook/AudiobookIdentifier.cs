using System.Diagnostics;

namespace Lyo.Audiobook;

/// <summary>External identifier on a work. Unique on (scheme, normalized value). Chapters have none.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AudiobookIdentifier
{
    public Guid Id { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"AudiobookIdentifier(Scheme={Scheme}, Value=\"{Value}\")";
}