namespace Lyo.Authentication.Models.Audit;

/// <summary>
/// Closed taxonomy of audit-worthy moments in the Lyo authentication subsystem. Stored as the kind column in <c>[user].[event]</c> when persistence is wired up, so this enum is
/// effectively part of the schema — only append new members at the end, never renumber or rename.
/// </summary>
public enum AuthAuditEventKind
{
    /// <summary>Catch-all fallback. Should never be persisted in practice; means "the caller passed an out-of-band value".</summary>
    Unknown = 0,

    /// <summary>A Lyo user was just provisioned (typically the first time we see them on an external identity provider).</summary>
    UserProvisioned = 1,

    /// <summary>An external identity (provider/subject) was linked to a Lyo user.</summary>
    IdentityLinked = 2,

    /// <summary>An external identity was unlinked from a Lyo user.</summary>
    IdentityUnlinked = 3,

    /// <summary>A user successfully completed an external login (callback succeeded, JWT minted).</summary>
    ExternalLoginSucceeded = 4,

    /// <summary>An external login was rejected (state/nonce/signature/policy failure, disabled user, unverified email, etc.).</summary>
    ExternalLoginRejected = 5,

    /// <summary>A Lyo JWT was issued for a user (login or refresh).</summary>
    JwtIssued = 6,

    /// <summary>A handoff code was minted for a successful browser login. Records the (sessionless) issuance, separate from the JWT issuance event.</summary>
    HandoffCodeIssued = 7,

    /// <summary>A handoff code was redeemed successfully.</summary>
    HandoffCodeConsumed = 8,

    /// <summary>A handoff code redemption was rejected (unknown id, wrong origin, already consumed, expired).</summary>
    HandoffCodeRejected = 9,

    /// <summary>An opaque API token (Format-B) was issued.</summary>
    TokenIssued = 10,

    /// <summary>A presented opaque API token was validated successfully.</summary>
    TokenValidated = 11,

    /// <summary>A presented opaque API token was rejected.</summary>
    TokenRejected = 12,

    /// <summary>An opaque API token was revoked (rotation, explicit revoke, logout, theft).</summary>
    TokenRevoked = 13,

    /// <summary>A refresh-token exchange succeeded (rotated to a new pair).</summary>
    RefreshSucceeded = 14,

    /// <summary>A refresh-token exchange was rejected.</summary>
    RefreshRejected = 15,

    /// <summary>A user signed out — either via the consumer's sign-out path (revoking the refresh token) or via an admin disable.</summary>
    SignedOut = 16,

    /// <summary>A user was disabled (Option-C kill switch).</summary>
    UserDisabled = 17,

    /// <summary>A user was re-enabled.</summary>
    UserEnabled = 18,

    /// <summary>The Ed25519 signing key was bootstrapped (typically on first start).</summary>
    SigningKeyBootstrapped = 19,

    /// <summary>The Ed25519 signing key was rotated (new version added + set current).</summary>
    SigningKeyRotated = 20
}
