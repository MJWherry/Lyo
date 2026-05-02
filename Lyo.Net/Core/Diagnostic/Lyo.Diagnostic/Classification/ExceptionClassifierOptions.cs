using System.Diagnostics;

namespace Lyo.Diagnostic.Classification;

/// <summary>Configures <see cref="ExceptionClassifier" /> behaviour.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ExceptionClassifierOptions
{
    /// <summary>Additional mappings from exception type name (simple or fully-qualified) to <see cref="ExceptionKind" />. These take priority over built-in rules.</summary>
    public IReadOnlyDictionary<string, ExceptionKind> CustomMappings { get; init; } = new Dictionary<string, ExceptionKind>();

    /// <summary>Default singleton with no customisation.</summary>
    public static ExceptionClassifierOptions Default { get; } = new();

    public override string ToString() => $"CustomMappings={CustomMappings.Count}";
}