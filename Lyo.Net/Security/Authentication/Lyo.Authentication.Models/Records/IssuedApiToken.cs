namespace Lyo.Authentication.Models.Records;

/// <summary>The result of <see cref="Services.Opaque.IApiTokenIssuer.IssueAsync" />: the wire-form plaintext (shown to the user once) plus the row persisted to the store.</summary>
/// <param name="Plaintext">The full wire-form token (e.g. <c>lyo_pat_live_01hxy8k2qf9_4f3b...</c>). Shown to the caller exactly once and then discarded.</param>
/// <param name="Record">The persisted record (with hashed secret only).</param>
public sealed record IssuedApiToken(string Plaintext, ApiTokenRecord Record);