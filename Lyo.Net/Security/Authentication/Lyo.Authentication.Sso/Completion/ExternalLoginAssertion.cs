namespace Lyo.Authentication.Sso.Completion;

/// <summary>Protocol-neutral identity produced after a provider validates a login. Completion links this to a Lyo user and mints one JWT.</summary>
/// <param name="Provider">Provider name stored on the linked identity.</param>
/// <param name="Subject">Stable provider subject. This is the link key.</param>
/// <param name="DisplayName">Name written onto a newly provisioned user.</param>
/// <param name="Email">Email from the provider, when the profile mapped one.</param>
/// <param name="EmailVerified">True only when the profile's mapped email attribute was verified by that protocol.</param>
/// <param name="AvatarUrl">Optional avatar URL.</param>
/// <param name="PreferredLanguageBcp47">Optional BCP-47 language tag.</param>
/// <param name="ProviderScopes">Scopes stored on the link. They are not copied onto the Lyo JWT.</param>
/// <param name="RawClaims">Provider claims. A value is a <see cref="string" /> or an <see cref="IReadOnlyList{T}" /> of strings.</param>
public sealed record ExternalLoginAssertion(
    string Provider,
    string Subject,
    string DisplayName,
    string? Email,
    bool EmailVerified,
    string? AvatarUrl,
    string? PreferredLanguageBcp47,
    IReadOnlyList<string> ProviderScopes,
    IReadOnlyDictionary<string, object?> RawClaims);