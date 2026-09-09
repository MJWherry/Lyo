using System.Text.Json;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Lyo.Authentication.Web.Components;

/// <summary>Compact Guid cell that prefers <see cref="LyoIdField" /> <c>Value</c> so QueryProject Guid JSON actually renders.</summary>
public partial class AuthProjectedGuid
{
    [Parameter]
    public object? Item { get; set; }

    [Parameter]
    public string Field { get; set; } = "UserId";

    private Guid? _value;

    protected override void OnParametersSet() => _value = TryRead(Item, Field);

    internal static Guid? TryRead(object? item, string field)
    {
        var raw = ProjectedValueHelper.GetValue(item, field);
        if (TryGuid(raw, out var guid))
            return guid;

        if (item is not JsonElement { ValueKind: JsonValueKind.Object } je)
            return null;

        foreach (var prop in je.EnumerateObject()) {
            if (!prop.Name.Equals(field, StringComparison.OrdinalIgnoreCase))
                continue;

            return TryGuid(prop.Value, out guid) ? guid : null;
        }

        return null;
    }

    private static bool TryGuid(object? raw, out Guid guid)
    {
        guid = default;
        switch (raw) {
            case Guid g:
                guid = g;
                return true;
            case JsonElement je when je.ValueKind == JsonValueKind.String && je.TryGetGuid(out guid):
                return true;
            case JsonElement je when je.ValueKind == JsonValueKind.String && Guid.TryParse(je.GetString(), out guid):
                return true;
            case string s when Guid.TryParse(s, out guid):
                return true;
            default:
                return ProjectedValueHelper.TryGetGuid(raw, out guid);
        }
    }
}
