using Lyo.Authentication.OpenIdConnect.Provider;

namespace Lyo.Authentication.Google;

/// <summary>Maps Google's id_token claims into a <see cref="OidcClaimMappingResult" />.</summary>
public static class GoogleClaimMapper
{
    /// <summary>Reads the canonical Google claims (<c>name</c>, <c>email</c>, <c>email_verified</c>, <c>picture</c>, <c>locale</c>).</summary>
    public static OidcClaimMappingResult Map(IReadOnlyDictionary<string, object?> claims)
    {
        var name = ReadString(claims, "name");
        var email = ReadString(claims, "email");
        var displayName = string.IsNullOrWhiteSpace(name) ? string.IsNullOrWhiteSpace(email) ? "Google user" : email! : name!;
        var emailVerified = ReadBool(claims, "email_verified", false);
        var picture = ReadString(claims, "picture");
        var locale = ReadString(claims, "locale");
        return new(displayName, email, emailVerified, picture, locale, []);
    }

    private static string? ReadString(IReadOnlyDictionary<string, object?> claims, string key) => claims.TryGetValue(key, out var v) ? v?.ToString() : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> claims, string key, bool defaultIfMissing)
    {
        if (!claims.TryGetValue(key, out var v) || v is null)
            return defaultIfMissing;

        if (v is bool b)
            return b;

        if (v is string s && bool.TryParse(s, out var parsed))
            return parsed;

        return defaultIfMissing;
    }
}