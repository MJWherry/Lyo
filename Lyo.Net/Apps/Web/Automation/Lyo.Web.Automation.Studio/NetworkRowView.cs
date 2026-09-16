using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Lyo.Desktop.Components;
using Lyo.Web.Automation.Playwright.Record;

namespace Lyo.Web.Automation.Studio;

/// <summary>One network row: method, status, and type chips, then the URL.</summary>
internal sealed class NetworkRowView : Grid
{
    private readonly Chip _method = new();
    private readonly Chip _status = new();
    private readonly Chip _type = new();
    private readonly TextBlock _url = new() {
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
        Margin = new(4, 0, 0, 0)
    };

    public NetworkRowView(AutomationStudioNetworkRow row)
    {
        ColumnDefinitions.Add(new(GridLength.Auto));
        ColumnDefinitions.Add(new(GridLength.Auto));
        ColumnDefinitions.Add(new(GridLength.Auto));
        ColumnDefinitions.Add(new(1, GridUnitType.Star));
        Set(0, _method);
        Set(1, _status);
        Set(2, _type);
        Set(3, _url);
        Margin = new(0, 2);
        Apply(row);
    }

    /// <summary>Repaints the chips when a response arrives for the same request.</summary>
    public void Apply(AutomationStudioNetworkRow row)
    {
        Paint(_method, NetworkChip.Method(row.Method));
        Paint(_status, NetworkChip.Status(row.Status));
        Paint(_type, NetworkChip.Type(row.ResourceType));
        _url.Text = row.Url;
    }

    private static void Paint(Chip chip, NetworkChipSpec spec)
    {
        chip.Text = spec.Label;
        chip.Hue = spec.Hue;
    }

    private void Set(int column, Control child)
    {
        Grid.SetColumn(child, column);
        if (column < 3)
            child.Margin = new(0, 0, 4, 0);
        Children.Add(child);
    }
}
