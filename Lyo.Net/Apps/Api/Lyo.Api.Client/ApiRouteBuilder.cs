namespace Lyo.Api.Client;

/// <summary>Builds Lyo API client paths: applies the host route prefix, then adds <c>include</c> query parameters.</summary>
/// <remarks>Feature clients share a host-configured prefix and EF-style include expansion, so both steps sit here instead of on each client.</remarks>
public static class ApiRouteBuilder
{
    /// <summary>Puts <paramref name="routePrefix" /> in front of <paramref name="relativePath" />, accepting a trailing slash on the prefix.</summary>
    /// <param name="routePrefix">Host mount point, for example <c>api/v1</c>. Null or blank leaves the path as-is.</param>
    /// <param name="relativePath">Path relative to the client, for example <c>jobs/runs</c>.</param>
    /// <returns>The combined request path.</returns>
    public static string Build(string? routePrefix, string relativePath)
        => string.IsNullOrWhiteSpace(routePrefix) ? relativePath : $"{routePrefix!.TrimEnd('/')}/{relativePath}";

    /// <summary>Adds one <c>include</c> query parameter for each navigation to expand.</summary>
    /// <param name="route">Route from <see cref="Build" />.</param>
    /// <param name="includes">Navigation paths to expand. Null or empty leaves the route as-is.</param>
    /// <returns>The route plus include query string, when any includes were given.</returns>
    public static string WithIncludes(string route, IEnumerable<string>? includes)
    {
        if (includes is null)
            return route;

        var paths = includes as string[] ?? includes.ToArray();
        return paths.Length == 0 ? route : $"{route}?include={string.Join("&include=", paths)}";
    }
}
