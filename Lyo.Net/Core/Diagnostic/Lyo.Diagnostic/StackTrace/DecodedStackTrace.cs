using System.Diagnostics;

namespace Lyo.Diagnostic.StackTrace;

/// <summary>The fully decoded, structured representation of one exception and its inner chain.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DecodedStackTrace(
    string ExceptionMessage,
    IReadOnlyList<StackFrame> AllFrames,
    IReadOnlyList<StackFrame> UserFrames,
    IReadOnlyList<StackFrame> SystemFrames,
    IReadOnlyList<StackFrame> TestFrames,
    IReadOnlyList<StackFrame> AsyncFrames,
    IReadOnlyList<StackFrame> LambdaFrames,
    StackFrame? LikelyCrashSite,
    CrashSiteConfidence CrashSiteConfidence,
    StackFrame? DeepestUserFrame,
    StackFrame? LastSystemFrame,
    IReadOnlyList<FrameGroup> Groups,
    IReadOnlyList<string> UserNamespaces,
    IReadOnlyList<RecursionInfo> RecursionPatterns,
    string Fingerprint,
    IReadOnlyList<DecodedStackTrace> InnerExceptions)
{
    public int TotalFrameCount => AllFrames.Count;

    public int UserFrameCount => UserFrames.Count;

    public int SystemFrameCount => SystemFrames.Count;

    public int AsyncFrameCount => AsyncFrames.Count;

    public int LambdaFrameCount => LambdaFrames.Count;

    /// <summary>True when at least one user-code frame exists.</summary>
    public bool HasUserCode => UserFrames.Count > 0;

    /// <summary>True when all frames are system / third-party.</summary>
    public bool IsPureSystemTrace => UserFrames.Count == 0 && AllFrames.Count > 0;

    /// <summary>True when recursion was detected.</summary>
    public bool HasRecursion => RecursionPatterns.Count > 0;

    /// <summary>Total depth of the full inner-exception chain.</summary>
    public int InnerExceptionDepth => InnerExceptions.Count;

    public override string ToString()
        => $"{UserFrameCount} user / {SystemFrameCount} system frames [{CrashSiteConfidence}] fp={Fingerprint}";
}