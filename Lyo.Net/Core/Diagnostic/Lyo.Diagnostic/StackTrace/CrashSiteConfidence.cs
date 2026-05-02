namespace Lyo.Diagnostic.StackTrace;

/// <summary>How confident the decoder is that <see cref="DecodedStackTrace.LikelyCrashSite" /> is the actual origin of the exception.</summary>
public enum CrashSiteConfidence
{
    /// <summary>No user-code frames were found.</summary>
    None,

    /// <summary>User frame found but no source-file info and is async/lambda generated.</summary>
    Low,

    /// <summary>User frame found; either source info or non-generated code, but not both.</summary>
    Medium,

    /// <summary>User frame has source-file info and is not a compiler-generated method.</summary>
    High
}