using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using Path = Avalonia.Controls.Shapes.Path;

namespace Lyo.Desktop.Components;

/// <summary>
/// Virtualized log. Rows show a timestamp, a level chip, an optional source, and the message. The toolbar filters by minimum level and text, follows the tail, and copies or
/// exports the visible lines. The eye at the left of a row opens <see cref="LogLineView" /> for that line. Lines come from a <see cref="LogSink" />.
/// </summary>
public class LogView : UserControl
{
    private static readonly LogLevel[] Levels = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical];
    private static readonly FontFamily Mono = new("Consolas, Menlo, DejaVu Sans Mono");
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xB2));

    private readonly Dictionary<LogLevel, Chip> _chips = [];
    private readonly Button _copy;
    private readonly MenuItem _copyAllItem;
    private readonly MenuItem _copyItem;
    private readonly StackPanel _empty;
    private readonly TextBlock _emptyHint;
    private readonly Button _export;
    private readonly MenuItem _exportItem;
    private readonly CheckBox _followBox;
    private readonly List<LogLine> _lines = [];
    private readonly ListBox _list;
    private readonly TextBox _search;
    private readonly ObservableCollection<LogLine> _visible = [];
    private bool _applyingFollow;
    private Window? _detail;
    private LogLineView? _detailView;
    private bool _follow = true;
    private LogLevel _minLevel = LogLevel.Trace;
    private string? _query;
    private ScrollViewer? _scroll;
    private LogSink? _sink;

    /// <summary>Source of lines. Replacing it stops listening to the previous sink and shows the new buffer. Set this on the UI thread.</summary>
    public LogSink? Sink {
        get => _sink;
        set => ApplySink(value);
    }

    public LogView()
    {
        _copyItem = CommandItem("Copy", () => CopyAsync(LinesToCopy()));
        _copyAllItem = CommandItem("Copy all", () => CopyAsync(_visible.ToArray()));
        _exportItem = CommandItem("Export", ExportAsync);
        _list = CreateList();
        _search = new() { PlaceholderText = "Search", MinWidth = 120, VerticalAlignment = VerticalAlignment.Center };
        _search.TextChanged += (_, _) => {
            _query = _search.Text;
            Rebuild();
        };

        _followBox = new() { Content = "Follow", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
        _followBox.IsCheckedChanged += OnFollowChanged;
        _copy = Tool("Copy", () => CopyAsync(LinesToCopy()));
        _export = Tool("Export", ExportAsync);
        var clear = Tool("Clear", null);
        clear.Click += (_, _) => _sink?.Clear();
        var root = new DockPanel();
        var bar = Toolbar(clear);
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);
        _emptyHint = new() {
            HorizontalAlignment = HorizontalAlignment.Center,
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };

        _empty = EmptyState();
        var body = new Grid();
        body.Children.Add(_list);
        body.Children.Add(_empty);
        root.Children.Add(body);
        Content = root;
        PaintChips();
        UpdateChrome();
    }

    private ListBox CreateList()
    {
        var list = new ListBox {
            ItemsSource = _visible,
            SelectionMode = SelectionMode.Multiple,
            Background = Brushes.Transparent,
            BorderThickness = new(0),
            ItemTemplate = new FuncDataTemplate<LogLine>((line, _) => line == null ? new Panel() : Row(line)),
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

        list.TemplateApplied += (_, _) => AttachScroll(list);
        list.AddHandler(KeyDownEvent, OnListKey, RoutingStrategies.Tunnel);
        var menu = new ContextMenu();
        menu.Items.Add(_copyItem);
        menu.Items.Add(_copyAllItem);
        menu.Items.Add(_exportItem);
        var clear = new MenuItem { Header = "Clear" };
        clear.Click += (_, _) => _sink?.Clear();
        menu.Items.Add(clear);
        list.ContextMenu = menu;
        return list;
    }

    private Control Toolbar(Button clear)
    {
        var bar = new Grid { Margin = new(4), ColumnSpacing = 8 };
        bar.ColumnDefinitions.Add(new(GridLength.Auto));
        bar.ColumnDefinitions.Add(new(GridLength.Star));
        bar.ColumnDefinitions.Add(new(GridLength.Auto));
        bar.Children.Add(LevelFilters());
        Grid.SetColumn(_search, 1);
        bar.Children.Add(_search);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(_followBox);
        actions.Children.Add(_copy);
        actions.Children.Add(_export);
        actions.Children.Add(clear);
        Grid.SetColumn(actions, 2);
        bar.Children.Add(actions);
        return bar;
    }

    private StackPanel LevelFilters()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        foreach (var level in Levels) {
            var spec = LogLine.LevelChip(level);
            var chip = new Chip { Text = spec.Label, Cursor = new(StandardCursorType.Hand) };
            var chosen = level;
            chip.PointerPressed += (_, e) => {
                if (!e.GetCurrentPoint(chip).Properties.IsLeftButtonPressed)
                    return;

                SetLevel(chosen);
                e.Handled = true;
            };

            _chips[level] = chip;
            panel.Children.Add(chip);
        }

        return panel;
    }

    private StackPanel EmptyState()
    {
        var title = new TextBlock { Text = "No log lines", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14 };
        var panel = new StackPanel {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(12),
            IsHitTestVisible = false
        };

        panel.Children.Add(title);
        panel.Children.Add(_emptyHint);
        return panel;
    }

    private void ApplySink(LogSink? value)
    {
        if (ReferenceEquals(_sink, value))
            return;

        if (_sink != null) {
            _sink.Written -= OnWritten;
            _sink.Cleared -= OnCleared;
        }

        _sink = value;
        _lines.Clear();
        if (_sink != null) {
            _lines.AddRange(_sink.Snapshot());
            _sink.Written += OnWritten;
            _sink.Cleared += OnCleared;
        }

        Rebuild();
    }

    private void OnWritten(LogLine line)
    {
        if (Dispatcher.UIThread.CheckAccess())
            AddLine(line);
        else
            Dispatcher.UIThread.Post(() => AddLine(line));
    }

    private void OnCleared()
    {
        if (Dispatcher.UIThread.CheckAccess())
            Reset();
        else
            Dispatcher.UIThread.Post(Reset);
    }

    private void AddLine(LogLine line)
    {
        _lines.Add(line);
        var capacity = _sink?.Capacity ?? LogSink.DefaultCapacity;
        while (_lines.Count > capacity) {
            var dropped = _lines[0];
            _lines.RemoveAt(0);
            if (_visible.Count > 0 && ReferenceEquals(_visible[0], dropped))
                _visible.RemoveAt(0);
        }

        if (LogLine.Matches(line, _minLevel, _query))
            _visible.Add(line);

        UpdateChrome();
        FollowTail();
    }

    private void Reset()
    {
        _lines.Clear();
        _visible.Clear();
        UpdateChrome();
    }

    private void SetLevel(LogLevel level)
    {
        _minLevel = level;
        PaintChips();
        Rebuild();
    }

    private void Rebuild()
    {
        _visible.Clear();
        foreach (var line in _lines) {
            if (LogLine.Matches(line, _minLevel, _query))
                _visible.Add(line);
        }

        UpdateChrome();
        FollowTail();
    }

    private void PaintChips()
    {
        foreach (var (level, chip) in _chips) {
            var selected = level == _minLevel;
            chip.Hue = selected ? LogLine.LevelChip(level).Hue : -1;
            chip.Opacity = selected ? 1 : 0.55;
        }
    }

    private void UpdateChrome()
    {
        var has = _visible.Count > 0;
        _copy.IsEnabled = has;
        _export.IsEnabled = has;
        _copyItem.IsEnabled = has;
        _copyAllItem.IsEnabled = has;
        _exportItem.IsEnabled = has;
        _empty.IsVisible = !has;
        _emptyHint.Text = _lines.Count == 0 ? "Lines will appear here as they arrive." : "Nothing matches the current level and search.";
    }

    private void OnFollowChanged(object? sender, RoutedEventArgs e)
    {
        var on = _followBox.IsChecked == true;
        if (on == _follow)
            return;

        _follow = on;
        if (on)
            FollowTail();
    }

    private void FollowTail()
    {
        if (!_follow || _visible.Count == 0)
            return;

        var last = _visible[^1];
        _applyingFollow = true;
        Dispatcher.UIThread.Post(
            () => {
                _list.ScrollIntoView(last);
                Dispatcher.UIThread.Post(() => _applyingFollow = false, DispatcherPriority.Background);
            }, DispatcherPriority.Background);
    }

    private void AttachScroll(ListBox list)
    {
        var scroll = list.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll == null || ReferenceEquals(scroll, _scroll))
            return;

        if (_scroll != null)
            _scroll.ScrollChanged -= OnScrollChanged;

        _scroll = scroll;
        _scroll.ScrollChanged += OnScrollChanged;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_applyingFollow || !_follow || _scroll == null)
            return;

        if (e.OffsetDelta.Y == 0 || e.ExtentDelta.Y != 0)
            return;

        var max = Math.Max(0, _scroll.Extent.Height - _scroll.Viewport.Height);
        if (_scroll.Offset.Y >= max - 8)
            return;

        _follow = false;
        _followBox.IsChecked = false;
    }

    private void Open(LogLine line)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var created = false;
        if (_detail == null) {
            _detailView = new();
            _detail = new() {
                Width = 760,
                Height = 640,
                MinWidth = 480,
                MinHeight = 320,
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                Content = _detailView
            };

            PromptDialog.ApplyChrome(_detail);
            _detail.Closed += (_, _) => {
                _detail = null;
                _detailView = null;
            };

            created = true;
        }

        _detailView!.Line = line;
        _detail.Title = line.Level + " · " + Trim(line.Message, 80);
        if (created) {
            if (owner != null)
                _detail.Show(owner);
            else
                _detail.Show();
        }

        _detail.Activate();
    }

    private static string Trim(string text, int max)
    {
        var flat = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return flat.Length <= max ? flat : flat[..max] + "…";
    }

    private void OnListKey(object? sender, KeyEventArgs e)
    {
        if (Chord(e, Key.C)) {
            if (TextHasSelection())
                return;

            e.Handled = true;
            _ = CopyAsync(LinesToCopy());
            return;
        }

        if (!Chord(e, Key.A))
            return;

        e.Handled = true;
        _list.SelectAll();
    }

    private static bool Chord(KeyEventArgs e, Key key) => e.Key == key && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta));

    private bool TextHasSelection() => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is SelectableTextBlock text && !string.IsNullOrEmpty(text.SelectedText);

    private IReadOnlyList<LogLine> LinesToCopy()
    {
        if (_list.SelectedItems is not { Count: > 0 } selected)
            return _visible.ToArray();

        var chosen = selected.OfType<LogLine>().ToArray();
        return _visible.Where(line => chosen.Any(item => ReferenceEquals(item, line))).ToArray();
    }

    private async Task CopyAsync(IReadOnlyList<LogLine> lines)
    {
        if (lines.Count == 0)
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
            return;

        var transfer = new DataTransfer();
        var item = new DataTransferItem();
        item.SetText(LogLine.Format(lines));
        transfer.Add(item);
        await clipboard.SetDataAsync(transfer);
    }

    private async Task ExportAsync()
    {
        if (_visible.Count == 0)
            return;

        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null)
            return;

        var file = await storage.SaveFilePickerAsync(new() { Title = "Export log", SuggestedFileName = "log.txt", DefaultExtension = "txt" });
        if (file == null)
            return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(LogLine.Format(_visible));
    }

    private Control Row(LogLine? line)
    {
        if (line == null)
            return new Panel();

        var spec = LogLine.LevelChip(line.Level);
        var grid = new Grid {
            Height = 28,
            Margin = new(4, 0, 8, 0),
            ColumnSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var column = 0;
        Add(grid, Eye(line), column++);
        Add(grid, Stamp(line), column++);
        Add(grid, new Chip { Text = spec.Label, Hue = spec.Hue, VerticalAlignment = VerticalAlignment.Center }, column++);
        if (!string.IsNullOrWhiteSpace(line.Source))
            Add(grid, Source(line.Source!), column++);

        var message = new SelectableTextBlock {
            Text = line.Message,
            FontFamily = Mono,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        grid.ColumnDefinitions.Add(new(GridLength.Star));
        Grid.SetColumn(message, column);
        grid.Children.Add(message);
        return grid;
    }

    private Button Eye(LogLine line)
    {
        var button = new Button {
            Padding = new(2),
            Width = 22,
            Height = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new(StandardCursorType.Hand)
        };

        button.Content = EyeIcon(button);
        ToolTip.SetTip(button, "Show log line");
        button.Click += (_, e) => {
            Open(line);
            e.Handled = true;
        };

        return button;
    }

    private static Control EyeIcon(Button button)
    {
        var icon = new Grid {
            Width = 16,
            Height = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var outline = new Path {
            Data = Geometry.Parse("M1,8 C3.2,4.2 12.8,4.2 15,8 C12.8,11.8 3.2,11.8 1,8"),
            StrokeThickness = 1.4,
            StrokeLineCap = PenLineCap.Round,
            Stretch = Stretch.Uniform
        };

        outline.Bind(Shape.StrokeProperty, button.GetObservable(ForegroundProperty));
        var pupil = new Ellipse {
            Width = 4,
            Height = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        pupil.Bind(Shape.FillProperty, button.GetObservable(ForegroundProperty));
        icon.Children.Add(outline);
        icon.Children.Add(pupil);
        return icon;
    }

    private static void Add(Grid grid, Control child, int column)
    {
        grid.ColumnDefinitions.Add(new(GridLength.Auto));
        Grid.SetColumn(child, column);
        grid.Children.Add(child);
    }

    private static TextBlock Stamp(LogLine line)
        => new() {
            Text = line.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"),
            FontFamily = Mono,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Dim,
            MinWidth = 96
        };

    private static TextBlock Source(string source)
        => new() {
            Text = source,
            FontFamily = Mono,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Dim,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 220
        };

    private static Button Tool(string label, Func<Task>? action)
    {
        var button = new Button {
            Content = label,
            Padding = new(8, 0),
            Height = 26,
            FontSize = 12
        };

        if (action != null)
            button.Click += (_, _) => _ = action();

        return button;
    }

    private static MenuItem CommandItem(string header, Func<Task> action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => _ = action();
        return item;
    }
}