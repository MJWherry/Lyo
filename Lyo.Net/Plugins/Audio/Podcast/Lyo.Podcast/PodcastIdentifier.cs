using System.Diagnostics;

namespace Lyo.Podcast;

/// <summary>External identifier. Show schemes and episode schemes live on separate tables, each unique on (scheme, normalized value).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastIdentifier
{
    public Guid Id { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"PodcastIdentifier(Scheme={Scheme}, Value=\"{Value}\")";
}
