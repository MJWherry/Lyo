namespace Lyo.Formatter;

/// <summary>
/// Evaluates <c>{...}</c> tokens. <see cref="FormatterExpressionEvaluator" /> is the default.
/// A host can replace it; <c>AddFormatterService</c> does not register algorithms.
/// </summary>
public interface IFormatterExpressionEvaluator
{
    /// <summary>Returns an error message when <paramref name="expression" /> cannot be parsed, or null when it is acceptable.</summary>
    string? TryValidate(string expression);

    /// <summary>Names the template must supply. Empty when the expression has no context identifiers.</summary>
    IReadOnlyList<string> GetIdentifiers(string expression);

    /// <summary>Runs <paramref name="expression" /> against <paramref name="context" />. Returns false when it cannot run.</summary>
    bool TryEvaluate(string expression, IReadOnlyDictionary<string, object?> context, out object? value, out string? error);
}
