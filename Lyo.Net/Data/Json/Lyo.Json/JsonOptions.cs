using System.Globalization;
using System.Text;
using Lyo.Common.Core.Enums;

namespace Lyo.Json;

/// <summary>Options for <see cref="JsonService" />. A host <see cref="System.Text.Json.JsonSerializerOptions" /> instance, when supplied, replaces this contract.</summary>
public class JsonOptions
{
    /// <summary>Configuration section name. Nothing in this package binds it.</summary>
    public const string SectionName = "Lyo:Json";

    /// <summary>Property and string-enum naming. Starts as camelCase.</summary>
    public DocumentNaming Naming { get; set; } = DocumentNaming.CamelCase;

    /// <summary>Enum wire format. Starts as names.</summary>
    public DocumentEnumFormat Enums { get; set; } = DocumentEnumFormat.String;

    /// <summary>When true, a JSON string that is a canonical number becomes a number on a <see cref="System.Text.Json.Nodes.JsonNode" /> tree. "01" stays a string. Starts false.</summary>
    public bool ReadNumbersFromStrings { get; set; }

    /// <summary>When true, numbers are written as JSON strings. Starts false.</summary>
    public bool WriteNumbersAsStrings { get; set; }

    /// <summary>When true, the opt-in string scalar converters are registered. Starts false.</summary>
    public bool UseStringScalarConverters { get; set; }

    /// <summary>When true, null properties are omitted. Starts true. <see cref="IgnoreDefaults" /> implies this for that write.</summary>
    public bool IgnoreNulls { get; set; } = true;

    /// <summary>When true, default values (0, false, default enum, default dates) are omitted, and nulls are omitted too. Starts false.</summary>
    public bool IgnoreDefaults { get; set; }

    /// <summary>When true, empty strings are omitted. Starts false.</summary>
    public bool IgnoreEmptyStrings { get; set; }

    /// <summary>When true, empty arrays and empty objects are omitted. Starts false.</summary>
    public bool IgnoreEmptyCollections { get; set; }

    /// <summary>When true, "" read into a string property becomes null. Starts false.</summary>
    public bool EmptyStringAsNull { get; set; }

    /// <summary>When true, property matching ignores case. Starts true.</summary>
    public bool PropertyNameCaseInsensitive { get; set; } = true;

    /// <summary>Unknown JSON properties. Starts as skip.</summary>
    public DocumentUnmappedMembers UnmappedMembers { get; set; } = DocumentUnmappedMembers.Skip;

    /// <summary>Date wire format. Starts as round-trip text.</summary>
    public DocumentDateTimeStyle DateTimeStyle { get; set; } = DocumentDateTimeStyle.Roundtrip;

    /// <summary>Culture for the string converters and Unix dates. Starts as the invariant culture.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>Maximum JSON depth. Starts at 64.</summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>When true, exported JSON is indented. Starts false so API export stays compact.</summary>
    public bool WriteIndented { get; set; }

    /// <summary>When true, a trailing comma is accepted. Starts false.</summary>
    public bool AllowTrailingCommas { get; set; }

    /// <summary>Comment policy. Starts as disallow.</summary>
    public DocumentCommentHandling CommentHandling { get; set; } = DocumentCommentHandling.Disallow;

    /// <summary>Text encoding for file, stream, and byte I/O. Starts as UTF-8 without a BOM. A BOM on read still wins.</summary>
    public Encoding Encoding { get; set; } = new UTF8Encoding(false);

    /// <summary>Property order for a typed object and for a <see cref="System.Text.Json.Nodes.JsonObject" />. Starts as declaration order, which does not sort.</summary>
    public MemberOrder Order { get; set; } = MemberOrder.Defined;

    /// <summary>Written names used when <see cref="Order" /> is <see cref="MemberOrder.Specified" />. Names in this list are written first, in this sequence.</summary>
    public List<string> OrderNames { get; } = [];

    /// <summary>Rejects a null encoding, a null culture, and a depth below 1.</summary>
    public void Validate()
    {
        if (Encoding is null)
            throw new ArgumentNullException(nameof(Encoding));

        if (Culture is null)
            throw new ArgumentNullException(nameof(Culture));

        if (MaxDepth < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxDepth), "MaxDepth must be at least 1.");
    }
}