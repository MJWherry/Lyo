using Lyo.Authentication.AspNetCore.Defaults;
using Microsoft.AspNetCore.Http;

namespace Lyo.Authentication.AspNetCore.Schemes.Bearer;

/// <summary>Sniffs the credential off the request and chooses the right inner authentication scheme.</summary>
public static class LyoBearerPolicySchemeHandler
{
    /// <summary>Picks <see cref="LyoAuthenticationSchemes.OpaqueToken" /> for credentials starting with <c>lyo_</c>, otherwise <see cref="LyoAuthenticationSchemes.LyoJwt" />.</summary>
    public static string SelectScheme(HttpContext context, LyoBearerPolicySchemeOptions options)
    {
        var token = ExtractCredential(context.Request, options);
        return token.StartsWith("lyo_", StringComparison.Ordinal) ? LyoAuthenticationSchemes.OpaqueToken : LyoAuthenticationSchemes.LyoJwt;
    }

    private static string ExtractCredential(HttpRequest request, LyoBearerPolicySchemeOptions options)
    {
        var header = request.Headers[options.HeaderName].ToString();
        if (!string.IsNullOrEmpty(header)) {
            var prefix = options.Scheme + " ";
            if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return header.Substring(prefix.Length).Trim();
        }

        if (!string.IsNullOrEmpty(options.AlsoAccept)) {
            var raw = request.Headers[options.AlsoAccept!].ToString();
            if (!string.IsNullOrEmpty(raw))
                return raw.Trim();
        }

        return string.Empty;
    }
}