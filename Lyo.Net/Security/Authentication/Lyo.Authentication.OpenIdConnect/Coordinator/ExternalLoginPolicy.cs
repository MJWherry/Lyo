namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>Controls what happens when a successful OIDC login arrives for a (provider, subject) tuple that is not yet linked to a Lyo user.</summary>
public enum ExternalLoginPolicy
{
    /// <summary>Auto-create the Lyo user from id_token claims. Default for most deployments.</summary>
    JustInTime,

    /// <summary>Require a pre-existing Lyo user with the same email; reject otherwise. Suitable for closed deployments.</summary>
    RequireExistingUser,

    /// <summary>Auto-create only when an allow-list claim matches (e.g. Google <c>hd</c> equals the org domain).</summary>
    JitFromAllowedClaim
}