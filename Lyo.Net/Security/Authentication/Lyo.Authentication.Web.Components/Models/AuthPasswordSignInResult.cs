namespace Lyo.Authentication.Web.Components.Models;

/// <summary>Outcome of an <see cref="Abstractions.IAuthPasswordSignIn.SignInAsync" /> attempt. Discriminated-union-style using a single record + a static factory pair.</summary>
/// <param name="Succeeded">True when sign-in completed.</param>
/// <param name="ReturnUrl">Where to navigate after success (overrides the page's own return URL when non-null).</param>
/// <param name="FailureReason">Human-readable reason rendered as a <c>MudAlert</c> on failure.</param>
public sealed record AuthPasswordSignInResult(bool Succeeded, string? ReturnUrl, string? FailureReason)
{
    /// <summary>A successful result, optionally overriding the return URL the page already has.</summary>
    public static AuthPasswordSignInResult Success(string? returnUrl = null) => new(true, returnUrl, null);

    /// <summary>A failed result carrying a user-facing reason string.</summary>
    public static AuthPasswordSignInResult Failure(string reason) => new(false, null, reason);
}