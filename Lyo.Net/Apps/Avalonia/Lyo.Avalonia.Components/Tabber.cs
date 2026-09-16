using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Lyo.Exceptions;

namespace Lyo.Desktop.Components;

/// <summary>One page in a <see cref="Tabber" />. The tabber parents <see cref="Content" /> once and leaves it there.</summary>
public sealed class TabPage
{
    /// <summary>Label drawn on the tab.</summary>
    public required string Header { get; set; }

    /// <summary>Page body. Add this control to one tabber only.</summary>
    public required Control Content { get; init; }

    /// <summary>When true, the header shows a close button.</summary>
    public bool CanClose { get; init; }
}

/// <summary>
/// Horizontal tabs. Each page stays in the body; choosing a tab only shows that page.
/// Use this when a view switch must not move controls into a second parent.
/// </summary>
public class Tabber : DockPanel
{
    private readonly StackPanel _headers = new() { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new(8, 8, 8, 0) };
    private readonly Grid _body = new();
    private readonly List<TabPage> _pages = [];
    private readonly List<Border> _chrome = [];
    private readonly List<TextBlock> _labels = [];
    private int _selected = -1;

    public Tabber()
    {
        LastChildFill = true;
        DockPanel.SetDock(_headers, Dock.Top);
        Children.Add(_headers);
        Children.Add(_body);
    }

    /// <summary>Index of the visible page, or -1 when there are no pages.</summary>
    public int SelectedIndex => _selected;

    /// <summary>Pages in tab order.</summary>
    public IReadOnlyList<TabPage> Pages => _pages;

    /// <summary>Raised after the visible page changes, including when the last page closes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Raised with the index of a page that just closed, before the next page is selected.</summary>
    public event EventHandler<int>? TabClosed;

    /// <summary>Adds a page and parents its content. The first page becomes selected.</summary>
    public void Add(TabPage page)
    {
        ArgumentHelpers.ThrowIfNull(page);
        ArgumentHelpers.ThrowIfNull(page.Content);
        _pages.Add(page);
        page.Content.IsVisible = false;
        _body.Children.Add(page.Content);
        _headers.Children.Add(Header(page));
        if (_selected < 0)
            Select(0);
    }

    /// <summary>Shows the page at <paramref name="index" />. The other pages stay parented and hidden.</summary>
    public void Select(int index)
    {
        if (index < 0 || index >= _pages.Count || index == _selected)
            return;

        _selected = index;
        for (var i = 0; i < _pages.Count; i++) {
            _pages[i].Content.IsVisible = i == index;
            Paint(_chrome[i], i == index);
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the page at <paramref name="index" /> and selects the nearest remaining page.</summary>
    public void Close(int index)
    {
        if (index < 0 || index >= _pages.Count)
            return;

        var page = _pages[index];
        _pages.RemoveAt(index);
        _body.Children.Remove(page.Content);
        _headers.Children.RemoveAt(index);
        _chrome.RemoveAt(index);
        _labels.RemoveAt(index);
        var selected = _selected;
        if (selected > index)
            selected--;
        else if (selected == index)
            selected = _pages.Count == 0 ? -1 : Math.Min(index, _pages.Count - 1);

        _selected = -1;
        TabClosed?.Invoke(this, index);
        if (selected >= 0)
            Select(selected);
        else
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Header index under <paramref name="source" />, or -1 when <paramref name="source" /> is not a tab header.</summary>
    public int IndexAt(Avalonia.StyledElement? source)
    {
        var current = source;
        while (current != null && !ReferenceEquals(current, this)) {
            if (current is Border border) {
                var index = _chrome.IndexOf(border);
                if (index >= 0)
                    return index;
            }

            current = current.Parent;
        }

        return -1;
    }

    /// <summary>Replaces the label at <paramref name="index" />.</summary>
    public void Rename(int index, string header)
    {
        if (index < 0 || index >= _pages.Count)
            return;

        ArgumentHelpers.ThrowIfNull(header);
        _pages[index].Header = header;
        _labels[index].Text = header;
    }

    /// <summary>Enables or disables the header at <paramref name="index" />. A disabled tab does not accept clicks.</summary>
    public void SetEnabled(int index, bool enabled)
    {
        if (index < 0 || index >= _chrome.Count)
            return;

        _chrome[index].IsEnabled = enabled;
        _chrome[index].Opacity = enabled ? 1 : 0.4;
    }

    private Control Header(TabPage page)
    {
        var label = new TextBlock { Text = page.Header, VerticalAlignment = VerticalAlignment.Center, FontSize = 13, Margin = new(12, 8, 12, 6) };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(label);
        if (page.CanClose) {
            var close = new Button {
                Content = "×",
                Width = 22,
                Height = 22,
                Padding = new(0),
                Margin = new(0, 0, 6, 0),
                Background = Brushes.Transparent,
                BorderThickness = new(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            close.Click += (_, _) => Close(_pages.IndexOf(page));
            row.Children.Add(close);
        }

        var border = new Border { Child = row, CornerRadius = new(6, 6, 0, 0) };
        border.PointerPressed += (_, _) => Select(_pages.IndexOf(page));
        _labels.Add(label);
        _chrome.Add(border);
        Paint(border, false);
        return border;
    }

    private static void Paint(Border border, bool selected)
    {
        border.Background = selected ? new SolidColorBrush(Color.FromArgb(40, 0x3B, 0x82, 0xF6)) : Brushes.Transparent;
        border.BorderThickness = new(0, 0, 0, 2);
        border.BorderBrush = selected ? new SolidColorBrush(ChipHues.Fill(ChipHues.Blue)) : Brushes.Transparent;
    }
}
