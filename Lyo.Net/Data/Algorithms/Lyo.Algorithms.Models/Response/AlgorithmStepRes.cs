using System.Text.Json.Serialization;

namespace Lyo.Algorithms.Models.Response;

/// <summary>One breakdown step. <see cref="Points" /> is 0 when <see cref="Value" /> is not a number.</summary>
public sealed class AlgorithmStepRes
{
    /// <summary>Catalog key, or the child key inside a composite.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Scorer that produced this step.</summary>
    public AlgorithmKind Kind { get; set; }

    /// <summary>Input text that selected the value, when the scorer read one.</summary>
    public string? Input { get; set; }

    /// <summary>Numeric contribution. Zero when the step value is not a number.</summary>
    public double Points { get; set; }

    /// <summary>Step value when it is not a number. Omitted when null so a numeric breakdown stays a points list.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    /// <summary>Steps recorded while this step ran.</summary>
    public List<AlgorithmStepRes> Children { get; set; } = [];
}
