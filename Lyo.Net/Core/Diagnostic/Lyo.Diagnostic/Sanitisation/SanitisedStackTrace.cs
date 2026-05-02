using System.Diagnostics;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Sanitisation;

/// <summary>A sanitised, PII-safe representation of a decoded stack trace. Safe to include in API responses or send to external monitoring systems.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SanitisedStackTrace(
    string? ExceptionMessage,
    IReadOnlyList<SanitisedFrame> Frames,
    string? CrashSite,
    CrashSiteConfidence CrashSiteConfidence,
    string Fingerprint,
    IReadOnlyList<string> UserNamespaces,
    bool HasRecursion,
    IReadOnlyList<SanitisedStackTrace> InnerExceptions)
{
    public override string ToString() => $"{Frames.Count} frame{(Frames.Count == 1 ? "" : "s")} [{CrashSiteConfidence}] fp={Fingerprint}";
}