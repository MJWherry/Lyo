namespace Lyo.Algorithms;

/// <summary>One expression compiled into the startup store from configuration. Context is <see cref="AlgorithmContextBag" />; the result is <see cref="object" />.</summary>
public sealed class AlgorithmExpressionOption
{
    /// <summary>Startup key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Math expression, for example <c>Atk * 2 - Def</c>.</summary>
    public string Expression { get; set; } = string.Empty;
}
