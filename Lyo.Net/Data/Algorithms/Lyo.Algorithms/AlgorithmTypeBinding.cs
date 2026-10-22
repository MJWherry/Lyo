using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>
/// Types for one revision. Each identifier and each <c>Algo</c> / <c>score</c> call has its own type. An omitted name is <see cref="double" />, except identifiers that end in
/// <c>_key</c>, which stay <see cref="string" />. A missing result is <see cref="double" /> for expressions and structured revisions.
/// </summary>
public sealed class AlgorithmTypeBinding
{
    private static readonly Dictionary<string, Type> Aliases = new(StringComparer.OrdinalIgnoreCase) {
        ["bool"] = typeof(bool),
        ["byte"] = typeof(byte),
        ["sbyte"] = typeof(sbyte),
        ["short"] = typeof(short),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["ulong"] = typeof(ulong),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["string"] = typeof(string),
        ["datetime"] = typeof(DateTime),
        ["datetimeoffset"] = typeof(DateTimeOffset),
        ["timespan"] = typeof(TimeSpan)
    };

    private static readonly ConcurrentDictionary<string, Type> Enums = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Type> _calls;
    private readonly Dictionary<string, Type> _parameters;

    private AlgorithmTypeBinding(
        Type result, bool resultSpecified, Type input, bool inputSpecified, Dictionary<string, Type> parameters, Dictionary<string, Type> calls, string canonical)
    {
        Result = result;
        ResultSpecified = resultSpecified;
        Input = input;
        InputSpecified = inputSpecified;
        _parameters = parameters;
        _calls = calls;
        Canonical = canonical;
    }

    /// <summary>Binding used when a revision stores no types. The result and every undeclared name are <see cref="double" />.</summary>
    public static AlgorithmTypeBinding Untyped { get; } = Build(typeof(double), false, typeof(double), false, new(), new());

    /// <summary>Result type of the revision. <see cref="double" /> when <see cref="ResultSpecified" /> is false.</summary>
    public Type Result { get; }

    /// <summary>True when the payload named <c>result</c>.</summary>
    public bool ResultSpecified { get; }

    /// <summary>Numeric input type for a range band or a linear map. <see cref="double" /> when <see cref="InputSpecified" /> is false.</summary>
    public Type Input { get; }

    /// <summary>True when the payload named <c>inputType</c>.</summary>
    public bool InputSpecified { get; }

    /// <summary>Canonical encoding of the maps, included in the compiler cache key.</summary>
    public string Canonical { get; }

    /// <summary>Alias for <paramref name="type" />. Arrays use the element alias plus <c>[]</c>. A registered enum uses its simple name.</summary>
    public static string Alias(Type type)
    {
        ArgumentHelpers.ThrowIfNull(type);
        if (type.IsArray)
            return Alias(type.GetElementType()!) + "[]";

        foreach (var pair in Aliases) {
            if (pair.Value == type)
                return pair.Key;
        }

        foreach (var pair in Enums) {
            if (pair.Value == type)
                return pair.Key;
        }

        return type.Name;
    }

    /// <summary>
    /// Registers <paramref name="type" /> so a revision may name it. The alias is the enum's simple name. Two enums with the same simple name cannot both be registered.
    /// </summary>
    /// <param name="type">Enum type the host owns.</param>
    public static void RegisterEnum(Type type)
    {
        ArgumentHelpers.ThrowIfNull(type);
        AlgorithmHelpers.ThrowIf(!type.IsEnum, $"{type.Name} is not an enum.", "algorithm.payload.invalid");
        AlgorithmHelpers.ThrowIf(!Enums.TryAdd(type.Name, type), $"Enum '{type.Name}' is already registered.", "algorithm.payload.invalid");
    }

    /// <summary>Builds a binding from alias names. Unknown names throw <see cref="AlgorithmException" />.</summary>
    /// <param name="result">Result alias. Null means <see cref="double" /> and <see cref="ResultSpecified" /> is false.</param>
    /// <param name="parameters">Identifier aliases. Null means no declared identifiers.</param>
    /// <param name="calls"><c>Algo</c> / <c>score</c> result aliases. Null means no declared calls.</param>
    /// <param name="input">Numeric input alias for a band or a line. Null means <see cref="double" />.</param>
    public static AlgorithmTypeBinding Create(
        string? result = null, IReadOnlyDictionary<string, string>? parameters = null, IReadOnlyDictionary<string, string>? calls = null, string? input = null)
    {
        var parameterTypes = ReadMap(parameters);
        var callTypes = ReadMap(calls);
        var resultType = result is null ? typeof(double) : ParseAlias(result);
        var inputType = input is null ? typeof(double) : ParseAlias(input);
        return Build(resultType, result is not null, inputType, input is not null, parameterTypes, callTypes);
    }

    /// <summary>Type of <paramref name="name" />. A <c>_key</c> suffix that was not declared is <see cref="string" />. Anything else undeclared is <see cref="double" />.</summary>
    public Type ParameterType(string name)
    {
        if (_parameters.TryGetValue(name, out var type))
            return type;

        return name.EndsWith("_key", StringComparison.Ordinal) ? typeof(string) : typeof(double);
    }

    /// <summary>Result type of <c>Algo</c> / <c>score</c> for <paramref name="key" />. Undeclared calls are <see cref="double" />.</summary>
    public Type CallType(string key) => _calls.TryGetValue(key, out var type) ? type : typeof(double);

    /// <summary>Reads <c>result</c>, <c>inputType</c>, <c>parameters</c>, and <c>calls</c> from a revision payload. Null and empty payloads are <see cref="Untyped" />.</summary>
    internal static AlgorithmTypeBinding ParsePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return Untyped;

        try {
            using var document = JsonDocument.Parse(payload!);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                AlgorithmHelpers.Throw("Algorithm payload must be a JSON object.", "algorithm.payload.invalid");

            string? result = null;
            string? input = null;
            if (root.TryGetProperty("result", out var resultElement) && resultElement.ValueKind == JsonValueKind.String)
                result = resultElement.GetString();

            if (root.TryGetProperty("inputType", out var inputElement) && inputElement.ValueKind == JsonValueKind.String)
                input = inputElement.GetString();

            return Create(result, ReadObject(root, "parameters"), ReadObject(root, "calls"), input);
        }
        catch (AlgorithmException) {
            throw;
        }
        catch (JsonException ex) {
            AlgorithmHelpers.Throw(ex.Message, "algorithm.payload.invalid");
            return Untyped;
        }
    }

    internal AlgorithmTypeBinding WithResult(Type result) => Build(result, true, Input, InputSpecified, new Dictionary<string, Type>(_parameters, StringComparer.Ordinal), new Dictionary<string, Type>(_calls, StringComparer.Ordinal));

    internal AlgorithmTypeBinding WithCalls(IReadOnlyDictionary<string, Type> calls)
    {
        var copy = new Dictionary<string, Type>(_calls, StringComparer.Ordinal);
        foreach (var pair in calls)
            copy[pair.Key] = pair.Value;

        return Build(Result, ResultSpecified, Input, InputSpecified, new Dictionary<string, Type>(_parameters, StringComparer.Ordinal), copy);
    }

    private static AlgorithmTypeBinding Build(
        Type result, bool resultSpecified, Type input, bool inputSpecified, Dictionary<string, Type> parameters, Dictionary<string, Type> calls)
    {
        var canonical = new StringBuilder();
        canonical.Append(result.FullName);
        canonical.Append('|');
        canonical.Append(input.FullName);
        Append(canonical, "p:", parameters);
        Append(canonical, "c:", calls);
        return new(result, resultSpecified, input, inputSpecified, parameters, calls, canonical.ToString());
    }

    private static void Append(StringBuilder canonical, string prefix, Dictionary<string, Type> map)
    {
        if (map.Count == 0)
            return;

        var names = new string[map.Count];
        map.Keys.CopyTo(names, 0);
        Array.Sort(names, StringComparer.Ordinal);
        foreach (var name in names) {
            canonical.Append('|');
            canonical.Append(prefix);
            canonical.Append(name);
            canonical.Append('=');
            canonical.Append(map[name].FullName);
        }
    }

    private static Dictionary<string, Type> ReadMap(IReadOnlyDictionary<string, string>? names)
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        if (names is null)
            return map;

        foreach (var pair in names)
            map[pair.Key] = ParseAlias(pair.Value);

        return map;
    }

    private static Dictionary<string, string>? ReadObject(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
            return null;

        AlgorithmHelpers.ThrowIf(element.ValueKind != JsonValueKind.Object, $"Algorithm '{name}' must be an object.", "algorithm.payload.invalid");
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject()) {
            AlgorithmHelpers.ThrowIf(property.Value.ValueKind != JsonValueKind.String, $"Algorithm type '{property.Name}' must be a string.", "algorithm.payload.invalid");
            map[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return map;
    }

    private static Type ParseAlias(string alias)
    {
        var text = alias.Trim();
        AlgorithmHelpers.ThrowIf(text.EndsWith("[][]", StringComparison.Ordinal), $"Algorithm type '{alias}' is nested.", "algorithm.payload.invalid");
        var array = text.EndsWith("[]", StringComparison.Ordinal);
        if (array)
            text = text.Substring(0, text.Length - 2).Trim();

        if (!Aliases.TryGetValue(text, out var type) && !Enums.TryGetValue(text, out type))
            AlgorithmHelpers.Throw($"Algorithm type '{alias}' is not supported.", "algorithm.payload.invalid");

        return array ? type!.MakeArrayType() : type!;
    }

    internal static bool IsNumeric(Type type)
    {
        var code = Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type);
        return code is >= TypeCode.SByte and <= TypeCode.Decimal;
    }

    internal static bool IsIntegral(Type type)
    {
        var code = Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type);
        return code is >= TypeCode.SByte and <= TypeCode.UInt64;
    }

    internal static bool UseDecimal(Type type) => IsIntegral(type) || type == typeof(decimal);
}
