namespace Lyo.Authentication.Models.Records;

/// <summary>The request shape for <see cref="Services.Opaque.IApiTokenIssuer.IssueAsync" />.</summary>
/// <param name="Kind">One of <see cref="Format.ApiTokenKind" />. Determines display routing (PATs vs internal vs webhook).</param>
/// <param name="DisplayName">User-facing label.</param>
/// <param name="Scopes">Snapshotted scopes for this token. Must already be intersected against the caller's effective scopes (the issuer does NOT consult the user store).</param>
/// <param name="UserId">Owning user; <c>null</c> for unowned <c>svc</c>/<c>internal</c> tokens.</param>
/// <param name="Ring">Override the default ring from <see cref="Options.AuthenticationOptions.Ring" />. Usually left <c>null</c>.</param>
/// <param name="Lifetime">Override the default lifetime. <c>null</c> means "use the kind's default"; <see cref="TimeSpan.Zero" /> means "no expiry".</param>
/// <param name="Metadata">Optional metadata bag. Persisted as <c>metadata_json</c>.</param>
/// <param name="RotatedFromId">When this issuance replaces a previous token, set this so the audit chain can be reconstructed.</param>
public sealed record ApiTokenIssueRequest(
    string Kind,
    string DisplayName,
    IReadOnlyList<string> Scopes,
    Guid? UserId = null,
    string? Ring = null,
    TimeSpan? Lifetime = null,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    string? RotatedFromId = null);