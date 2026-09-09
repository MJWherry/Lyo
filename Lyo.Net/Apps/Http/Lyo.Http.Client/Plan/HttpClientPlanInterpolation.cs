using System.Text.RegularExpressions;

namespace Lyo.Http.Client.Plan;

/// <summary>Replaces <c>{{name}}</c> from string bindings.</summary>
public static class HttpClientPlanInterpolation
{
    private static readonly Regex Placeholder = new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled);

    /// <summary>Interpolates <paramref name="template" /> using <paramref name="bindings" />. Unknown keys stay as-is.</summary>
    public static string Interpolate(string? template, IReadOnlyDictionary<string, string> bindings)
    {
        if (string.IsNullOrEmpty(template))
            return template ?? "";

        return Placeholder.Replace(template, m => {
            var key = m.Groups[1].Value.Trim();
            return bindings.TryGetValue(key, out var value) ? value : m.Value;
        });
    }
}
