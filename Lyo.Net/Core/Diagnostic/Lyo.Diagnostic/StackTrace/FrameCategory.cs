namespace Lyo.Diagnostic.StackTrace;

/// <summary>Broad origin category of a single stack frame.</summary>
public enum FrameCategory
{
    /// <summary>Code written by the application developer.</summary>
    UserCode,

    /// <summary>BCL, ASP.NET Core, EF Core, or other well-known library frame.</summary>
    SystemOrThirdParty,

    /// <summary>Test framework infrastructure (NUnit, xUnit, MSTest).</summary>
    TestFramework
}