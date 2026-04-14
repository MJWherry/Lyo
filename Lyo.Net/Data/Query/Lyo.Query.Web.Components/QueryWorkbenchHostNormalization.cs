namespace Lyo.Query.Web.Components;

public static class QueryWorkbenchHostNormalization
{
    public static string NormalizeBaseUrl(string? hostOrAuthority)
    {
        var s = (hostOrAuthority ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return "";

        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return s.TrimEnd('/');

        return "http://" + s.Trim().TrimEnd('/');
    }

    public static QueryWorkbenchRunConfiguration NormalizeRun(QueryWorkbenchRunConfiguration run)
    {
        var dict = QueryWorkbenchRunConfiguration.CloneHostEndpoints(run.HostEndpoints);
        if (dict.Count == 0)
            return run with { HostEndpoints = dict, SelectedHost = string.IsNullOrWhiteSpace(run.SelectedHost) ? null : run.SelectedHost, Route = run.Route.Trim() };

        string? selected = run.SelectedHost;
        if (selected == null
            || !dict.Keys.Any(k => string.Equals(NormalizeBaseUrl(k), NormalizeBaseUrl(selected), StringComparison.OrdinalIgnoreCase)))
            selected = dict.Keys.First();

        var matchKey = dict.Keys.First(k => string.Equals(NormalizeBaseUrl(k), NormalizeBaseUrl(selected), StringComparison.OrdinalIgnoreCase));
        var routes = dict[matchKey];
        var route = run.Route;
        if (routes is { Count: > 0 } && !routes.Contains(route, StringComparer.OrdinalIgnoreCase))
            route = routes[0];

        return run with { HostEndpoints = dict, SelectedHost = matchKey, Route = route };
    }
}
