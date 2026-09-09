namespace Lyo.Diagnostic.StackTrace;

/// <summary>Broad origin category of one stack frame.</summary>
public enum FrameCategory
{
    /// <summary>Code written by the application author.</summary>
    UserCode,

    /// <summary>BCL, Microsoft platform assemblies, and frames matching configured third-party prefixes.</summary>
    SystemOrThirdParty,

    /// <summary>Test-framework infrastructure (NUnit, xUnit, MSTest).</summary>
    TestFramework
}