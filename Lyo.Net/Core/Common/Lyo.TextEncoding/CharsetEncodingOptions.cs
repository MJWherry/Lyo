using System.Text;

namespace Lyo.TextEncoding;

/// <summary>Frozen defaults for <see cref="CharsetEncodingService" />.</summary>
public sealed class CharsetEncodingOptions
{
    /// <summary>Process-wide sensible defaults (<see cref="CharsetEncodingService.Shared" />).</summary>
    public static CharsetEncodingOptions Default { get; } = new();

    /// <summary>Used when callers omit charset / detect falls through.</summary>
    public CharsetInfo DefaultCharset { get; set; } = CharsetInfo.Utf8;

    /// <summary>When true, registers <see cref="CodePagesEncodingProvider" /> on service construction.</summary>
    public bool RegisterCodePages { get; set; } = true;

    /// <summary>Optional encoder fallback; null keeps the BCL default for the resolved encoding.</summary>
    public EncoderFallback? EncoderFallback { get; set; }

    /// <summary>Optional decoder fallback; null keeps the BCL default for the resolved encoding (UTF-8 uses replacement by default).</summary>
    public DecoderFallback? DecoderFallback { get; set; }

    /// <summary>When true, write APIs emit <see cref="System.Text.Encoding.GetPreamble" /> before the body when non-empty.</summary>
    public bool EmitBom { get; set; }
}
