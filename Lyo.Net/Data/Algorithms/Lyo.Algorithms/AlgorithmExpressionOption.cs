using System.Diagnostics;

namespace Lyo.Algorithms;

/// <summary>One expression compiled into the startup store from configuration. Context is <see cref="AlgorithmContextBag" />; the result is <see cref="object" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AlgorithmExpressionOption
{
    /// <summary>Startup key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Math expression, for example <c>Atk * 2 - Def</c>.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Result alias. Null keeps the startup expression result as <see cref="object" />.</summary>
    public string? Result { get; set; }

    /// <summary>Identifier aliases. Omitted names are <see cref="double" />, except <c>_key</c> names, which are <see cref="string" />.</summary>
    public Dictionary<string, string> Parameters { get; set; } = [];

    /// <summary><c>Algo</c> and <c>score</c> result aliases. Omitted calls are <see cref="double" />.</summary>
    public Dictionary<string, string> Calls { get; set; } = [];

    /// <summary>Key, result alias, and expression length. The expression text is omitted.</summary>
    public override string ToString()
        => $"Key={Key} Result={Result} ExpressionLength={Expression?.Length} Parameters={Parameters.Count} Calls={Calls.Count}";
}