namespace Lyo.Algorithms.Models.Response;

/// <summary>Result of an unsaved preview. <see cref="Value" /> is JSON for the declared result type.</summary>
public sealed class AlgorithmPreviewRes
{
    /// <summary>Final value. A number, string, boolean, or ISO-8601 date, matching <see cref="Result" />.</summary>
    public object? Value { get; set; }

    /// <summary>Result alias, such as <c>double</c> or <c>datetime</c>.</summary>
    public string Result { get; set; } = "double";

    /// <summary>Top-level steps.</summary>
    public List<AlgorithmStepRes> Steps { get; set; } = [];
}
