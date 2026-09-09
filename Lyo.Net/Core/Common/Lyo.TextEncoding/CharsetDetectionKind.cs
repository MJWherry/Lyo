namespace Lyo.TextEncoding;

/// <summary>Strategy used to detect a charset.</summary>
public enum CharsetDetectionKind
{
    /// <summary>A byte-order mark matched.</summary>
    Bom = 0,

    /// <summary>Payload looked like valid UTF-8 without a BOM.</summary>
    Utf8Heuristic = 1,

    /// <summary>XML/HTML <c>encoding=</c> / <c>charset=</c> declaration in the text.</summary>
    TextDeclaration = 2,

    /// <summary>Fell back to the configured default charset.</summary>
    Default = 3
}