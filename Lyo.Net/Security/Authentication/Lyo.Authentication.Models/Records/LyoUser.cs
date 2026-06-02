using System.Diagnostics;

namespace Lyo.Authentication.Models.Records;

/// <summary>A Lyo-internal user. Backed by the <c>[user].[user]</c> table when persisted via <c>Lyo.Authentication.Postgres</c>.</summary>
/// <param name="Id">Stable Lyo user identifier. Forms the <c>sub</c> claim on issued JWTs (<c>lyo_user:&lt;guid&gt;</c>).</param>
/// <param name="DisplayName">Human-readable name shown in UI. Free-form.</param>
/// <param name="Email">Primary email. Case-insensitive unique within the store.</param>
/// <param name="EmailVerified"><c>true</c> once any linked identity has proven ownership of the email via a verified provider claim.</param>
/// <param name="AvatarUrl">Optional URL from a provider <c>picture</c> claim.</param>
/// <param name="PreferredLanguageBcp47">Optional BCP-47 language tag from a provider <c>locale</c> claim.</param>
/// <param name="Scopes">Admin-managed baseline scopes the user holds independently of any external identity.</param>
/// <param name="Metadata">App-attached key/value metadata.</param>
/// <param name="PersonId">Optional soft pointer to a <c>Lyo.People.Person</c> record. No cross-schema FK.</param>
/// <param name="CreatedAt">When the row was first written.</param>
/// <param name="UpdatedAt">When the row was last updated.</param>
/// <param name="LastLoginAt">When the user most recently completed a login or refreshed a JWT.</param>
/// <param name="DisabledAt">When set, ALL existing tokens AND JWTs for this user are rejected immediately (Option C kill-switch).</param>
/// <param name="DisabledReason">Optional human-readable reason for the disable (audit/UX).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoUser(
    Guid Id,
    string DisplayName,
    string Email,
    bool EmailVerified,
    string? AvatarUrl,
    string? PreferredLanguageBcp47,
    IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, object?>? Metadata,
    Guid? PersonId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastLoginAt,
    DateTime? DisabledAt,
    string? DisabledReason)
{
    /// <summary>True if the user is currently disabled and all of their credentials must be rejected.</summary>
    public bool IsDisabled => DisabledAt.HasValue;

    public override string ToString() => $"LyoUser: id={Id}, email={Email}, name={DisplayName}, disabled={IsDisabled}";
}