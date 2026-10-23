using Lyo.Algorithms;
using Lyo.Exceptions;

namespace Lyo.Formatter.Algorithms;

/// <summary>
/// <see cref="IFormatterExpressionEvaluator" /> that runs <c>Algo("key")</c> and <c>score("key")</c> on <see cref="IAlgorithmRegistry" />. Any other expression uses the
/// built-in formatter engine.
/// </summary>
public sealed class AlgorithmFormatterExpressionEvaluator : IFormatterExpressionEvaluator
{
    private readonly IFormatterExpressionEvaluator _fallback;
    private readonly IAlgorithmRegistry _registry;

    /// <summary>Creates the addon. Other expressions use <see cref="FormatterExpressionEvaluator" />.</summary>
    /// <param name="registry">Catalog and startup algorithms. Must not be null.</param>
    public AlgorithmFormatterExpressionEvaluator(IAlgorithmRegistry registry)
        : this(registry, new FormatterExpressionEvaluator()) { }

    /// <summary>Creates the addon with a caller-supplied fallback for non-algorithm expressions.</summary>
    /// <param name="registry">Catalog and startup algorithms. Must not be null.</param>
    /// <param name="fallback">Evaluator for expressions that are not <c>Algo</c> or <c>score</c>. Must not be null.</param>
    public AlgorithmFormatterExpressionEvaluator(IAlgorithmRegistry registry, IFormatterExpressionEvaluator fallback)
    {
        ArgumentHelpers.ThrowIfNull(registry);
        ArgumentHelpers.ThrowIfNull(fallback);
        _registry = registry;
        _fallback = fallback;
    }

    /// <inheritdoc />
    public string? TryValidate(string expression)
    {
        if (!ContainsCall(expression))
            return _fallback.TryValidate(expression);

        return TryParseCall(expression, out var _) ? null : "Formatter algorithms support Algo(\"key\") or score(\"key\").";
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetIdentifiers(string expression) => ContainsCall(expression) ? [] : _fallback.GetIdentifiers(expression);

    /// <inheritdoc />
    public bool TryEvaluate(string expression, IReadOnlyDictionary<string, object?> context, out object? value, out string? error)
    {
        if (!ContainsCall(expression))
            return _fallback.TryEvaluate(expression, context, out value, out error);

        if (!TryParseCall(expression, out var key)) {
            value = null;
            error = "Formatter algorithms support Algo(\"key\") or score(\"key\").";
            return false;
        }

        try {
            var bag = ToBag(context);
            value = _registry.ExecuteAsync<AlgorithmContextBag, double>(AlgorithmRef.Latest(key), bag).GetAwaiter().GetResult();
            error = null;
            return true;
        }
        catch (Exception ex) {
            value = null;
            error = ex.Message;
            return false;
        }
    }

    private static AlgorithmContextBag ToBag(IReadOnlyDictionary<string, object?> context)
    {
        var bag = new AlgorithmContextBag();
        foreach (var pair in context) {
            if (AlgorithmContextBag.IsIdentifier(pair.Key))
                bag.Set(pair.Key, pair.Value);
        }

        return bag;
    }

    private static bool ContainsCall(string expression)
        => expression.IndexOf("Algo(", StringComparison.Ordinal) >= 0 || expression.IndexOf("score(", StringComparison.Ordinal) >= 0;

    private static bool TryParseCall(string expression, out string key)
    {
        key = string.Empty;
        var text = expression.Trim();
        var prefix = text.StartsWith("Algo(", StringComparison.Ordinal) ? "Algo(" : text.StartsWith("score(", StringComparison.Ordinal) ? "score(" : null;
        if (prefix is null || !text.EndsWith(")", StringComparison.Ordinal))
            return false;

        var inner = text.Substring(prefix.Length, text.Length - prefix.Length - 1).Trim();
        if (inner.Length < 2 || inner[0] != '"' || inner[^1] != '"')
            return false;

        key = inner.Substring(1, inner.Length - 2);
        return AlgorithmKeys.IsKey(key);
    }
}