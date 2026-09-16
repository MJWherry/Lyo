namespace Lyo.Json;

/// <summary>How property names and string enums are written.</summary>
public enum DocumentNaming
{
    /// <summary>CLR names become camelCase. This is the Lyo HTTP default.</summary>
    CamelCase,

    /// <summary>CLR names are written as declared.</summary>
    PascalCase,

    /// <summary>CLR names become lower_snake_case.</summary>
    SnakeCase,

    /// <summary>CLR names become lower-kebab-case.</summary>
    KebabCase
}

/// <summary>How enum values are written.</summary>
public enum DocumentEnumFormat
{
    /// <summary>Enum names, following <see cref="DocumentNaming" />.</summary>
    String,

    /// <summary>The underlying numeric value.</summary>
    Number
}

/// <summary>What happens when a JSON object has a property the CLR type does not declare.</summary>
public enum DocumentUnmappedMembers
{
    /// <summary>The extra property is ignored.</summary>
    Skip,

    /// <summary>Reading the document throws.</summary>
    Error
}

/// <summary>How <see cref="DateTime" /> and <see cref="DateTimeOffset" /> values are written.</summary>
public enum DocumentDateTimeStyle
{
    /// <summary>ISO-8601 round-trip text.</summary>
    Roundtrip,

    /// <summary>Unix time in milliseconds, as a JSON number.</summary>
    UnixMilliseconds
}

/// <summary>How comments in a JSON document are treated on read.</summary>
public enum DocumentCommentHandling
{
    /// <summary>A comment is a parse error.</summary>
    Disallow,

    /// <summary>Comments are skipped.</summary>
    Skip
}
