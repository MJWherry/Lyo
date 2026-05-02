using System.Diagnostics;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Sanitisation;

/// <summary>A sanitised version of a <see cref="Diagnostic.StackTrace.StackFrame" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SanitisedFrame(string ShortMethod, string? LocationSummary, FrameCategory Category, bool IsAsync, bool IsLambda)
{
    public override string ToString() => LocationSummary is null ? $"{ShortMethod} [{Category}]" : $"{ShortMethod} [{Category}] @ {LocationSummary}";
}