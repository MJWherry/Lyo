using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Lyo.Exceptions;

namespace Lyo.Desktop.Components;

/// <summary>One row in a <see cref="KeyValueView" />. The value is text, an image, or any control.</summary>
public sealed class KeyValueEntry
{
    /// <summary>Left label. Blank hides the label and shows only the value.</summary>
    public required string Key { get; init; }

    /// <summary>Selectable text. Used when <see cref="Image" /> and <see cref="View" /> are both null.</summary>
    public string? Text { get; init; }

    /// <summary>Picture shown as the value.</summary>
    public IImage? Image { get; init; }

    /// <summary>Any control shown as the value, such as a <see cref="Chip" />.</summary>
    public Control? View { get; init; }

    /// <summary>Text placed on the clipboard by the row's Copy button. Null hides the button.</summary>
    public string? CopyText { get; init; }

    /// <summary>Text the Copy button writes. Null when the row has nothing to copy.</summary>
    public string? ClipboardText => string.IsNullOrWhiteSpace(CopyText) ? null : CopyText;

    /// <summary>A text value. Blank text draws a dash and has no Copy button.</summary>
    public static KeyValueEntry FromText(string key, string? text)
    {
        var value = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return new() { Key = key, Text = value, CopyText = value };
    }

    /// <summary>An image value. <paramref name="copyText" /> is what Copy writes, such as a URL.</summary>
    public static KeyValueEntry FromImage(string key, IImage image, string? copyText = null)
    {
        ArgumentHelpers.ThrowIfNull(image);
        return new() { Key = key, Image = image, CopyText = string.IsNullOrWhiteSpace(copyText) ? null : copyText.Trim() };
    }

    /// <summary>A control value. <paramref name="copyText" /> is what Copy writes.</summary>
    public static KeyValueEntry FromControl(string key, Control view, string? copyText = null)
    {
        ArgumentHelpers.ThrowIfNull(view);
        return new() { Key = key, View = view, CopyText = string.IsNullOrWhiteSpace(copyText) ? null : copyText.Trim() };
    }

    /// <summary>A chip value. Copy writes <paramref name="copyText" /> or the chip label.</summary>
    public static KeyValueEntry FromChip(string key, string label, int hue = -1, string? copyText = null)
        => FromControl(key, new Chip { Text = label, Hue = hue }, copyText ?? label);
}

/// <summary>
/// Key and value rows. A value is selectable text, an image, or any control. Text can be selected, and a Copy button writes <see cref="KeyValueEntry.ClipboardText" />.
/// </summary>
public class KeyValueView : UserControl
{
    private static readonly FontFamily Mono = new("Consolas, Menlo, DejaVu Sans Mono");
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xB2));
    private static readonly IBrush Line = new SolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x3A));

    /// <summary>Rows to show. Null or empty draws nothing.</summary>
    public static readonly StyledProperty<IReadOnlyList<KeyValueEntry>?> EntriesProperty =
        AvaloniaProperty.Register<KeyValueView, IReadOnlyList<KeyValueEntry>?>(nameof(Entries));

    private readonly StackPanel _rows = new();

    public KeyValueView() => Content = _rows;

    /// <summary>Rows to show.</summary>
    public IReadOnlyList<KeyValueEntry>? Entries
    {
        get => GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    /// <summary>Tab-separated key and copy text, one row per line. A row with nothing to copy writes only the key.</summary>
    public static string Format(IEnumerable<KeyValueEntry> entries)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        var builder = new StringBuilder();
        foreach (var entry in entries) {
            ArgumentHelpers.ThrowIfNull(entry);
            builder.Append(entry.Key);
            if (entry.ClipboardText is { } value)
                builder.Append('\t').Append(value);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EntriesProperty)
            Rebuild();
    }

    private void Rebuild()
    {
        _rows.Children.Clear();
        if (Entries == null)
            return;

        foreach (var entry in Entries)
            _rows.Children.Add(Row(entry));
    }

    private Control Row(KeyValueEntry entry)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var body = new StackPanel { Spacing = 2 };
        if (!string.IsNullOrWhiteSpace(entry.Key)) {
            body.Children.Add(Selectable(entry.Key, 11, Dim, mono: false));
        }

        body.Children.Add(Value(entry));
        grid.Children.Add(body);
        if (entry.ClipboardText is { } copy) {
            var button = new Button {
                Content = "Copy",
                Padding = new Thickness(8, 0),
                Height = 22,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top
            };
            button.Click += async (_, _) => await CopyAsync(copy);
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);
        }

        return new Border {
            Padding = new Thickness(0, 6),
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = Line,
            Child = grid
        };
    }

    private static Control Value(KeyValueEntry entry)
    {
        if (entry.View != null)
            return entry.View;
        if (entry.Image != null) {
            return new Image {
                Source = entry.Image,
                MaxHeight = 180,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left
            };
        }

        return Selectable(string.IsNullOrWhiteSpace(entry.Text) ? "—" : entry.Text, 12, null, mono: true);
    }

    private static SelectableTextBlock Selectable(string text, double size, IBrush? foreground, bool mono)
        => new() {
            Text = text,
            FontSize = size,
            FontFamily = mono ? Mono : FontFamily.Default,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

    private async Task CopyAsync(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
            return;
        var transfer = new DataTransfer();
        var item = new DataTransferItem();
        item.SetText(text);
        transfer.Add(item);
        await clipboard.SetDataAsync(transfer);
    }
}
