using Lyo.Common.Core.Enums;

namespace Lyo.TextEncoding;

/// <summary>Fixed defaults for <see cref="BinaryEncodingService" />.</summary>
public sealed class BinaryEncodingOptions
{
    /// <summary>Process-wide defaults used by <see cref="BinaryEncodingService.Shared" />.</summary>
    public static BinaryEncodingOptions Default { get; } = new();

    /// <summary>Letter casing used when encode helpers emit hex.</summary>
    public TextLetterCase DefaultHexLetterCase { get; set; } = TextLetterCase.Upper;

    /// <summary>
    /// When greater than zero, Base64/Base64Url encode output is wrapped with CRLF every N characters (for example 76 for MIME). Hex is never wrapped. Zero means no
    /// wrapping.
    /// </summary>
    public int LineLength { get; set; }
}