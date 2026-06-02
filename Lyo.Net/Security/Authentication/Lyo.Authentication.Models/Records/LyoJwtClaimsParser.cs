using System.Security.Claims;
using System.Text.Json;
using Lyo.Authentication.Models.Format;
using Lyo.Common.Extensions;

namespace Lyo.Authentication.Models.Records;

/// <summary>
/// Projects a Lyo-signed JWT payload into a <see cref="ClaimsIdentity" /> for cookie-based or in-browser authentication. Does not verify the signature — callers must only
/// feed in tokens they obtained from a trusted server-to-server exchange. Signature verification happens on the API side when the token is presented as <c>Authorization: Bearer</c>.
/// </summary>
public static class LyoJwtClaimsParser
{
    /// <summary>Parses the JWT payload (middle segment) into a list of claims. Multi-value <c>scope</c> is split into individual claims to match the rest of the Lyo stack.</summary>
    public static IReadOnlyList<Claim> Parse(string jwt)
    {
        if (jwt.IsNullOrWhitespace())
            return Array.Empty<Claim>();

        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return Array.Empty<Claim>();

        byte[] payloadBytes;
        try {
            payloadBytes = Base64Url.Decode(parts[1]);
        }
        catch (FormatException) {
            return Array.Empty<Claim>();
        }

        Dictionary<string, JsonElement>? payload;
        try {
            payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadBytes);
        }
        catch (JsonException) {
            return Array.Empty<Claim>();
        }

        if (payload is null || payload.Count == 0)
            return Array.Empty<Claim>();

        var claims = new List<Claim>(payload.Count + 4);
        foreach (var kvp in payload) {
            if (string.Equals(kvp.Key, LyoJwtClaims.Scope, StringComparison.Ordinal)) {
                var raw = kvp.Value.ValueKind == JsonValueKind.String ? kvp.Value.GetString() : kvp.Value.ToString();
                if (raw.IsNullOrWhitespace())
                    continue;

                foreach (var s in raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    claims.Add(new(LyoJwtClaims.Scope, s));

                continue;
            }

            claims.Add(new(kvp.Key, kvp.Value.ValueKind == JsonValueKind.String ? kvp.Value.GetString() ?? string.Empty : kvp.Value.ToString()));
        }

        return claims;
    }
}