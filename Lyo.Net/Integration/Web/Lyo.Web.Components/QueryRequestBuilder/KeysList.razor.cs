using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class KeysList
{
    [Parameter]
    public List<object[]> Keys { get; set; } = [];

    [Parameter]
    public EventCallback<List<object[]>> KeysChanged { get; set; }

    [Parameter]
    public IEnumerable<string> KeysAll { get; set; } = [];

    [Parameter]
    public EventCallback<IEnumerable<string>> KeysAllChanged { get; set; }

    private static string FormatKeyPart(object? value)
    {
        if (value == null)
            return "null";

        if (value is string stringValue)
            return $"\"{stringValue}\"";

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return $"\"{jsonElement.GetString() ?? ""}\"";

        return value.ToString() ?? "null";
    }

    private static string FormatKeySet(object[] keySet) => string.Join(", ", keySet.Select(FormatKeyPart));

    private IEnumerable<string> GetValuesList()
    {
        var keysFormatted = Keys.Select(FormatKeySet).ToList();
        var keysAllList = KeysAll.ToList();
        return keysAllList.Count > keysFormatted.Count ? keysAllList : keysFormatted;
    }

    private List<string> GetSelectedValuesList() => Keys.Select(FormatKeySet).ToList();

    private static object[] ParseKeyInput(string input)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return [];

        return trimmed.Split(',')
            .Select(part => {
                var normalizedPart = part.Trim();
                if (normalizedPart.Length >= 2 && normalizedPart[0] == '"' && normalizedPart[^1] == '"')
                    normalizedPart = normalizedPart[1..^1].Replace("\\\"", "\"");

                if (long.TryParse(normalizedPart, out var longValue))
                    return longValue;

                if (decimal.TryParse(normalizedPart, out var decimalValue))
                    return (object)decimalValue;

                return normalizedPart;
            })
            .ToArray();
    }

    private async Task OnValuesChanged(IEnumerable<string> values)
    {
        var seen = new HashSet<string>();
        var keys = new List<object[]>();
        foreach (var value in values) {
            var key = ParseKeyInput(value);
            if (key.Length == 0)
                continue;

            var formatted = FormatKeySet(key);
            if (!seen.Add(formatted))
                continue;

            keys.Add(key);
        }

        var formattedList = keys.Select(FormatKeySet).ToList();
        if (KeysAllChanged.HasDelegate)
            await KeysAllChanged.InvokeAsync(formattedList);

        await KeysChanged.InvokeAsync(keys);
    }

    private async Task OnSelectedChanged(IEnumerable<string> selected)
    {
        var keys = selected.Select(ParseKeyInput).Where(key => key.Length > 0).ToList();
        if (KeysAllChanged.HasDelegate)
            await KeysAllChanged.InvokeAsync(GetValuesList().ToList());

        await KeysChanged.InvokeAsync(keys);
    }
}