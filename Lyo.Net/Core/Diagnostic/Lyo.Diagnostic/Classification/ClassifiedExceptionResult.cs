using System.Diagnostics;

namespace Lyo.Diagnostic.Classification;

/// <summary>Result of classifying an exception via <see cref="ExceptionClassifier" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ClassifiedExceptionResult(
    ExceptionKind Kind,
    ExceptionSeverity Severity,
    string Label,
    string RemediationHint,
    bool IsExpectedControlFlow,
    string MatchedTypeName)
{
    public override string ToString() => $"{Kind} [{Severity}] {Label}";
}