using Avalonia;
using Avalonia.Controls;

namespace Lyo.Desktop.Components;

/// <summary>One text row for <see cref="NameValueList" />. Prefer <see cref="KeyValueEntry" /> when the value is an image or a control.</summary>
public sealed class NameValueRow
{
    /// <summary>Left-hand label.</summary>
    public required string Name { get; init; }

    /// <summary>Right-hand text. Blank draws a dash.</summary>
    public string? Value { get; init; }
}

/// <summary>Text rows on a <see cref="KeyValueView" />. The text can be selected, and each row has a Copy button.</summary>
public class NameValueList : UserControl
{
    /// <summary>Rows to show. Null or empty draws nothing.</summary>
    public static readonly StyledProperty<IReadOnlyList<NameValueRow>?> RowsProperty = AvaloniaProperty.Register<NameValueList, IReadOnlyList<NameValueRow>?>(nameof(Rows));

    private readonly KeyValueView _view = new();

    /// <summary>Rows to show.</summary>
    public IReadOnlyList<NameValueRow>? Rows {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public NameValueList() => Content = _view;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RowsProperty)
            _view.Entries = Rows?.Select(row => KeyValueEntry.FromText(row.Name, row.Value)).ToArray();
    }
}