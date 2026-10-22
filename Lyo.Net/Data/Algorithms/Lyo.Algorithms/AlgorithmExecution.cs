using System.Text.Json.Serialization;

namespace Lyo.Algorithms;

/// <summary>One scored step. Children are nested <c>Algo</c> / <c>score</c> calls or composite parts.</summary>
public sealed class AlgorithmStep
{
    /// <summary>Catalog key, or the child key inside a composite.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Scorer that produced <see cref="Points" />.</summary>
    public AlgorithmKind Kind { get; init; }

    /// <summary>Input value that selected the points, when the scorer read one.</summary>
    public string? Input { get; init; }

    /// <summary>Numeric value contributed by this step. Zero when <see cref="Value" /> is not a number.</summary>
    public double Points { get; init; }

    /// <summary>Step value when it is not a number. Omitted from JSON when null so numeric breakdowns stay a points list.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; init; }

    /// <summary>Steps recorded while this step ran.</summary>
    public IReadOnlyList<AlgorithmStep> Children { get; init; } = [];
}

/// <summary>Value plus the steps that produced it.</summary>
public sealed class AlgorithmExecution<T>
{
    /// <summary>Final value.</summary>
    public T Value { get; init; } = default!;

    /// <summary>Top-level steps. Empty when breakdown was not collected.</summary>
    public IReadOnlyList<AlgorithmStep> Steps { get; init; } = [];
}