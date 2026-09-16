namespace Lyo.RateLimit;

/// <summary>Decides whether a request is on a quota whitelist. The store never sees this list.</summary>
public static class RateLimitEndpointMatcher
{
    /// <summary>
    /// True when <paramref name="whitelist" /> exempts this call. An entry is an endpoint name (<c>GetPerson</c>) or <c>METHOD /route</c>
    /// (<c>GET /health</c>, <c>POST /person/{id}</c>). A trailing <c>/*</c> is a path prefix. Query strings and a trailing slash are ignored.
    /// </summary>
    /// <param name="whitelist">Exempt endpoints. Null or empty exempts nothing.</param>
    /// <param name="endpointName">Route endpoint name, when the host assigned one.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="routeTemplate">Route template such as <c>/person/{id}</c>. Null for outbound calls.</param>
    /// <param name="requestPath">Path actually requested, such as <c>/person/3</c>.</param>
    public static bool IsWhitelisted(IEnumerable<string>? whitelist, string? endpointName, string method, string? routeTemplate, string? requestPath)
    {
        if (whitelist is null)
            return false;

        var path = NormalizePath(requestPath);
        var template = NormalizePath(routeTemplate);
        foreach (var entry in whitelist) {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            var trimmed = entry.Trim();
            var space = trimmed.IndexOf(' ');
            if (space < 0) {
                if (endpointName is not null && string.Equals(trimmed, endpointName, StringComparison.Ordinal))
                    return true;

                continue;
            }

            var entryMethod = trimmed[..space];
            if (!entryMethod.Equals(method, StringComparison.OrdinalIgnoreCase))
                continue;

            var entryPath = NormalizePath(trimmed[(space + 1)..]);
            if (PathMatches(entryPath, path) || PathMatches(entryPath, template))
                return true;
        }

        return false;
    }

    private static bool PathMatches(string pattern, string actual)
    {
        if (pattern.Length == 0 || actual.Length == 0)
            return false;

        if (pattern.EndsWith("/*", StringComparison.Ordinal)) {
            var prefix = NormalizePath(pattern[..^2]);
            return string.Equals(actual, prefix, StringComparison.Ordinal)
                || actual.StartsWith(prefix + "/", StringComparison.Ordinal);
        }

        if (string.Equals(pattern, actual, StringComparison.Ordinal))
            return true;

        var patternParts = pattern.Split('/');
        var actualParts = actual.Split('/');
        if (patternParts.Length != actualParts.Length)
            return false;

        for (var i = 0; i < patternParts.Length; i++) {
            var expected = patternParts[i];
            if (expected.Length >= 2 && expected[0] == '{' && expected[expected.Length - 1] == '}')
                continue;

            if (!string.Equals(expected, actualParts[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var value = path.Trim();
        var query = value.IndexOf('?');
        if (query >= 0)
            value = value[..query];

        if (value.Length > 1)
            value = value.TrimEnd('/');

        return value.Length == 0 ? "/" : value;
    }
}
