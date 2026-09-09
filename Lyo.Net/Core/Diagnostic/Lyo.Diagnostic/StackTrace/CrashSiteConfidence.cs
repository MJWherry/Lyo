namespace Lyo.Diagnostic.StackTrace;

/// <summary>How confident the decoder is that <see cref="DecodedStackTrace.LikelyCrashSite" /> is the real origin of the exception.</summary>
public enum CrashSiteConfidence
{
    /// <summary>No user-code frames were found.</summary>
    None,

    /// <summary>A user frame was found but it has no source-file info and is async/lambda generated.</summary>
    Low,

    /// <summary>A user frame was found; it has either source info or non-generated code, but not both.</summary>
    Medium,

    /// <summary>The user frame has source-file info and is not a compiler-generated method.</summary>
    High
}