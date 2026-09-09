using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Exceptions;

namespace Lyo.Common.Metadata.Records;

/// <summary>
/// Type catalog used by pickers and value editors (config, job parameters, filters). Most rows are CLR types
/// (<see cref="IsClr" />). Other packages add non-CLR rows (for example <c>Lyo.Formatter</c> templates) through <see cref="Register" />.
/// </summary>
/// <remarks>
/// <para>
/// Stored names use <see cref="Type.FullName" /> for CLR types (same convention as config <c>ForValueType</c>), or a package-owned name for non-CLR types
/// (for example <c>Lyo.Formatter.Template</c>). Resolve by short name (<c>int</c>), alias (<c>Int32</c>), or full name. Concrete enums are not listed;
/// <see cref="FromType" /> maps any <c>type.IsEnum</c> to <see cref="Enum" />. The stored FullName for an enum stays the concrete type, not <c>System.Enum</c>.
/// </para>
/// <para>Implicitly converts to <see cref="Type" /> (the JSON/runtime type; for non-CLR rows this is typically <see cref="string" />).</para>
/// </remarks>
/// <param name="ShortName">UI label (for example <c>string</c>, <c>int</c>, <c>JSON object</c>).</param>
/// <param name="FullName">Stored name: CLR <see cref="Type.FullName" /> when <see cref="IsClr" />, otherwise a package-owned identifier.</param>
/// <param name="Description">Human-readable summary for tooltips and docs.</param>
/// <param name="Type">
/// Runtime/JSON type. For <see cref="Enum" /> this is <see cref="System.Enum" />. For non-CLR rows, the JSON payload type (usually <see cref="string" />).
/// </param>
/// <param name="Category">Picker group.</param>
/// <param name="EditorKind">Which input widget to render.</param>
/// <param name="IsScalar">False for JSON documents, collections, and binary payloads that need a JSON editor.</param>
/// <param name="DefaultJson">Empty/zero JSON payload for new values (for example <c>0</c>, <c>false</c>, <c>""</c>, <c>{}</c>, <c>[]</c>).</param>
/// <param name="Aliases">Extra lookup names (C# keywords and <see cref="Type.Name" /> are also registered for CLR rows).</param>
/// <param name="ElementType">Element type for collections; otherwise <see langword="null" />.</param>
/// <param name="IsClr">True when <see cref="FullName" /> is a real CLR <see cref="Type.FullName" />.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoTypeInfo(
    string ShortName,
    string FullName,
    string Description,
    Type Type,
    LyoTypeCategory Category,
    LyoTypeEditorKind EditorKind,
    bool IsScalar,
    string DefaultJson,
    string[] Aliases,
    Type? ElementType,
    bool IsClr = true)
{
    /// <summary>Sentinel for an unrecognized or unregistered CLR type.</summary>
    public static readonly LyoTypeInfo Unknown = Create(
        "unknown", typeof(object), "Unknown or unregistered CLR type", LyoTypeCategory.Unknown, LyoTypeEditorKind.Text, "null", isScalar: false, aliases: ["unknown", "unspecified"]);

    /// <summary><see cref="System.String" />.</summary>
    public static readonly LyoTypeInfo String = Create("string", typeof(string), "UTF-16 string", LyoTypeCategory.Text, LyoTypeEditorKind.Text, "\"\"");

    /// <summary><see cref="System.Uri" />.</summary>
    public static readonly LyoTypeInfo Uri = Create(
        "Uri", typeof(Uri), "Absolute or relative URI", LyoTypeCategory.Text, LyoTypeEditorKind.Uri, "\"http://localhost\"", aliases: ["uri", "url"]);

    /// <summary><see cref="System.Boolean" />.</summary>
    public static readonly LyoTypeInfo Bool = Create(
        "bool", typeof(bool), "Boolean true/false", LyoTypeCategory.Boolean, LyoTypeEditorKind.Boolean, "false", aliases: ["boolean", "Bool"]);

    /// <summary><see cref="System.Byte" />.</summary>
    public static readonly LyoTypeInfo Byte = Create("byte", typeof(byte), "Unsigned 8-bit integer", LyoTypeCategory.Integer, LyoTypeEditorKind.Integer, "0");

    /// <summary><see cref="System.Int16" />.</summary>
    public static readonly LyoTypeInfo Short = Create("short", typeof(short), "Signed 16-bit integer", LyoTypeCategory.Integer, LyoTypeEditorKind.Integer, "0", aliases: ["int16"]);

    /// <summary><see cref="System.Int32" />.</summary>
    public static readonly LyoTypeInfo Int = Create(
        "int", typeof(int), "Signed 32-bit integer", LyoTypeCategory.Integer, LyoTypeEditorKind.Integer, "0", aliases: ["int32", "Int"]);

    /// <summary><see cref="System.Int64" />.</summary>
    public static readonly LyoTypeInfo Long = Create(
        "long", typeof(long), "Signed 64-bit integer", LyoTypeCategory.Integer, LyoTypeEditorKind.Integer, "0", aliases: ["int64", "Long"]);

    /// <summary><see cref="System.Single" />.</summary>
    public static readonly LyoTypeInfo Float = Create(
        "float", typeof(float), "IEEE 754 single-precision floating point", LyoTypeCategory.Number, LyoTypeEditorKind.Decimal, "0", aliases: ["single"]);

    /// <summary><see cref="System.Double" />.</summary>
    public static readonly LyoTypeInfo Double = Create(
        "double", typeof(double), "IEEE 754 double-precision floating point", LyoTypeCategory.Number, LyoTypeEditorKind.Decimal, "0");

    /// <summary><see cref="System.Decimal" />.</summary>
    public static readonly LyoTypeInfo Decimal = Create("decimal", typeof(decimal), "128-bit decimal number", LyoTypeCategory.Number, LyoTypeEditorKind.Decimal, "0");

    /// <summary><see cref="System.Guid" />.</summary>
    public static readonly LyoTypeInfo Guid = Create(
        "Guid", typeof(Guid), "128-bit globally unique identifier", LyoTypeCategory.Identifier, LyoTypeEditorKind.Guid, "\"00000000-0000-0000-0000-000000000000\"",
        aliases: ["uuid"]);

    /// <summary><see cref="System.Enum" />. Concrete enums are not listed; <see cref="FromType" /> maps any enum type here.</summary>
    public static readonly LyoTypeInfo Enum = Create(
        "enum", typeof(Enum), "Enumeration (name or numeric value). Store the concrete enum FullName, not System.Enum.", LyoTypeCategory.Enum, LyoTypeEditorKind.Enum, "0");

    /// <summary><see cref="System.DateTime" />.</summary>
    public static readonly LyoTypeInfo DateTime = Create(
        "DateTime", typeof(DateTime), "Date and time of day", LyoTypeCategory.Temporal, LyoTypeEditorKind.DateTime, "\"0001-01-01T00:00:00\"");

    /// <summary><see cref="System.DateTimeOffset" />.</summary>
    public static readonly LyoTypeInfo DateTimeOffset = Create(
        "DateTimeOffset", typeof(DateTimeOffset), "Date and time with UTC offset", LyoTypeCategory.Temporal, LyoTypeEditorKind.DateTime, "\"0001-01-01T00:00:00+00:00\"");

#if NET6_0_OR_GREATER
    /// <summary><see cref="System.DateOnly" />.</summary>
    public static readonly LyoTypeInfo DateOnly = Create(
        "DateOnly", typeof(DateOnly), "Calendar date without time", LyoTypeCategory.Temporal, LyoTypeEditorKind.DateOnly, "\"0001-01-01\"");

    /// <summary><see cref="System.TimeOnly" />.</summary>
    public static readonly LyoTypeInfo TimeOnly = Create(
        "TimeOnly", typeof(TimeOnly), "Time of day without date", LyoTypeCategory.Temporal, LyoTypeEditorKind.TimeOnly, "\"00:00:00\"");
#endif

    /// <summary><see cref="System.TimeSpan" />.</summary>
    public static readonly LyoTypeInfo TimeSpan = Create(
        "TimeSpan", typeof(TimeSpan), "Time duration", LyoTypeCategory.Temporal, LyoTypeEditorKind.Duration, "\"00:00:00\"", aliases: ["duration"]);

    /// <summary>Regular-expression pattern persisted as a JSON string.</summary>
    public static readonly LyoTypeInfo Regex = Create(
        "regex", typeof(System.Text.RegularExpressions.Regex), "Regular-expression pattern", LyoTypeCategory.Text, LyoTypeEditorKind.Regex, "\"\"",
        aliases: ["Regex", "regexp"]);

    /// <summary>XML document persisted as a JSON string of markup.</summary>
    public static readonly LyoTypeInfo Xml = Create(
        "xml", typeof(XDocument), "XML document", LyoTypeCategory.Json, LyoTypeEditorKind.Xml, "\"<root/>\"", aliases: ["Xml", "XDocument"]);

    /// <summary><see cref="JsonObject" />.</summary>
    public static readonly LyoTypeInfo JsonObject = Create(
        "JSON object", typeof(JsonObject), "JSON object document", LyoTypeCategory.Json, LyoTypeEditorKind.JsonObject, "{}", isScalar: false, aliases: ["json object"]);

    /// <summary><see cref="JsonArray" />.</summary>
    public static readonly LyoTypeInfo JsonArray = Create(
        "JSON array", typeof(JsonArray), "JSON array document", LyoTypeCategory.Json, LyoTypeEditorKind.JsonArray, "[]", isScalar: false, aliases: ["json array"]);

    /// <summary><see cref="JsonNode" />.</summary>
    public static readonly LyoTypeInfo JsonNode = Create(
        "JSON node", typeof(JsonNode), "Any JSON node (object, array, or value)", LyoTypeCategory.Json, LyoTypeEditorKind.JsonObject, "{}", isScalar: false,
        aliases: ["json node", "json"]);

    /// <summary><see cref="List{T}" /> of strings.</summary>
    public static readonly LyoTypeInfo StringList = Create(
        "List<string>", typeof(List<string>), "List of strings", LyoTypeCategory.Collection, LyoTypeEditorKind.Collection, "[]", isScalar: false, elementType: typeof(string),
        aliases: ["list<string>", "string list"]);

    /// <summary><see cref="List{T}" /> of 32-bit integers.</summary>
    public static readonly LyoTypeInfo IntList = Create(
        "List<int>", typeof(List<int>), "List of 32-bit integers", LyoTypeCategory.Collection, LyoTypeEditorKind.Collection, "[]", isScalar: false, elementType: typeof(int),
        aliases: ["list<int>", "int list"]);

    /// <summary><see cref="List{T}" /> of 64-bit integers.</summary>
    public static readonly LyoTypeInfo LongList = Create(
        "List<long>", typeof(List<long>), "List of 64-bit integers", LyoTypeCategory.Collection, LyoTypeEditorKind.Collection, "[]", isScalar: false, elementType: typeof(long),
        aliases: ["list<long>", "long list"]);

    /// <summary><see cref="List{T}" /> of GUIDs.</summary>
    public static readonly LyoTypeInfo GuidList = Create(
        "List<Guid>", typeof(List<Guid>), "List of GUIDs", LyoTypeCategory.Collection, LyoTypeEditorKind.Collection, "[]", isScalar: false, elementType: typeof(Guid),
        aliases: ["list<guid>", "guid list"]);

    /// <summary>Array of strings.</summary>
    public static readonly LyoTypeInfo StringArray = Create(
        "string[]", typeof(string[]), "Array of strings", LyoTypeCategory.Collection, LyoTypeEditorKind.Collection, "[]", isScalar: false, elementType: typeof(string),
        aliases: ["string array"]);

    /// <summary>Array of 32-bit integers.</summary>
    public static readonly LyoTypeInfo IntArray = Create(
        "int[]", typeof(int[]), "Array of 32-bit integers", LyoTypeCategory.Collection, LyoTypeEditorKind.Collection, "[]", isScalar: false, elementType: typeof(int),
        aliases: ["int array"]);

    /// <summary>Byte array serialized as JSON base64.</summary>
    public static readonly LyoTypeInfo ByteArray = Create(
        "byte[]", typeof(byte[]), "Binary payload (JSON base64)", LyoTypeCategory.Binary, LyoTypeEditorKind.Binary, "\"\"", isScalar: false, aliases: ["bytes", "binary"]);

    private static readonly Dictionary<Type, LyoTypeInfo> ByType = new();
    private static readonly Dictionary<string, LyoTypeInfo> ByFullName = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, LyoTypeInfo> ByAlias = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Type> ResolvedClrTypes = new(StringComparer.Ordinal);
    private static readonly List<LyoTypeInfo> AllTypes = [];
    private static readonly object CatalogLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>Registered types excluding <see cref="Unknown" />, in declaration order.</summary>
    public static IReadOnlyList<LyoTypeInfo> All => AllTypes;

    /// <summary>True when <see cref="ElementType" /> is present (typed lists and arrays).</summary>
    public bool IsCollection => ElementType != null;

    static LyoTypeInfo()
    {
        var fields = typeof(LyoTypeInfo).PublicStaticFields<LyoTypeInfo>();

        foreach (var info in fields) {
            if (info == Unknown)
                continue;

            AllTypes.Add(info);
            if (info.IsClr)
                ByType[info.Type] = info;
            ByFullName[info.FullName] = info;
            RegisterAlias(info.ShortName, info);
            if (info.IsClr && !info.Type.IsGenericType && !string.IsNullOrEmpty(info.Type.Name))
                RegisterAlias(info.Type.Name, info);

            foreach (var alias in info.Aliases) {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterAlias(alias, info);
            }
        }
    }

    /// <summary>
    /// Looks up a catalog row by FullName (ordinal), ShortName, or alias (ignore-case). Unlisted <c>List&lt;T&gt;</c> / <c>T[]</c> FullNames resolve via <see cref="FromType" />.
    /// Loaded assemblies are searched so concrete enums resolve to <see cref="Enum" />. Returns <see cref="Unknown" /> when not registered.
    /// </summary>
    public static LyoTypeInfo FromName(string? name)
    {
        if (name.IsNullOrWhitespace())
            return Unknown;

        var trimmed = name.Trim();
        if (ByFullName.TryGetValue(trimmed, out var byFull))
            return byFull;

        if (ByAlias.TryGetValue(Normalize(trimmed), out var byAlias))
            return byAlias;

        var resolved = TryResolveClrType(trimmed);
        return resolved == null ? Unknown : FromType(resolved);
    }

    /// <summary>
    /// Resolves a CLR type by FullName. Calls <see cref="Type.GetType(string)" /> first, then each loaded assembly.
    /// Concrete enums and other loaded types missing from this catalog still become usable by pickers.
    /// </summary>
    public static Type? TryResolveClrType(string? name)
    {
        if (name.IsNullOrWhitespace())
            return null;

        var trimmed = name.Trim();
        lock (CatalogLock) {
            if (ResolvedClrTypes.TryGetValue(trimmed, out var cached))
                return cached;
        }

        var found = Type.GetType(trimmed, throwOnError: false);
        if (found == null) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    found = assembly.GetType(trimmed, throwOnError: false);
                }
                catch (ArgumentException) {
                    continue;
                }

                if (found != null)
                    break;
            }
        }

        if (found != null) {
            lock (CatalogLock)
                ResolvedClrTypes[trimmed] = found;
        }

        return found;
    }

    /// <summary>True when <paramref name="value" /> is empty or parses as a GUID.</summary>
    public static bool IsValidGuidText(string? value) => value.IsNullOrWhitespace() || System.Guid.TryParse(value, out _);

    /// <summary>
    /// True when <paramref name="value" /> is empty, an absolute URI, or a relative path (contains <c>/</c>). Bare tokens such as <c>87gg</c> are rejected.
    /// </summary>
    public static bool IsValidUriText(string? value)
    {
        if (value.IsNullOrWhitespace())
            return true;

        if (System.Uri.IsWellFormedUriString(value, UriKind.Absolute))
            return true;

        return value.IndexOf('/') >= 0 && System.Uri.TryCreate(value, UriKind.Relative, out _);
    }

    /// <summary>Attempts to resolve a catalog row by name or alias.</summary>
    public static bool TryFromName(string? name, out LyoTypeInfo info)
    {
        info = FromName(name);
        return info != Unknown;
    }

    /// <summary>
    /// Looks up a catalog row by runtime type (nullable unwrapped). Exact match first; any <c>type.IsEnum</c> maps to <see cref="Enum" />. Unlisted <c>List&lt;T&gt;</c> and
    /// single-dimension arrays of a registered element type (or enum) synthesize a collection row (not added to <see cref="All" />). Returns <see cref="Unknown" /> when not
    /// registered.
    /// </summary>
    public static LyoTypeInfo FromType(Type? type)
    {
        if (type == null)
            return Unknown;

        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (ByType.TryGetValue(underlying, out var found))
            return found;

        if (underlying.IsEnum)
            return Enum;

        return TrySynthesizeCollection(underlying) ?? Unknown;
    }

    /// <summary>Attempts to resolve a catalog row by runtime type.</summary>
    public static bool TryFromType(Type? type, out LyoTypeInfo info)
    {
        info = FromType(type);
        return info != Unknown;
    }

    /// <summary>Catalog rows in <paramref name="category" /> (excludes <see cref="Unknown" />).</summary>
    public static IEnumerable<LyoTypeInfo> ByCategory(LyoTypeCategory category) => AllTypes.Where(t => t.Category == category);

    /// <summary>Catalog FullName when <paramref name="name" /> is known; otherwise the trimmed input (custom CLR names are left unchanged).</summary>
    public static string NormalizeFullName(string? name)
    {
        var info = FromName(name);
        return info == Unknown ? name?.Trim() ?? "" : info.FullName;
    }

    /// <summary>Collection catalog row for <c>List&lt;<paramref name="elementType" />&gt;</c> (named list when registered, otherwise synthesized).</summary>
    public static LyoTypeInfo ListOf(Type elementType) => FromType(typeof(List<>).MakeGenericType(elementType));

    /// <summary>Collection catalog row for <paramref name="elementType" /><c>[]</c> (named array when registered, otherwise synthesized).</summary>
    public static LyoTypeInfo ArrayOf(Type elementType) => FromType(elementType.MakeArrayType());

    /// <summary>
    /// Adds a catalog row from another package. Non-CLR types pass <paramref name="isClr" /> false and a package-owned <paramref name="fullName" /> so they do not steal
    /// <see cref="FromType" /> for <paramref name="type" />. Idempotent on FullName.
    /// </summary>
    public static LyoTypeInfo Register(
        string shortName,
        string fullName,
        string description,
        LyoTypeCategory category,
        LyoTypeEditorKind editorKind,
        string defaultJson,
        Type? type = null,
        bool isClr = false,
        bool isScalar = true,
        Type? elementType = null,
        string[]? aliases = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(shortName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentHelpers.ThrowIfNull(description);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(defaultJson);

        lock (CatalogLock) {
            if (ByFullName.TryGetValue(fullName, out var existing))
                return existing;

            var runtimeType = type ?? typeof(string);
            var info = new LyoTypeInfo(
                shortName, fullName, description, runtimeType, category, editorKind, isScalar, defaultJson, aliases ?? [], elementType, isClr);
            AllTypes.Add(info);
            ByFullName[fullName] = info;
            if (isClr)
                ByType[runtimeType] = info;

            RegisterAlias(shortName, info);
            foreach (var alias in info.Aliases) {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterAlias(alias, info);
            }

            return info;
        }
    }

    /// <summary>
    /// True when <paramref name="json" /> is empty or deserializes as <paramref name="typeName" />. Regex and Xml values are JSON strings of the pattern or markup. Unknown types
    /// require parseable JSON.
    /// </summary>
    public static bool TryValidateJson(string? typeName, string? json)
    {
        if (json.IsNullOrWhitespace() || json == "null")
            return true;

        var info = FromName(typeName);
        try {
            switch (info.EditorKind) {
                case LyoTypeEditorKind.Guid:
                    return TryValidateGuid(json);
                case LyoTypeEditorKind.Uri:
                    return TryValidateUri(json);
                case LyoTypeEditorKind.Regex:
                    return TryValidateRegex(json);
                case LyoTypeEditorKind.Formatter:
                    return TryValidateFormatterJson(json);
                case LyoTypeEditorKind.Xml:
                    return TryValidateXml(json);
                case LyoTypeEditorKind.JsonObject:
                case LyoTypeEditorKind.JsonArray:
                    using (JsonDocument.Parse(json))
                        return true;
                default:
                    var target = info == Unknown ? typeof(System.Text.Json.Nodes.JsonNode) : info.Type;
                    _ = JsonSerializer.Deserialize(json, target, JsonOptions);
                    return true;
            }
        }
        catch (JsonException) {
            return false;
        }
        catch (NotSupportedException) {
            return false;
        }
        catch (ArgumentException) {
            return false;
        }
    }

    /// <summary>Serializes <paramref name="value" /> to JSON for storage on job, report, and config string columns.</summary>
    public string ToJson(object? value)
    {
        if (value is null)
            return "null";

        if (!IsClr || EditorKind == LyoTypeEditorKind.Formatter)
            return JsonSerializer.Serialize(value as string ?? value.ToString() ?? "", JsonOptions);

        if (value is string json && LooksLikeJson(json) && EditorKind is LyoTypeEditorKind.JsonObject or LyoTypeEditorKind.JsonArray or LyoTypeEditorKind.Collection)
            return json;

        return JsonSerializer.Serialize(value, Type == typeof(System.Enum) ? value.GetType() : Type, JsonOptions);
    }

    /// <summary>Implicit conversion to the runtime <see cref="Type" /> used for JSON and pickers.</summary>
    public static implicit operator Type(LyoTypeInfo info) => info.Type;

    /// <inheritdoc />
    public override string ToString() => Category == LyoTypeCategory.Unknown ? ShortName : $"{ShortName} ({FullName})";

    private static LyoTypeInfo Create(
        string shortName,
        Type type,
        string description,
        LyoTypeCategory category,
        LyoTypeEditorKind editorKind,
        string defaultJson,
        bool isScalar = true,
        Type? elementType = null,
        string[]? aliases = null,
        bool isClr = true)
        => new(shortName, type.FullName ?? type.Name, description, type, category, editorKind, isScalar, defaultJson, aliases ?? [], elementType, isClr);

    private static void RegisterAlias(string alias, LyoTypeInfo info)
    {
        var key = Normalize(alias);
        if (!ByAlias.ContainsKey(key))
            ByAlias[key] = info;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static LyoTypeInfo? TrySynthesizeCollection(Type type)
    {
        Type? element = null;
        var isArray = type.IsArray;
        if (isArray) {
            if (type.GetArrayRank() != 1)
                return null;
            element = type.GetElementType();
        }
        else if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(List<>))
            element = type.GetGenericArguments()[0];

        if (element == null)
            return null;

        var elementInfo = FromType(element);
        if (elementInfo == Unknown)
            return null;

        var elementLabel = elementInfo == Enum ? element.Name : elementInfo.ShortName;
        var shortName = isArray ? $"{elementLabel}[]" : $"List<{elementLabel}>";
        var info = new LyoTypeInfo(
            shortName, type.FullName ?? type.Name, isArray ? $"Array of {elementLabel}" : $"List of {elementLabel}", type, LyoTypeCategory.Collection,
            LyoTypeEditorKind.Collection, false, "[]", [], element, true);
        ByType[type] = info;
        ByFullName[info.FullName] = info;
        return info;
    }

    private static bool TryValidateGuid(string json)
    {
        var text = JsonSerializer.Deserialize<string>(json, JsonOptions);
        return text is not null && System.Guid.TryParse(text, out _);
    }

    private static bool TryValidateUri(string json)
    {
        var text = JsonSerializer.Deserialize<string>(json, JsonOptions);
        return !string.IsNullOrWhiteSpace(text) && IsValidUriText(text);
    }

    private static bool TryValidateFormatterJson(string json)
    {
        try {
            _ = JsonSerializer.Deserialize<string>(json, JsonOptions);
            return true;
        }
        catch (JsonException) {
            return json.IndexOf('{') >= 0;
        }
    }

    private static bool TryValidateRegex(string json)
    {
        var pattern = JsonSerializer.Deserialize<string>(json, JsonOptions);
        if (pattern.IsNullOrWhitespace())
            return true;

        try {
            _ = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.None, System.TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException) {
            return false;
        }
    }

    private static bool TryValidateXml(string json)
    {
        var markup = JsonSerializer.Deserialize<string>(json, JsonOptions);
        if (markup.IsNullOrWhitespace())
            return true;

        try {
            _ = XDocument.Parse(markup);
            return true;
        }
        catch (XmlException) {
            return false;
        }
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{") || trimmed.StartsWith("[") || trimmed.StartsWith("\"");
    }
}
