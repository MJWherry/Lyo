namespace Lyo.Authentication.Models.Format;

/// <summary>Well-known values for the <c>kind</c> segment of a Format-B Lyo token (`lyo_&lt;kind&gt;_&lt;ring&gt;_&lt;id&gt;_&lt;secret&gt;`).</summary>
/// <remarks>Stored as plain string in the database to remain forward-compatible; consumers can introduce additional kinds without a migration.</remarks>
public static class ApiTokenKind
{
    /// <summary>Personal access token. Owned by a Lyo user; minted from the token-management UI.</summary>
    public const string Pat = "pat";

    /// <summary>Service token. Owned by a Lyo user representing a service identity, or unowned for cross-service auth.</summary>
    public const string Svc = "svc";

    /// <summary>CLI token. Owned by a Lyo user, intended for command-line tooling.</summary>
    public const string Cli = "cli";

    /// <summary>Webhook signing token. Used by external systems to sign webhook callbacks back to Lyo.</summary>
    public const string Webhook = "webhook";

    /// <summary>Internal token (e.g. refresh tokens, system-internal flows). Never displayed in user-facing token lists.</summary>
    public const string Internal = "internal";

    /// <summary>All built-in kinds.</summary>
    public static readonly string[] All = [Pat, Svc, Cli, Webhook, Internal];
}