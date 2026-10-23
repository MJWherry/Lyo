using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lyo.Desktop.Components;

/// <summary>
/// Read-only JSON document. A JSON body opens as a virtualized tree with search, expand, collapse, and per-node copy. Anything else stays as text. Network bodies and
/// captured payloads use it.
/// </summary>
public class JsonEditor : UserControl
{
    private static readonly FontFamily Mono = new("Cascadia Code, Cascadia Mono, Consolas, monospace");
    private static readonly IBrush Hit = new SolidColorBrush(Color.FromRgb(0x2A, 0x3F, 0x66));
    private static readonly IBrush StringBrush = new SolidColorBrush(Color.FromRgb(0x8F, 0xD1, 0x8F));
    private static readonly IBrush NumberBrush = new SolidColorBrush(Color.FromRgb(0x7A, 0xB8, 0xFF));
    private static readonly IBrush BooleanBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xB0, 0x60));
    private static readonly IBrush NullBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xB2));

    /// <summary>Document text. JSON opens in the tree. Other text opens as raw.</summary>
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<JsonEditor, string?>(nameof(Text));

    private readonly Button _collapse;
    private readonly TextBlock _count;
    private readonly Button _expand;
    private readonly ListBox _list;
    private readonly Button _next;
    private readonly Button _previous;

    private readonly TextBox _raw = new() {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = Mono,
        FontSize = 12,
        BorderThickness = new(0),
        Background = Brushes.Transparent
    };

    private readonly Button _rawButton;

    private readonly ObservableCollection<JsonTreeRow> _rows = [];
    private readonly TextBox _search;
    private readonly Button _treeButton;

    private JsonTreeDocument? _document;
    private bool _showingTree;

    /// <summary>Document text.</summary>
    public string? Text {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public JsonEditor()
    {
        _search = new() { PlaceholderText = "Search", MinWidth = 140, VerticalAlignment = VerticalAlignment.Center };
        _search.TextChanged += (_, _) => {
            _document?.Search(_search.Text);
            ShowRows();
        };

        _count = new() { Text = "0/0", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 };
        _previous = Tool("Prev", () => Step(false));
        _next = Tool("Next", () => Step(true));
        _expand = Tool(
            "Expand", () => {
                _document?.ExpandAll();
                ShowRows();
            });

        _collapse = Tool(
            "Collapse", () => {
                _document?.CollapseAll();
                ShowRows();
            });

        _treeButton = new() { Content = "Tree", Padding = new(8, 2) };
        _rawButton = new() { Content = "Raw", Padding = new(8, 2) };
        _treeButton.Click += (_, _) => Show(true);
        _rawButton.Click += (_, _) => Show(false);
        _list = CreateList();
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new(0, 0, 0, 6) };
        bar.Children.Add(_search);
        bar.Children.Add(_count);
        bar.Children.Add(_previous);
        bar.Children.Add(_next);
        bar.Children.Add(_expand);
        bar.Children.Add(_collapse);
        bar.Children.Add(_treeButton);
        bar.Children.Add(_rawButton);
        var body = new Grid();
        body.Children.Add(_list);
        body.Children.Add(_raw);
        var root = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);
        root.Children.Add(body);
        Content = root;
        Show(false);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
            Apply();
    }

    private ListBox CreateList()
    {
        var list = new ListBox {
            ItemsSource = _rows,
            Background = Brushes.Transparent,
            BorderThickness = new(0),
            ItemTemplate = new FuncDataTemplate<JsonTreeRow>((row, _) => row == null ? new Panel() : Row(row)),
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel())
        };

        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        list.ContainerPrepared += (_, e) => {
            if (e.Container is not ListBoxItem item)
                return;

            item.Padding = new(0);
            item.MinHeight = 28;
            item.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        };

        return list;
    }

    private void Apply()
    {
        var text = Text ?? "";
        _raw.Text = text;
        _document = JsonTreeDocument.TryParse(text, out var document) ? document : null;
        var json = _document != null;
        _treeButton.IsEnabled = json;
        _expand.IsEnabled = json;
        _collapse.IsEnabled = json;
        if (json && !string.IsNullOrWhiteSpace(_search.Text))
            _document!.Search(_search.Text);

        ShowRows();
        Show(json);
    }

    private void Step(bool next)
    {
        if (_document == null)
            return;

        if (next)
            _document.NextMatch();
        else
            _document.PreviousMatch();

        ShowRows();
        var hit = _rows.FirstOrDefault(row => row.SearchHit);
        if (hit != null)
            _list.ScrollIntoView(hit);
    }

    private void ShowRows()
    {
        _rows.Clear();
        if (_document == null) {
            _count.Text = "0/0";
            _previous.IsEnabled = false;
            _next.IsEnabled = false;
            return;
        }

        foreach (var row in _document.VisibleRows)
            _rows.Add(row);

        _count.Text = _document.SearchCurrent + "/" + _document.SearchTotal;
        var any = _document.SearchTotal > 0;
        _previous.IsEnabled = any;
        _next.IsEnabled = any;
    }

    private void Show(bool tree)
    {
        _showingTree = tree && _treeButton.IsEnabled;
        _list.IsVisible = _showingTree;
        _raw.IsVisible = !_showingTree;
    }

    private Control Row(JsonTreeRow row)
    {
        var grid = new Grid {
            Height = 28,
            ColumnSpacing = 6,
            Margin = new(4 + row.Depth * 16, 0, 4, 0),
            Background = row.SearchHit ? Hit : Brushes.Transparent
        };

        grid.ColumnDefinitions.Add(new(GridLength.Auto));
        grid.ColumnDefinitions.Add(new(GridLength.Auto));
        grid.ColumnDefinitions.Add(new(GridLength.Star));
        grid.ColumnDefinitions.Add(new(GridLength.Auto));
        var toggle = new Button {
            Content = !row.Expandable ? "" : row.Expanded ? "▾" : "▸",
            Width = 22,
            Height = 22,
            Padding = new(0),
            IsEnabled = row.Expandable,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (row.Expandable) {
            var path = row.Path;
            toggle.Click += (_, _) => {
                _document?.Toggle(path);
                ShowRows();
            };
        }

        Grid.SetColumn(toggle, 0);
        grid.Children.Add(toggle);
        var key = new TextBlock {
            Text = string.IsNullOrEmpty(row.Key) ? "" : row.Key + ":",
            FontFamily = Mono,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(key, 1);
        grid.Children.Add(key);
        var value = new TextBlock {
            Text = row.Value,
            FontFamily = Mono,
            FontSize = 12,
            Foreground = Brush(row.Kind),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        Grid.SetColumn(value, 2);
        grid.Children.Add(value);
        var copy = new Button {
            Content = "Copy",
            Padding = new(6, 0),
            Height = 22,
            VerticalAlignment = VerticalAlignment.Center
        };

        var text = row.CopyText;
        copy.Click += (_, _) => _ = CopyAsync(text);
        Grid.SetColumn(copy, 3);
        grid.Children.Add(copy);
        return grid;
    }

    private static IBrush Brush(JsonTreeKind kind)
        => kind switch {
            JsonTreeKind.String => StringBrush,
            JsonTreeKind.Number => NumberBrush,
            JsonTreeKind.Boolean => BooleanBrush,
            JsonTreeKind.Null => NullBrush,
            var _ => Brushes.White
        };

    private static Button Tool(string text, Action click)
    {
        var button = new Button { Content = text, Padding = new(8, 2) };
        button.Click += (_, _) => click();
        return button;
    }

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