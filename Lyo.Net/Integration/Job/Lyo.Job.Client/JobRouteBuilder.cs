namespace Lyo.Job.Client;

internal static class JobRouteBuilder
{
    public static string Build(string? routePrefix, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(routePrefix))
            return relativePath;

        return $"{routePrefix.TrimEnd('/')}/{relativePath}";
    }

    public static string WithIncludes(string route, IEnumerable<string>? includes)
    {
        if (includes is null)
            return route;

        var arr = includes as string[] ?? includes.ToArray();
        if (arr.Length == 0)
            return route;

        return $"{route}?include={string.Join("&include=", arr)}";
    }
}
