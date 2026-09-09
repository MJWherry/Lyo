namespace Lyo.Common.Core.Enums;

/// <summary>Optgroup buckets for well-known <see cref="Records.LyoTypeInfo" /> rows in a type picker.</summary>
public enum LyoTypeCategory
{
    /// <summary>CLR type is not known or not registered.</summary>
    Unknown = 0,

    /// <summary>Text-like values: <see cref="string" />, <see cref="Uri" />, SmartFormat templates.</summary>
    Text,

    /// <summary>True/false values.</summary>
    Boolean,

    /// <summary>Integral numbers (byte, short, int, long).</summary>
    Integer,

    /// <summary>Floating-point and decimal numbers.</summary>
    Number,

    /// <summary>Date, time, and duration.</summary>
    Temporal,

    /// <summary>Opaque IDs such as <see cref="Guid" />.</summary>
    Identifier,

    /// <summary>Enums: <see cref="Enum" /> and concrete enum types.</summary>
    Enum,

    /// <summary>JSON documents: <see cref="System.Text.Json.Nodes.JsonObject" />, <see cref="System.Text.Json.Nodes.JsonArray" />, <see cref="System.Text.Json.Nodes.JsonNode" />.</summary>
    Json,

    /// <summary>Typed lists or arrays.</summary>
    Collection,

    /// <summary>Binary payloads (byte arrays and similar).</summary>
    Binary
}
