namespace Lyo.Algorithms;

/// <summary>Value and breakdown from an unsaved preview. <see cref="Result" /> is the alias the payload declared, or <c>double</c>.</summary>
public sealed class AlgorithmPreviewResult
{
    /// <summary>Final value, already the declared CLR type.</summary>
    public object? Value { get; init; }

    /// <summary>Result alias, such as <c>double</c>, <c>decimal</c>, or <c>datetime</c>.</summary>
    public string Result { get; init; } = "double";

    /// <summary>Top-level steps. A numeric step keeps <see cref="AlgorithmStep.Points" />. Any other step leaves points at 0 and sets <see cref="AlgorithmStep.Value" />.</summary>
    public IReadOnlyList<AlgorithmStep> Steps { get; init; } = [];
}
