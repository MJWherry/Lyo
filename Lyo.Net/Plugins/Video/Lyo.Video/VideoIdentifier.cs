using System.Diagnostics;

namespace Lyo.Video;

/// <summary>External identifier on a title or episode. Unique per owner table on (scheme, normalized value).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoIdentifier
{
    public Guid Id { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"VideoIdentifier(Scheme={Scheme}, Value=\"{Value}\")";
}
