namespace Lyo.Common.Core.Enums;

/// <summary>Editor widget hint for a <see cref="Records.LyoTypeInfo" /> value (switch, numeric, picker, JSON tree, and similar).</summary>
public enum LyoTypeEditorKind
{
    /// <summary>Free-text field (strings and unknown scalars).</summary>
    Text = 0,

    /// <summary>On/off switch or checkbox.</summary>
    Boolean,

    /// <summary>Integer field (byte, short, int, long).</summary>
    Integer,

    /// <summary>Decimal or floating-point field.</summary>
    Decimal,

    /// <summary>Date picker plus time picker.</summary>
    DateTime,

    /// <summary>Picker for a calendar date only.</summary>
    DateOnly,

    /// <summary>Picker for time of day only.</summary>
    TimeOnly,

    /// <summary><see cref="TimeSpan" /> / duration text.</summary>
    Duration,

    /// <summary>GUID text that is parsed.</summary>
    Guid,

    /// <summary>URI text that is parsed.</summary>
    Uri,

    /// <summary>Enum name or its numeric value.</summary>
    Enum,

    /// <summary>Tree editor for a JSON object.</summary>
    JsonObject,

    /// <summary>Tree editor for a JSON array.</summary>
    JsonArray,

    /// <summary>Typed collection: JSON array of <see cref="Records.LyoTypeInfo.ElementType" />.</summary>
    Collection,

    /// <summary>Binary payload, usually base64 JSON.</summary>
    Binary,

    /// <summary>Regex pattern stored as a JSON string.</summary>
    Regex,

    /// <summary>XML document stored as a JSON string of markup.</summary>
    Xml,

    /// <summary>SmartFormat template as a JSON string (hint for registered non-CLR formatter types).</summary>
    Formatter
}
