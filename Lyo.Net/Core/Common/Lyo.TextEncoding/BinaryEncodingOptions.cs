using Lyo.Common.Enums;

namespace Lyo.TextEncoding;

/// <summary>Frozen defaults for <see cref="BinaryEncodingService" />.</summary>
public sealed class BinaryEncodingOptions
{
    /// <summary>Process-wide sensible defaults (<see cref="BinaryEncodingService.Shared" />).</summary>
    public static BinaryEncodingOptions Default { get; } = new();

    /// <summary>Default letter casing when emitting hex from encode helpers.</summary>
    public TextLetterCase DefaultHexLetterCase { get; set; } = TextLetterCase.Upper;

    /// <summary>
    /// When greater than zero, Base64/Base64Url encode output is wrapped with CRLF every N characters (e.g. 76 for MIME).
    /// Hex is never wrapped. Zero means no wrapping.
    /// </summary>
    public int LineLength { get; set; }
}
