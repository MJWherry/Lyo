namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>Tuning knobs for <see cref="DefaultExternalLoginCoordinator" />.</summary>
public sealed class ExternalLoginOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "LyoExternalLogin";

    /// <summary>How to handle a never-before-seen (provider, subject) pair.</summary>
    public ExternalLoginPolicy Policy { get; set; } = ExternalLoginPolicy.JustInTime;

    /// <summary>When <see cref="Policy" /> is <see cref="ExternalLoginPolicy.JitFromAllowedClaim" />, the claim name to inspect (e.g. <c>hd</c>).</summary>
    public string? AllowedClaimName { get; set; }

    /// <summary>The allowed values for <see cref="AllowedClaimName" />. Any match passes.</summary>
    public IList<string> AllowedClaimValues { get; set; } = [];

    /// <summary>When <c>true</c>, reject providers that report <c>email_verified=false</c>. Default <c>true</c>.</summary>
    public bool RequireVerifiedEmail { get; set; } = true;

    /// <summary>
    /// Baseline scopes stamped onto every <c>LyoUser</c> that this coordinator provisions. Use this to give freshly-onboarded users a starting set of self-service permissions
    /// (e.g. <c>auth.tokens.read</c> + <c>auth.tokens.write</c> so they can mint their own PATs without an out-of-band admin grant). Has no effect on users that already exist when a
    /// fresh OIDC login arrives — existing user rows are not touched.
    /// </summary>
    public IList<string> DefaultUserScopes { get; set; } = [];
}