namespace Lyo.TextEncoding;

/// <summary>Binary↔text codec kind for <see cref="IBinaryEncodingService" /> / <see cref="BinaryEncoding" />.</summary>
public enum BinaryEncodingKind
{
    /// <summary>Standard Base64 (RFC 4648 §4) with <c>+</c>/<c>/</c> and <c>=</c> padding.</summary>
    Base64 = 0,

    /// <summary>URL-safe Base64 (RFC 4648 §5) with <c>-</c>/<c>_</c> and no padding.</summary>
    Base64Url = 1,

    /// <summary>Hexadecimal (two chars per byte).</summary>
    Hex = 2
}