namespace Lyo.TextEncoding;

/// <summary>How a charset was detected.</summary>
public enum CharsetDetectionKind
{
    /// <summary>Byte-order mark matched.</summary>
    Bom = 0,

    /// <summary>Payload looked like valid UTF-8 without a BOM.</summary>
    Utf8Heuristic = 1,

    /// <summary>XML/HTML <c>encoding=</c> / <c>charset=</c> declaration in text.</summary>
    TextDeclaration = 2,

    /// <summary>Fell back to configured default charset.</summary>
    Default = 3
}