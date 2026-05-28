namespace Lyo.Authentication.Models.Format;

/// <summary>Well-known values for the <c>ring</c> segment of a Format-B Lyo token. Identifies which deployment ring the token was issued for.</summary>
/// <remarks>Validators MUST reject tokens whose ring does not match the runtime ring to prevent dev/test credentials from accidentally working in production.</remarks>
public static class ApiTokenRing
{
    /// <summary>Production ring.</summary>
    public const string Live = "live";

    /// <summary>Staging / pre-production ring.</summary>
    public const string Test = "test";

    /// <summary>Local development ring.</summary>
    public const string Dev = "dev";

    /// <summary>All built-in rings.</summary>
    public static readonly string[] All = [Live, Test, Dev];
}
