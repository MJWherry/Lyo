using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Lyo.Desktop.Components;

/// <summary>Screen point from an edge-dock drag, in pixels.</summary>
public readonly record struct EdgeDockPoint(int X, int Y);

/// <summary>One quick action shown when an <see cref="EdgeDockBar" /> opens.</summary>
public sealed class EdgeDockButton
{
    /// <summary>Label on the button.</summary>
    public required string Text { get; init; }

    /// <summary>Runs when the button is clicked. The sender is the button control, so a menu can open under it.</summary>
    public EventHandler? Pressed { get; init; }
}

/// <summary>
/// Screen-edge tab a consumer fills with an icon and any number of buttons.
/// Hover can open those buttons inward. Click, double-click, and drag are separate events, so each app decides what they do.
/// Thickness, length, and corner radius are the shape. The host window still places itself from <see cref="ScreenEdgeDock" />.
/// </summary>
public class EdgeDockBar : Border
{
    /// <summary>Which screen edge the flat side sits on.</summary>
    public static readonly StyledProperty<ScreenEdge> EdgeProperty = AvaloniaProperty.Register<EdgeDockBar, ScreenEdge>(nameof(Edge), ScreenEdge.Right);

    /// <summary>Thickness of the collapsed tab, in device-independent pixels.</summary>
    public static readonly StyledProperty<double> ThicknessProperty = AvaloniaProperty.Register<EdgeDockBar, double>(nameof(Thickness), ScreenEdgeDock.Thickness);

    /// <summary>Length of the collapsed tab, in device-independent pixels.</summary>
    public static readonly StyledProperty<double> LengthProperty = AvaloniaProperty.Register<EdgeDockBar, double>(nameof(Length), ScreenEdgeDock.Diameter);

    /// <summary>Outer corner radius. The side on the screen edge stays square.</summary>
    public static readonly StyledProperty<double> CornerProperty = AvaloniaProperty.Register<EdgeDockBar, double>(nameof(Corner), ScreenEdgeDock.Thickness);

    /// <summary>Icon or mark drawn on the collapsed tab.</summary>
    public static readonly StyledProperty<Control?> IconProperty = AvaloniaProperty.Register<EdgeDockBar, Control?>(nameof(Icon));

    /// <summary>True while the quick actions are open.</summary>
    public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<EdgeDockBar, bool>(nameof(Expanded));

    /// <summary>Hover opens <see cref="Buttons" />. A consumer that only wants click and drag leaves this false.</summary>
    public static readonly StyledProperty<bool> ExpandOnHoverProperty = AvaloniaProperty.Register<EdgeDockBar, bool>(nameof(ExpandOnHover));

    /// <summary>Shifts the icon along the bubble, in device-independent pixels. Positive moves toward the right or the bottom.</summary>
    public static readonly StyledProperty<double> IconSlideProperty = AvaloniaProperty.Register<EdgeDockBar, double>(nameof(IconSlide));

    /// <summary>Shifts the collapsed face along the edge, in device-independent pixels, so the icon stays put when the open bar slides back onto the screen.</summary>
    public static readonly StyledProperty<double> FaceOffsetProperty = AvaloniaProperty.Register<EdgeDockBar, double>(nameof(FaceOffset));

    private readonly ObservableCollection<EdgeDockButton> _buttons = [];
    private readonly EdgeDockGesture _gesture = new();
    private readonly DispatcherTimer _expand;
    private readonly DispatcherTimer _collapse;
    private StackPanel? _actions;
    private Border? _face;
    private bool _building;
    private long _leaveAfter;
    private bool _left;
    private int _hoverX;
    private int _hoverY;
    private bool _hoverKnown;

    public EdgeDockBar()
    {
        ClipToBounds = true;
        Background = new SolidColorBrush(Color.Parse("#202124"));
        _buttons.CollectionChanged += OnButtonsChanged;
        _expand = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _expand.Tick += (_, _) => {
            _expand.Stop();
            if (ExpandOnHover)
                Expanded = true;
        };
        _collapse = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _collapse.Tick += (_, _) => {
            if (!Expanded) {
                _collapse.Stop();
                return;
            }

            if (!ShouldClose(HoldOpen, IsPointerOver && !_left, PointerStillInside(), Environment.TickCount64, _leaveAfter))
                return;
            _collapse.Stop();
            Expanded = false;
        };
        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel);
        PointerEntered += (_, e) => {
            _left = false;
            Remember(e);
            ArmExpand();
        };
        PointerMoved += (_, e) => {
            _left = false;
            Remember(e);
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                ArmExpand();
        };
        PointerExited += (_, _) => {
            _left = true;
            _expand.Stop();
            if (Expanded && !_collapse.IsEnabled)
                _collapse.Start();
        };
        Rebuild();
    }

    /// <summary>Which screen edge the flat side sits on.</summary>
    public ScreenEdge Edge
    {
        get => GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    /// <summary>Thickness of the collapsed tab, in device-independent pixels.</summary>
    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>Length of the collapsed tab, in device-independent pixels.</summary>
    public double Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    /// <summary>Outer corner radius. The side on the screen edge stays square.</summary>
    public double Corner
    {
        get => GetValue(CornerProperty);
        set => SetValue(CornerProperty, value);
    }

    /// <summary>Icon or mark drawn on the collapsed tab.</summary>
    public Control? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>True while the quick actions are open.</summary>
    public bool Expanded
    {
        get => GetValue(ExpandedProperty);
        set => SetValue(ExpandedProperty, value);
    }

    /// <summary>Hover opens <see cref="Buttons" />. Moving the pointer off the bar closes them again.</summary>
    public bool ExpandOnHover
    {
        get => GetValue(ExpandOnHoverProperty);
        set => SetValue(ExpandOnHoverProperty, value);
    }

    /// <summary>Shifts the icon along the bubble, in device-independent pixels. Positive moves toward the right or the bottom. <see cref="ScreenEdgeDock.IconBias" /> picks the value.</summary>
    public double IconSlide
    {
        get => GetValue(IconSlideProperty);
        set => SetValue(IconSlideProperty, value);
    }

    /// <summary>Shifts the collapsed face along the edge, in device-independent pixels. Keeps the icon where it was when an open bar has to slide to stay on the screen.</summary>
    public double FaceOffset
    {
        get => GetValue(FaceOffsetProperty);
        set => SetValue(FaceOffsetProperty, value);
    }

    /// <summary>Quick actions. The host can add as many as it wants. They show while <see cref="Expanded" /> is true.</summary>
    public IList<EdgeDockButton> Buttons => _buttons;

    /// <summary>Keeps the drawer open while a menu from one of the buttons is up.</summary>
    public bool HoldOpen { get; set; }

    /// <summary>The pointer went down and up on the tab, not on a button.</summary>
    public event EventHandler? Click;

    /// <summary>A second click landed on the first.</summary>
    public event EventHandler? DoubleClick;

    /// <summary>The pointer left the slop box. The point is in screen pixels. The host decides whether that pull leaves the edge.</summary>
    public event EventHandler<EdgeDockPoint>? Dragged;

    /// <summary>A drag ended without the host cancelling it.</summary>
    public event EventHandler? DragFinished;

    /// <summary>Size or edge changed. The host should place the window again.</summary>
    public event EventHandler? LayoutChanged;

    /// <summary>Closes the drawer once a menu that was holding it open has gone, unless the pointer is still over the tab.</summary>
    public void CollapseIfIdle()
    {
        if (!HoldOpen && !IsPointerOver)
            _collapse.Start();
    }

    /// <summary>Drops the current press so a later release is not a click.</summary>
    public void CancelPointer()
    {
        _gesture.Cancel();
        _expand.Stop();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_building)
            return;
        if (change.Property == IconSlideProperty || change.Property == FaceOffsetProperty) {
            ApplyIcon();
            return;
        }

        if (change.Property == ExpandedProperty) {
            ShowActions();
            if (Expanded) {
                _leaveAfter = Environment.TickCount64 + 500;
                ToolTip.SetIsOpen(this, false);
                if (!_collapse.IsEnabled)
                    _collapse.Start();
            }
            else
                _collapse.Stop();

            InvalidateMeasure();
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (change.Property == EdgeProperty || change.Property == ThicknessProperty || change.Property == LengthProperty || change.Property == CornerProperty
            || change.Property == IconProperty)
            Rebuild();
    }

    private void OnButtonsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        _building = true;
        try {
            Detach(Icon);
            var vertical = Edge is ScreenEdge.Left or ScreenEdge.Right;
            var face = new Border {
                Width = vertical ? Thickness : Length,
                Height = vertical ? Length : Thickness,
                Background = Brushes.Transparent,
                Child = Icon,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            _face = face;
            if (Icon != null) {
                Icon.HorizontalAlignment = HorizontalAlignment.Center;
                Icon.VerticalAlignment = VerticalAlignment.Center;
            }

            var actions = new StackPanel {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(6, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            _actions = actions;
            foreach (var action in _buttons) {
                var button = new Button { Content = action.Text, Padding = new Thickness(8, 2), FontSize = 12 };
                button.Click += (sender, args) => {
                    args.Handled = true;
                    action.Pressed?.Invoke(sender, EventArgs.Empty);
                };
                actions.Children.Add(button);
            }

            var row = new StackPanel { Orientation = vertical ? Orientation.Horizontal : Orientation.Vertical };
            if (Edge is ScreenEdge.Right or ScreenEdge.Bottom) {
                row.Children.Add(actions);
                row.Children.Add(face);
            }
            else {
                row.Children.Add(face);
                row.Children.Add(actions);
            }

            var corners = ScreenEdgeDock.Corners(Edge, (int)Math.Round(Corner));
            CornerRadius = new CornerRadius(corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft);
            ShowActions();
            ApplyIcon();
            Child = row;
        }
        finally {
            _building = false;
        }

        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Remember(PointerEventArgs e)
    {
        var point = this.PointToScreen(e.GetPosition(this));
        _hoverX = point.X;
        _hoverY = point.Y;
        _hoverKnown = true;
    }

    // A tooltip or the resize that opens the drawer reports an exit while the cursor has not moved.
    private bool PointerStillInside()
    {
        if (IsPointerOver || HoldOpen)
            return true;
        if (!_hoverKnown || Bounds.Width <= 0 || Bounds.Height <= 0)
            return false;
        var origin = this.PointToScreen(default);
        var far = this.PointToScreen(new Point(Bounds.Width, Bounds.Height));
        var left = Math.Min(origin.X, far.X);
        var top = Math.Min(origin.Y, far.Y);
        var right = Math.Max(origin.X, far.X);
        var bottom = Math.Max(origin.Y, far.Y);
        return _hoverX >= left && _hoverX <= right && _hoverY >= top && _hoverY <= bottom;
    }

    // A click lands on the label or the button template, not on the Button itself.
    internal static bool FromButton(object? source)
    {
        var visual = source as Visual;
        while (visual != null) {
            if (visual is Button)
                return true;
            if (visual is EdgeDockBar)
                return false;
            visual = visual.GetVisualParent();
        }

        var node = source as StyledElement;
        while (node != null) {
            if (node is Button)
                return true;
            if (node is EdgeDockBar)
                return false;
            node = node.Parent ?? node.TemplatedParent as StyledElement;
        }

        return false;
    }

    // Opening resizes the window and reports a leave while the cursor has not moved. That sample is inside the bar.
    // A later real leave has the same inside sample, so it only holds the drawer during the short grace after open.
    internal static bool ShouldClose(bool holdOpen, bool pointerOver, bool sampleInside, long now, long leaveAfter)
    {
        if (holdOpen || pointerOver)
            return false;
        if (sampleInside && now < leaveAfter)
            return false;
        return true;
    }

    private void ApplyIcon()
    {
        if (_face != null) {
            var vertical = Edge is ScreenEdge.Left or ScreenEdge.Right;
            _face.Margin = vertical ? new Thickness(0, Math.Max(0, FaceOffset), 0, 0) : new Thickness(Math.Max(0, FaceOffset), 0, 0, 0);
        }

        if (Icon == null)
            return;
        var side = Edge is ScreenEdge.Left or ScreenEdge.Right;
        Icon.RenderTransform = new TranslateTransform(side ? 0 : IconSlide, side ? IconSlide : 0);
    }

    private void ArmExpand()
    {
        if (ExpandOnHover && !Expanded && !_expand.IsEnabled)
            _expand.Start();
    }

    private void ShowActions()
    {
        if (_actions == null)
            return;
        _actions.IsVisible = Expanded && _buttons.Count > 0;
        _actions.InvalidateMeasure();
        InvalidateMeasure();
    }

    private static void Detach(Control? control)
    {
        if (control?.Parent is Decorator decorator)
            decorator.Child = null;
        else if (control?.Parent is Panel panel)
            panel.Children.Remove(control);
        else if (control?.Parent is ContentControl content)
            content.Content = null;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (FromButton(e.Source))
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        var point = this.PointToScreen(e.GetPosition(this));
        _gesture.Press(point.X, point.Y);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        var point = this.PointToScreen(e.GetPosition(this));
        if (!_gesture.Move(point.X, point.Y))
            return;
        _expand.Stop();
        if (Expanded)
            Expanded = false;
        Dragged?.Invoke(this, new EdgeDockPoint(point.X, point.Y));
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        var point = this.PointToScreen(e.GetPosition(this));
        var tap = _gesture.Release(point.X, point.Y, Environment.TickCount);
        if (e.Pointer.Captured == this)
            e.Pointer.Capture(null);
        if (tap == EdgeDockTap.Drag)
            DragFinished?.Invoke(this, EventArgs.Empty);
        else if (tap == EdgeDockTap.DoubleClick)
            DoubleClick?.Invoke(this, EventArgs.Empty);
        else if (tap == EdgeDockTap.Click)
            Click?.Invoke(this, EventArgs.Empty);
        e.Handled = tap != EdgeDockTap.None;
    }
}
