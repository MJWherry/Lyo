using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>Host settings for <c>AddAlgorithms</c>. Section name <see cref="SectionName" />.</summary>
public sealed class AlgorithmOptions
{
    /// <summary>Configuration section. <c>Algorithms</c>.</summary>
    public const string SectionName = "Algorithms";

    /// <summary>When true, a catalog row may replace a startup algorithm with the same key. Default false.</summary>
    public bool AllowRuntimeOverride { get; set; }

    /// <summary>Expressions compiled at startup into the frozen store (bag context, object result).</summary>
    public List<AlgorithmExpressionOption> Expressions { get; set; } = [];

    /// <summary>Maximum <c>Algo</c> / <c>score</c> nesting, including the algorithm that started the call. Default 16.</summary>
    public int MaxChainDepth { get; set; } = 16;

    /// <summary>Checks keys, chain depth, and non-empty sources. Does not compile; <c>AddAlgorithms</c> does that and fails fast.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfLessThan(MaxChainDepth, 1);
        foreach (var expression in Expressions) {
            ArgumentHelpers.ThrowIfNull(expression);
            AlgorithmKeys.ThrowIfInvalidKey(expression.Key);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(expression.Expression);
        }
    }
}