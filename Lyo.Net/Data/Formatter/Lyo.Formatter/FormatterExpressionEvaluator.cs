using Lyo.Exceptions;

namespace Lyo.Formatter;

/// <summary>Default <see cref="IFormatterExpressionEvaluator" />. DateTime, ternary, and in-memory LINQ. No algorithm catalog.</summary>
public sealed class FormatterExpressionEvaluator : IFormatterExpressionEvaluator
{
    private readonly FormatterExpressionEngine _engine;

    /// <summary>Creates an evaluator whose <c>DateTime.Now</c> is the system clock.</summary>
    public FormatterExpressionEvaluator()
        : this(static () => DateTimeOffset.Now) { }

    /// <summary>Creates an evaluator with a frozen or injectable clock.</summary>
    /// <param name="clock">Returns the current instant. Must not be null.</param>
    public FormatterExpressionEvaluator(Func<DateTimeOffset> clock)
    {
        ArgumentHelpers.ThrowIfNull(clock);
        _engine = new(clock);
    }

    /// <inheritdoc />
    public string? TryValidate(string expression) => _engine.TryValidate(expression);

    /// <inheritdoc />
    public IReadOnlyList<string> GetIdentifiers(string expression) => _engine.GetIdentifiers(expression);

    /// <inheritdoc />
    public bool TryEvaluate(string expression, IReadOnlyDictionary<string, object?> context, out object? value, out string? error)
    {
        var bag = context as FormatterContextBag ?? new FormatterContextBag(context);
        return _engine.TryEvaluate(expression, bag, out value, out error);
    }
}