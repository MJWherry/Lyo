namespace Lyo.Reporting.Web.Components;

/// <summary>
/// Builds the sample context a formatter-typed report parameter is offered in the template editor.
/// </summary>
/// <remarks>
/// Reporting has no format pass of its own. Nothing in <c>Lyo.Reporting.Postgres</c> runs a parameter value through <c>IFormatterService</c> the way
/// <c>JobScheduler</c> does, so a formatter-typed report parameter is resolved by whatever consumes the generated value. The only data this editor can honestly offer is
/// the definition being edited and the sibling parameters alongside it; nothing run-related is invented, so every token listed is one that actually exists.
/// </remarks>
public static class ReportFormatterContext
{
    /// <summary>
    /// Context exposing the definition as <c>Definition</c> and the sibling parameters both as a <c>Parameters</c> map and as flat <c>Parameter_{key}</c> entries, so a
    /// template can reference a sibling in either form.
    /// </summary>
    /// <param name="definition">Definition being edited or generated. Null yields a context with only the parameter entries.</param>
    /// <param name="parameters">Sibling keys and their current values. Pass the in-progress editor values so the preview tracks what the user has typed.</param>
    /// <returns>A case-insensitive dictionary suitable for a <c>FormatterContext</c> parameter.</returns>
    public static Dictionary<string, object?> Build(ReportDefinitionRes? definition, IEnumerable<KeyValuePair<string, string?>>? parameters = null)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in parameters ?? [])
            values[key] = value;

        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["Definition"] = definition,
            ["Parameters"] = values
        };

        foreach (var (key, value) in values)
            data[$"Parameter_{key}"] = value;

        return data;
    }
}
