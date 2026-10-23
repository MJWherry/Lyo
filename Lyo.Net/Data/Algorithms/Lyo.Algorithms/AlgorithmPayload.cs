using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Result;

namespace Lyo.Algorithms;

/// <summary>JSON bodies for structured catalog revisions.</summary>
public static class AlgorithmPayload
{
    internal static readonly JsonSerializerOptions Json = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Checks <paramref name="payload" /> for <paramref name="kind" />. Expression and named revisions do not use this.</summary>
    public static Result<Unit> Validate(AlgorithmKind kind, string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return Result<Unit>.Failure("Algorithm payload is required.", "algorithm.payload.missing");

        try {
            var binding = AlgorithmTypeBinding.ParsePayload(payload);
            switch (kind) {
                case AlgorithmKind.DiscreteMap:
                    var map = ParseMap(payload!);
                    RequireZeroValue(binding, map.Missing == MissingKeyPolicy.Zero, map.MissingValue);
                    break;
                case AlgorithmKind.RangeBand:
                    var bands = ParseBands(payload!);
                    RequireNumericInput(binding);
                    RequireZeroValue(binding, bands.OutOfRange == OutOfRangePolicy.Zero, bands.MissingValue);
                    break;
                case AlgorithmKind.Linear:
                    ParseLinear(payload!);
                    RequireNumericInput(binding);
                    RequireNumericResult(binding);
                    break;
                case AlgorithmKind.Composite:
                    var composite = ParseComposite(payload!);
                    if (composite.Combiner != CombinerKind.Formula)
                        RequireNumericResult(binding);

                    break;
                default:
                    return Result<Unit>.Failure($"Algorithm kind '{kind}' does not use a payload.", "algorithm.payload.invalid");
            }

            return Result<Unit>.Success(default);
        }
        catch (AlgorithmException ex) {
            return Result<Unit>.Failure(ex.Message, ex.Code ?? "algorithm.payload.invalid");
        }
        catch (Exception ex) {
            return Result<Unit>.Failure(ex.Message, "algorithm.payload.invalid");
        }
    }

    internal static DiscreteMapBody ParseMap(string payload) => Require(JsonSerializer.Deserialize<DiscreteMapBody>(payload, Json), "Discrete map payload is invalid.");

    internal static RangeBandBody ParseBands(string payload) => Require(JsonSerializer.Deserialize<RangeBandBody>(payload, Json), "Range band payload is invalid.");

    internal static LinearBody ParseLinear(string payload) => Require(JsonSerializer.Deserialize<LinearBody>(payload, Json), "Linear payload is invalid.");

    internal static CompositeBody ParseComposite(string payload) => Require(JsonSerializer.Deserialize<CompositeBody>(payload, Json), "Composite payload is invalid.");

    private static void RequireNumericInput(AlgorithmTypeBinding binding)
        => AlgorithmHelpers.ThrowIf(!AlgorithmTypeBinding.IsNumeric(binding.Input), "Algorithm input type must be numeric.", "algorithm.payload.invalid");

    private static void RequireNumericResult(AlgorithmTypeBinding binding)
        => AlgorithmHelpers.ThrowIf(!AlgorithmTypeBinding.IsNumeric(binding.Result), "Algorithm result type must be numeric.", "algorithm.payload.invalid");

    private static void RequireZeroValue(AlgorithmTypeBinding binding, bool usesZero, JsonElement missing)
    {
        if (!usesZero || AlgorithmTypeBinding.IsNumeric(binding.Result))
            return;

        AlgorithmHelpers.ThrowIf(
            missing.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null, "A non-numeric result needs missingValue when the missing policy is zero.",
            "algorithm.payload.invalid");
    }

    private static T Require<T>(T? value, string message)
        where T : class, ICheckedPayload
    {
        var body = AlgorithmHelpers.ThrowIfNull(value, message, "algorithm.payload.invalid");
        body.Check();
        return body;
    }
}

internal interface ICheckedPayload
{
    void Check();
}

/// <summary>Choice to points.</summary>
public sealed class DiscreteMapBody : ICheckedPayload
{
    /// <summary>Context identifier whose value is matched to <see cref="Entries" />.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>What happens when <see cref="Input" /> is not in <see cref="Entries" />.</summary>
    public MissingKeyPolicy Missing { get; set; } = MissingKeyPolicy.Zero;

    /// <summary>Value used when <see cref="Missing" /> is <see cref="MissingKeyPolicy.Zero" /> and the result is not numeric.</summary>
    public JsonElement MissingValue { get; set; }

    /// <summary>Allowed values.</summary>
    public List<DiscreteMapEntry> Entries { get; set; } = [];

    void ICheckedPayload.Check()
    {
        AlgorithmHelpers.ThrowIf(!AlgorithmContextBag.IsIdentifier(Input), "Discrete map input must be an identifier.", "algorithm.payload.invalid");
        AlgorithmHelpers.ThrowIf(Entries.Count == 0, "Discrete map entries are required.", "algorithm.payload.invalid");
        foreach (var entry in Entries) {
            AlgorithmHelpers.ThrowIf(string.IsNullOrWhiteSpace(entry.Key), "Discrete map entry key is required.", "algorithm.payload.invalid");
            AlgorithmHelpers.ThrowIf(entry.Points.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null, "Discrete map entry value is required.", "algorithm.payload.invalid");
        }
    }
}

/// <summary>One discrete choice.</summary>
public sealed class DiscreteMapEntry
{
    /// <summary>Matched against the input text.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Value for <see cref="Key" />. A JSON number or string, converted to the revision result when the revision compiles.</summary>
    public JsonElement Points { get; set; }
}

/// <summary>How a discrete map treats an unknown choice.</summary>
public enum MissingKeyPolicy
{
    /// <summary>Score zero.</summary>
    Zero = 0,

    /// <summary>Fail the execute.</summary>
    Reject = 1
}

/// <summary>Half-open bands over a number.</summary>
public sealed class RangeBandBody : ICheckedPayload
{
    /// <summary>Context identifier.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>What happens when no band contains the input.</summary>
    public OutOfRangePolicy OutOfRange { get; set; } = OutOfRangePolicy.Zero;

    /// <summary>Value used when <see cref="OutOfRange" /> is <see cref="OutOfRangePolicy.Zero" /> and the result is not numeric.</summary>
    public JsonElement MissingValue { get; set; }

    /// <summary>Bands. Each is <c>[min, max)</c>.</summary>
    public List<RangeBandEntry> Bands { get; set; } = [];

    void ICheckedPayload.Check()
    {
        AlgorithmHelpers.ThrowIf(!AlgorithmContextBag.IsIdentifier(Input), "Range band input must be an identifier.", "algorithm.payload.invalid");
        AlgorithmHelpers.ThrowIf(Bands.Count == 0, "Range bands are required.", "algorithm.payload.invalid");
        foreach (var band in Bands) {
            AlgorithmHelpers.ThrowIf(AlgorithmValues.Decimal(band.Max) <= AlgorithmValues.Decimal(band.Min), "Range band max must be greater than min.", "algorithm.payload.invalid");
            AlgorithmHelpers.ThrowIf(band.Points.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null, "Range band value is required.", "algorithm.payload.invalid");
        }
    }
}

/// <summary>One half-open band.</summary>
public sealed class RangeBandEntry
{
    /// <summary>Inclusive start. A JSON number, converted to the input type when the revision compiles.</summary>
    public JsonElement Min { get; set; }

    /// <summary>Exclusive end. A JSON number, converted to the input type when the revision compiles.</summary>
    public JsonElement Max { get; set; }

    /// <summary>Value while the input is in this band. Converted to the revision result when the revision compiles.</summary>
    public JsonElement Points { get; set; }
}

/// <summary>How a range band treats a value outside every band.</summary>
public enum OutOfRangePolicy
{
    /// <summary>Score zero.</summary>
    Zero = 0,

    /// <summary>Use the nearest band.</summary>
    Nearest = 1,

    /// <summary>Fail the execute.</summary>
    Reject = 2
}

/// <summary>Maps an input interval onto a points interval.</summary>
public sealed class LinearBody : ICheckedPayload
{
    /// <summary>Context identifier.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>Input value that maps to <see cref="PointsMin" />. A JSON number.</summary>
    public JsonElement InputMin { get; set; }

    /// <summary>Input value that maps to <see cref="PointsMax" />. A JSON number.</summary>
    public JsonElement InputMax { get; set; }

    /// <summary>Value at <see cref="InputMin" />. Converted to the revision result when the revision compiles.</summary>
    public JsonElement PointsMin { get; set; }

    /// <summary>Value at <see cref="InputMax" />. Converted to the revision result when the revision compiles.</summary>
    public JsonElement PointsMax { get; set; }

    /// <summary>When true, inputs outside the range stay on the nearest endpoint.</summary>
    public bool Clamp { get; set; }

    void ICheckedPayload.Check()
    {
        AlgorithmHelpers.ThrowIf(!AlgorithmContextBag.IsIdentifier(Input), "Linear input must be an identifier.", "algorithm.payload.invalid");
        AlgorithmHelpers.ThrowIf(AlgorithmValues.Decimal(InputMax) == AlgorithmValues.Decimal(InputMin), "Linear inputMax must differ from inputMin.", "algorithm.payload.invalid");
    }
}

/// <summary>Children plus a combiner.</summary>
public sealed class CompositeBody : ICheckedPayload
{
    /// <summary>How child points become one number.</summary>
    public CombinerKind Combiner { get; set; } = CombinerKind.Sum;

    /// <summary>Expression used when <see cref="Combiner" /> is <see cref="CombinerKind.Formula" />. <c>score("child")</c> reads a child.</summary>
    public string? Formula { get; set; }

    /// <summary>Inline scorers or catalog refs, in order.</summary>
    public List<CompositeChild> Children { get; set; } = [];

    void ICheckedPayload.Check()
    {
        AlgorithmHelpers.ThrowIf(Children.Count == 0, "Composite children are required.", "algorithm.payload.invalid");
        AlgorithmHelpers.ThrowIf(Combiner == CombinerKind.Formula && string.IsNullOrWhiteSpace(Formula), "Composite formula is required.", "algorithm.payload.invalid");
        foreach (var child in Children) {
            var hasRef = !string.IsNullOrWhiteSpace(child.Ref);
            var hasInline = child.Kind is not null;
            AlgorithmHelpers.ThrowIf(hasRef == hasInline, "Composite child needs a ref or an inline kind, not both.", "algorithm.payload.invalid");
            AlgorithmHelpers.ThrowIf(hasRef && !AlgorithmKeys.IsKey(child.Ref!), $"Composite ref '{child.Ref}' is not a catalog key.", "algorithm.payload.invalid");
            if (!hasInline)
                continue;

            AlgorithmHelpers.ThrowIf(
                string.IsNullOrWhiteSpace(child.Key) || !AlgorithmContextBag.IsIdentifier(child.Key!), "Inline composite child key must be an identifier.",
                "algorithm.payload.invalid");

            AlgorithmHelpers.ThrowIf(
                child.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null, "Inline composite child payload is required.", "algorithm.payload.missing");

            var validated = AlgorithmPayload.Validate(child.Kind!.Value, child.Payload.GetRawText());
            if (!validated.IsSuccess)
                AlgorithmHelpers.Throw(validated.Errors![0].Message, validated.Errors[0].Code);
        }
    }
}

/// <summary>One composite part.</summary>
public sealed class CompositeChild
{
    /// <summary>Name used by <c>score("name")</c> for an inline child.</summary>
    public string? Key { get; set; }

    /// <summary>Catalog key. Latest revision.</summary>
    public string? Ref { get; set; }

    /// <summary>Inline scorer kind.</summary>
    public AlgorithmKind? Kind { get; set; }

    /// <summary>Inline scorer JSON.</summary>
    public JsonElement Payload { get; set; }

    /// <summary>Multiplier for <see cref="CombinerKind.WeightedSum" />. Omitted means 1. A JSON number, read as a decimal or double coefficient when the revision compiles.</summary>
    public JsonElement Weight { get; set; }
}

/// <summary>How a composite combines children.</summary>
public enum CombinerKind
{
    /// <summary>Add child points.</summary>
    Sum = 0,

    /// <summary>Add child points times <see cref="CompositeChild.Weight" />.</summary>
    WeightedSum = 1,

    /// <summary>Run <see cref="CompositeBody.Formula" />.</summary>
    Formula = 2
}

/// <summary>One <c>Algo</c> or <c>score</c> call replaced with a typed placeholder before parse.</summary>
internal readonly record struct AlgorithmCallSite(string Name, string Key, Type Type);

/// <summary>Reads <c>Algo("key")</c> and <c>score("key")</c> names out of an expression.</summary>
internal static class AlgorithmCalls
{
    /// <summary>Replaces <c>Algo("key")</c> and <c>score("key")</c> with identifiers whose types are <paramref name="callType" />.</summary>
    public static string Placeholders(string source, Func<string, Type> callType, List<AlgorithmCallSite> sites)
    {
        var builder = new System.Text.StringBuilder(source.Length);
        var index = 0;
        while (index < source.Length) {
            var algo = source.IndexOf("Algo(\"", index, StringComparison.Ordinal);
            var score = source.IndexOf("score(\"", index, StringComparison.Ordinal);
            var start = Next(algo, score);
            if (start < 0) {
                builder.Append(source, index, source.Length - index);
                break;
            }

            builder.Append(source, index, start - index);
            var keyStart = source[start] == 'A' ? start + 6 : start + 7;
            var keyEnd = source.IndexOf('"', keyStart);
            AlgorithmHelpers.ThrowIf(keyEnd < 0 || keyEnd + 1 >= source.Length || source[keyEnd + 1] != ')', "Algo and score require a constant key.", "algorithm.expression.parse");
            var key = source.Substring(keyStart, keyEnd - keyStart);
            var name = "__lyo" + sites.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sites.Add(new(name, key, callType(key)));
            builder.Append(name);
            index = keyEnd + 2;
        }

        return builder.ToString();
    }

    public static IEnumerable<string> Keys(string? source)
    {
        if (source is not { Length: > 0 })
            yield break;

        var index = 0;
        while (index < source.Length) {
            var algo = source.IndexOf("Algo(\"", index, StringComparison.Ordinal);
            var score = source.IndexOf("score(\"", index, StringComparison.Ordinal);
            var start = Next(algo, score);
            if (start < 0)
                yield break;

            var quote = source.IndexOf('"', start);
            var end = quote < 0 ? -1 : source.IndexOf('"', quote + 1);
            if (end < 0)
                yield break;

            yield return source.Substring(quote + 1, end - quote - 1);

            index = end + 1;
        }
    }

    private static int Next(int algo, int score)
    {
        if (algo < 0)
            return score;

        if (score < 0)
            return algo;

        return Math.Min(algo, score);
    }
}