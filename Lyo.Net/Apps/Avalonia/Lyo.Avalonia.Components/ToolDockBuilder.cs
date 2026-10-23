using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Lyo.Exceptions;
using Path = Avalonia.Controls.Shapes.Path;

namespace Lyo.Desktop.Components;

/// <summary>Lays tool panes around a center control. Left and right stack vertically. The bottom stacks sideways.</summary>
public static class ToolDockBuilder
{
    private const double MinSide = 120;
    private const double MinBottom = 80;

    /// <summary>
    /// Fills <paramref name="host" /> from <paramref name="layout" />. <paramref name="changed" /> runs after a dock, hide, or splitter release. Header tags are the pane ids, so
    /// the host can start a drag from the title bar.
    /// </summary>
    public static void Fill(Grid host, ToolDockState layout, IReadOnlyDictionary<string, Control> panes, Control center, Action changed)
    {
        ArgumentHelpers.ThrowIfNull(host);
        ArgumentHelpers.ThrowIfNull(layout);
        ArgumentHelpers.ThrowIfNull(panes);
        ArgumentHelpers.ThrowIfNull(center);
        ArgumentHelpers.ThrowIfNull(changed);
        Detach(center);
        host.Children.Clear();
        host.ColumnDefinitions.Clear();
        host.RowDefinitions.Clear();
        var left = Visible(layout, ToolDockEdge.Left);
        var right = Visible(layout, ToolDockEdge.Right);
        var bottom = Visible(layout, ToolDockEdge.Bottom);
        var centerColumn = left.Count > 0 ? 2 : 0;
        EdgeSlot? leftSlot = null;
        if (left.Count > 0) {
            var column = new ColumnDefinition();
            host.ColumnDefinitions.Add(column);
            host.ColumnDefinitions.Add(new(GridLength.Auto));
            leftSlot = new() {
                Layout = layout,
                Edge = ToolDockEdge.Left,
                Min = MinSide,
                Column = column
            };
        }

        host.ColumnDefinitions.Add(new(GridLength.Star));
        var rightColumn = host.ColumnDefinitions.Count;
        EdgeSlot? rightSlot = null;
        if (right.Count > 0) {
            host.ColumnDefinitions.Add(new(GridLength.Auto));
            rightColumn = host.ColumnDefinitions.Count;
            var column = new ColumnDefinition();
            host.ColumnDefinitions.Add(column);
            rightSlot = new() {
                Layout = layout,
                Edge = ToolDockEdge.Right,
                Min = MinSide,
                Column = column
            };
        }

        host.RowDefinitions.Add(new(GridLength.Star));
        EdgeSlot? bottomSlot = null;
        if (bottom.Count > 0) {
            host.RowDefinitions.Add(new(GridLength.Auto));
            var row = new RowDefinition();
            host.RowDefinitions.Add(row);
            bottomSlot = new() {
                Layout = layout,
                Edge = ToolDockEdge.Bottom,
                Min = MinBottom,
                Row = row
            };
        }

        if (leftSlot != null) {
            Place(host, Stack(left, true, panes, layout, changed, leftSlot), 0, 0);
            var splitter = Splitter(
                false, () => {
                    layout.Left = host.ColumnDefinitions[0].ActualWidth;
                    changed();
                });

            leftSlot.Splitter = splitter;
            Place(host, splitter, 1, 0);
            leftSlot.Apply();
        }

        Place(host, center, centerColumn, 0);
        if (rightSlot != null) {
            var splitter = Splitter(
                false, () => {
                    layout.Right = host.ColumnDefinitions[rightColumn].ActualWidth;
                    changed();
                });

            rightSlot.Splitter = splitter;
            Place(host, splitter, rightColumn - 1, 0);
            Place(host, Stack(right, true, panes, layout, changed, rightSlot), rightColumn, 0);
            rightSlot.Apply();
        }

        if (bottomSlot != null) {
            var span = host.ColumnDefinitions.Count;
            var splitter = Splitter(
                true, () => {
                    layout.Bottom = host.RowDefinitions[^1].ActualHeight;
                    changed();
                });

            bottomSlot.Splitter = splitter;
            Grid.SetColumnSpan(splitter, span);
            Place(host, splitter, 0, 1);
            var body = Stack(bottom, false, panes, layout, changed, bottomSlot);
            Grid.SetColumnSpan(body, span);
            Place(host, body, 0, 2);
            bottomSlot.Apply();
        }
    }

    private static List<ToolDockPane> Visible(ToolDockState layout, ToolDockEdge edge) => layout.Panes.Where(pane => pane.Visible && pane.Edge == edge).ToList();

    private static Control Stack(List<ToolDockPane> panes, bool vertical, IReadOnlyDictionary<string, Control> bodies, ToolDockState layout, Action changed, EdgeSlot edge)
    {
        if (panes.Count == 1)
            return Chrome(panes[0], Body(panes[0], bodies), layout, changed, edge, null, null);

        var grid = new Grid();
        var slot = 0;
        for (var i = 0; i < panes.Count; i++) {
            RowDefinition? paneRow = null;
            ColumnDefinition? paneColumn = null;
            if (vertical) {
                paneRow = new(panes[i].Minimized ? GridLength.Auto : new(1, GridUnitType.Star));
                grid.RowDefinitions.Add(paneRow);
            }
            else {
                paneColumn = new(panes[i].Minimized ? GridLength.Auto : new(1, GridUnitType.Star));
                grid.ColumnDefinitions.Add(paneColumn);
            }

            var chrome = Chrome(panes[i], Body(panes[i], bodies), layout, changed, edge, paneRow, paneColumn);
            if (vertical)
                Grid.SetRow(chrome, slot);
            else
                Grid.SetColumn(chrome, slot);

            grid.Children.Add(chrome);
            slot++;
            if (i == panes.Count - 1)
                continue;

            if (vertical)
                grid.RowDefinitions.Add(new(GridLength.Auto));
            else
                grid.ColumnDefinitions.Add(new(GridLength.Auto));

            var splitter = Splitter(vertical, () => changed());
            if (vertical)
                Grid.SetRow(splitter, slot);
            else
                Grid.SetColumn(splitter, slot);

            grid.Children.Add(splitter);
            slot++;
        }

        return grid;
    }

    private static Control Body(ToolDockPane pane, IReadOnlyDictionary<string, Control> bodies)
    {
        if (!bodies.TryGetValue(pane.Id, out var body))
            throw new InvalidOperationException($"No control is registered for {pane.Id}.");

        Detach(body);
        return body;
    }

    private static Control Chrome(ToolDockPane pane, Control body, ToolDockState layout, Action changed, EdgeSlot edge, RowDefinition? paneRow, ColumnDefinition? paneColumn)
    {
        var move = new Cursor(StandardCursorType.SizeAll);
        var grip = new Border {
            Width = 22,
            Height = 22,
            Margin = new(0, 0, 6, 0),
            Cursor = move,
            Background = Brushes.Transparent,
            Child = new Path {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = Brushes.White,
                Data = Geometry.Parse("M5,1 H7 V5 H11 V7 H7 V11 H5 V7 H1 V5 H5 Z")
            }
        };

        ToolTip.SetTip(grip, "Drag to another edge");
        DockPanel.SetDock(grip, Dock.Left);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        DockPanel.SetDock(buttons, Dock.Right);
        body.IsVisible = !pane.Minimized;
        Button? minimize = null;
        minimize = HeaderButton(
            "—", pane.Minimized ? "Restore" : "Minimize", new(StandardCursorType.Arrow), () => {
                layout.ToggleMinimized(pane.Id);
                body.IsVisible = !pane.Minimized;
                ToolTip.SetTip(minimize!, pane.Minimized ? "Restore" : "Minimize");
                if (paneRow != null)
                    paneRow.Height = pane.Minimized ? GridLength.Auto : new(1, GridUnitType.Star);

                if (paneColumn != null)
                    paneColumn.Width = pane.Minimized ? GridLength.Auto : new(1, GridUnitType.Star);

                edge.Apply();
            });

        buttons.Children.Add(minimize);
        buttons.Children.Add(
            HeaderButton(
                "×", "Close", new(StandardCursorType.Arrow), () => {
                    layout.SetVisible(pane.Id, false);
                    changed();
                }));

        var title = new TextBlock { Text = pane.Title, VerticalAlignment = VerticalAlignment.Center };
        var header = new Border {
            Tag = pane.Id,
            Cursor = move,
            Padding = new(4, 2),
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x31, 0x40)),
            BorderThickness = new(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x5C, 0x67, 0x7A)),
            Child = new DockPanel { Children = { buttons, grip, title } }
        };

        var panel = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);
        panel.Children.Add(body);
        return new Border {
            Margin = new(1),
            BorderThickness = new(1),
            CornerRadius = new(4),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x5C, 0x67, 0x7A)),
            Child = panel
        };
    }

    private static Button HeaderButton(string glyph, string tip, Cursor cursor, Action action)
    {
        var button = new Button {
            Content = glyph,
            Padding = new(6, 0),
            MinWidth = 22,
            Height = 22,
            FontSize = 14,
            Cursor = cursor,
            Background = Brushes.Transparent
        };

        ToolTip.SetTip(button, tip);
        button.Click += (_, _) => action();
        return button;
    }

    private static GridSplitter Splitter(bool vertical, Action released)
    {
        var splitter = new GridSplitter {
            ResizeDirection = vertical ? GridResizeDirection.Rows : GridResizeDirection.Columns, Width = vertical ? double.NaN : 4, Height = vertical ? 4 : double.NaN
        };

        splitter.PointerReleased += (_, _) => released();
        return splitter;
    }

    private static void Place(Grid host, Control child, int column, int row)
    {
        Grid.SetColumn(child, column);
        Grid.SetRow(child, row);
        host.Children.Add(child);
    }

    private static void Detach(Control control)
    {
        if (control.Parent is Panel panel)
            panel.Children.Remove(control);
        else if (control.Parent is Decorator decorator)
            decorator.Child = null;
        else if (control.Parent is ContentControl content)
            content.Content = null;
    }

    private sealed class EdgeSlot
    {
        public required ToolDockState Layout { get; init; }

        public required ToolDockEdge Edge { get; init; }

        public required double Min { get; init; }

        public RowDefinition? Row { get; init; }

        public ColumnDefinition? Column { get; init; }

        public GridSplitter? Splitter { get; set; }

        public void Apply()
        {
            var size = Edge switch {
                ToolDockEdge.Left => Layout.Left,
                ToolDockEdge.Right => Layout.Right,
                var _ => Layout.Bottom
            };

            var hug = Layout.HugsHeader(Edge);
            var length = hug ? GridLength.Auto : new(Math.Max(Min, size), GridUnitType.Pixel);
            if (Row != null)
                Row.Height = length;

            if (Column != null)
                Column.Width = length;

            if (Splitter != null)
                Splitter.IsVisible = !hug;
        }
    }
}