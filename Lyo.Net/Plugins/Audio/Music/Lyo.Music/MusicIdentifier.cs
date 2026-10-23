using System.Diagnostics;

namespace Lyo.Music;

/// <summary>Catalog identifier on a release. Unique catalog-wide on (scheme, normalized value).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicIdentifier
{
    public Guid Id { get; set; }

    public Guid ReleaseId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"MusicIdentifier(Scheme={Scheme}, Value=\"{Value}\")";
}