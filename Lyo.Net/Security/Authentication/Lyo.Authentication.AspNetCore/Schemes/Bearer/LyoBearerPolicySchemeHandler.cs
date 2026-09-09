using Lyo.Authentication.AspNetCore.Defaults;
using Lyo.Common.Core.Extensions;
using Microsoft.AspNetCore.Http;

namespace Lyo.Authentication.AspNetCore.Schemes.Bearer;

/// <summary>Sniffs the credential on the request and picks the matching inner authentication scheme.</summary>
public static class LyoBearerPolicySchemeHandler
{
    /// <summary>Picks <see cref="LyoAuthenticationSchemes.OpaqueToken" /> for <c>lyo_</c> prefixes, otherwise <see cref="LyoAuthenticationSchemes.LyoJwt" />.</summary>
    public static string SelectScheme(HttpContext context, LyoBearerPolicySchemeOptions options)
    {
        var token = ExtractCredential(context.Request, options);
        return token.StartsWith("lyo_", StringComparison.Ordinal) ? LyoAuthenticationSchemes.OpaqueToken : LyoAuthenticationSchemes.LyoJwt;
    }

    private static string ExtractCredential(HttpRequest request, LyoBearerPolicySchemeOptions options)
    {
        var header = request.Headers[options.HeaderName].ToString();
        if (!header.IsNullOrEmpty()) {
            var prefix = options.Scheme + " ";
            if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return header.Substring(prefix.Length).Trim();
        }

        if (!options.AlsoAccept.IsNullOrEmpty()) {
            var raw = request.Headers[options.AlsoAccept].ToString();
            if (!raw.IsNullOrEmpty())
                return raw.Trim();
        }

        return string.Empty;
    }
}