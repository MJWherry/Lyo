using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lyo.Desktop.Components;

/// <summary>Abbreviated identifier with an eye toggle and copy, matching the web id field.</summary>
public class IdText : Grid
{
    private const string EyeOutline = "M1,8 C3.2,4.2 12.8,4.2 15,8 C12.8,11.8 3.2,11.8 1,8";
    private const string EyeSlash = "M2.5,13.5 L13.5,2.5";

    /// <summary>Full identifier. Blank draws a dash and hides the buttons.</summary>
    public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<IdText, string?>(nameof(Value));

    private readonly TextBlock _text = new() { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
    private readonly Button _toggle;
    private readonly Button _copy;
    private bool _full;

    public IdText()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _toggle = new Button { Padding = new Thickness(2), Width = 22, Height = 22, VerticalAlignment = VerticalAlignment.Center };
        _copy = Small("Copy");
        _toggle.Click += (_, _) => {
            _full = !_full;
            Refresh();
        };
        _copy.Click += async (_, _) => {
            var value = Value?.Trim();
            if (string.IsNullOrEmpty(value))
                return;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
                return;
            var transfer = new DataTransfer();
            var item = new DataTransferItem();
            item.SetText(value);
            transfer.Add(item);
            await clipboard.SetDataAsync(transfer);
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        buttons.Children.Add(_toggle);
        buttons.Children.Add(_copy);
        SetColumn(buttons, 1);
        Children.Add(_text);
        Children.Add(buttons);
        Refresh();
    }

    /// <summary>Full identifier.</summary>
    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Last <paramref name="length" /> characters. Digit-only values and short values stay whole.</summary>
    public static string Abbreviate(string? value, int length = 9)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var text = value.Trim();
        if (length < 1 || text.Length <= length || Digits(text))
            return text;
        return text[^length..];
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) {
            _full = false;
            Refresh();
        }
    }

    private void Refresh()
    {
        var full = Value?.Trim() ?? "";
        var toggle = full.Length > 9 && !Digits(full);
        _toggle.IsVisible = toggle;
        _copy.IsVisible = full.Length > 0;
        _toggle.Content = Eye(_full);
        ToolTip.SetTip(_toggle, _full ? "Hide id" : "Show id");
        _text.Text = full.Length == 0 ? "-" : _full || !toggle ? full : Abbreviate(full);
    }

    private Control Eye(bool revealed)
    {
        var icon = new Grid { Width = 16, Height = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        icon.Children.Add(Stroke(EyeOutline));
        var pupil = new Ellipse { Width = 4, Height = 4, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        pupil.Bind(Shape.FillProperty, _toggle.GetObservable(TemplatedControl.ForegroundProperty));
        icon.Children.Add(pupil);
        if (!revealed)
            icon.Children.Add(Stroke(EyeSlash));
        return icon;
    }

    private Avalonia.Controls.Shapes.Path Stroke(string data)
    {
        var shape = new Avalonia.Controls.Shapes.Path {
            Data = Geometry.Parse(data),
            StrokeThickness = 1.4,
            StrokeLineCap = PenLineCap.Round,
            Stretch = Stretch.Uniform
        };
        shape.Bind(Shape.StrokeProperty, _toggle.GetObservable(TemplatedControl.ForegroundProperty));
        return shape;
    }

    private static Button Small(string label)
        => new() { Content = label, Padding = new Thickness(6, 0), FontSize = 11, Height = 22 };

    private static bool Digits(string value)
    {
        foreach (var c in value)
            if (!char.IsDigit(c))
                return false;
        return value.Length > 0;
    }
}
