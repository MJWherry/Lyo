using System.Text.Json;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Common.Extensions;

namespace Lyo.Authentication.Keycloak;

/// <summary>Maps Keycloak's id_token claims into a <see cref="OidcClaimMappingResult" />, including <c>realm_access.roles</c> → Lyo scope translation.</summary>
public static class KeycloakClaimMapper
{
    /// <summary>Reads the canonical Keycloak claims and maps realm roles to Lyo scopes via <paramref name="rolesToScopes" />.</summary>
    public static OidcClaimMappingResult Map(IReadOnlyDictionary<string, object?> claims, IReadOnlyDictionary<string, string[]> rolesToScopes)
    {
        var name = ReadString(claims, "name");
        var preferred = ReadString(claims, "preferred_username");
        var email = ReadString(claims, "email");
        var displayName = FirstNonBlank(name, preferred, email) ?? "Keycloak user";
        var emailVerified = ReadBool(claims, "email_verified", false);
        var picture = ReadString(claims, "picture");
        var locale = ReadString(claims, "locale");
        var scopes = ExtractScopes(claims, rolesToScopes);
        return new(displayName, email, emailVerified, picture, locale, scopes);
    }

    /// <summary>Extracts realm-role names from <c>realm_access.roles</c> and maps them via <paramref name="rolesToScopes" />.</summary>
    public static IReadOnlyList<string> ExtractScopes(IReadOnlyDictionary<string, object?> claims, IReadOnlyDictionary<string, string[]> rolesToScopes)
    {
        var roles = ReadRealmRoles(claims);
        if (roles.Count == 0)
            return [];

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles) {
            if (rolesToScopes.TryGetValue(role, out var mapped)) {
                foreach (var scope in mapped) {
                    if (!scope.IsNullOrWhitespace())
                        set.Add(scope);
                }
            }
        }

        return set.ToArray();
    }

    /// <summary>Reads <c>realm_access.roles</c> if present. Tolerates the claim arriving as either a <see cref="JsonElement" /> or a plain CLR collection.</summary>
    public static IReadOnlyList<string> ReadRealmRoles(IReadOnlyDictionary<string, object?> claims)
    {
        if (!claims.TryGetValue("realm_access", out var raw) || raw is null)
            return [];

        switch (raw) {
            case JsonElement el when el.ValueKind == JsonValueKind.Object:
                if (el.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array) {
                    var roles = new List<string>(rolesEl.GetArrayLength());
                    foreach (var item in rolesEl.EnumerateArray()) {
                        if (item.ValueKind == JsonValueKind.String) {
                            var s = item.GetString();
                            if (!s.IsNullOrWhitespace())
                                roles.Add(s);
                        }
                    }

                    return roles;
                }

                return [];
            case IDictionary<string, object?> dict when dict.TryGetValue("roles", out var inner) && inner is IEnumerable<object?> seq:
                var fromDict = new List<string>();
                foreach (var item in seq) {
                    if (item is string s && !s.IsNullOrWhitespace())
                        fromDict.Add(s);
                }

                return fromDict;
            default:
                return [];
        }
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

    private static string? FirstNonBlank(params string?[] candidates)
    {
        foreach (var c in candidates) {
            if (!c.IsNullOrWhitespace())
                return c;
        }

        return null;
    }
}