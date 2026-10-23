using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Lyo.Desktop.Components;

/// <summary>
/// Parks a window on a screen edge as an <see cref="EdgeDockBar" /> and puts the window back when the user pulls it off. The app still sets the icon, the buttons, and what a
/// click does.
/// </summary>
public sealed class EdgeDockWindow
{
    private readonly EdgeDockBar _bar;
    private readonly bool _canResizeWhenOpen;
    private readonly double? _minWidthWhenOpen;
    private readonly EdgeDockRest _rest;
    private readonly Control _shell;
    private readonly Window _window;
    private ScreenEdgeAnchor _collapsed;
    private bool _decorationRetried;
    private bool _fitting;
    private bool _offEdge;
    private bool _placeDecorations;
    private int _placeHeight;
    private PixelPoint _placePoint;
    private DispatcherTimer? _placeTimer;
    private int _placeWidth;
    private bool _previewing;
    private int _settleId;
    private int _settleTicks;
    private bool _settling;
    private bool _stopped;

    /// <summary>Saved edge and rest position. The host updates it while the user drags.</summary>
    public EdgeDockPlacement Placement { get; } = new();

    /// <summary>True while the window is the edge bubble.</summary>
    public bool Docked { get; private set; }

    /// <summary>True while the title bar is being asked for again. An <c>Opened</c> handler should return so it does not dock a second time.</summary>
    public bool Restoring { get; private set; }

    /// <summary>Attaches <paramref name="bar" /> to <paramref name="window" />. <paramref name="shell" /> hides while the bubble is showing.</summary>
    public EdgeDockWindow(Window window, EdgeDockBar bar, Control shell, EdgeDockRest rest, bool canResizeWhenOpen = true, double? minWidthWhenOpen = null)
    {
        _window = window;
        _bar = bar;
        _shell = shell;
        _rest = rest;
        _canResizeWhenOpen = canResizeWhenOpen;
        _minWidthWhenOpen = minWidthWhenOpen;
        _bar.Dragged += (_, point) => Drag(point);
        _bar.DragFinished += (_, _) => FinishDrag();
        _bar.LayoutChanged += (_, _) => {
            if (!Docked || _fitting || _previewing)
                return;

            ApplyShape(_collapsed, true);
        };
    }

    /// <summary>The docked flag or the rest position changed, including a drag that ended on the edge.</summary>
    public event EventHandler? DockedChanged;

    /// <summary>Collapses onto the saved edge, or the nearest edge of the current window when <paramref name="saved" /> is false.</summary>
    public void Show(bool saved)
    {
        if (Docked)
            return;

        var screen = Screen();
        if (screen is null)
            return;

        if (!saved) {
            Placement.RestX = _window.Position.X;
            Placement.RestY = _window.Position.Y;
        }

        ApplyChrome(saved ? AnchorSaved(screen) : AnchorFromWindow(screen));
        DockedChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Opens the window again at the rest position.</summary>
    public void Undock()
    {
        StopSettle();
        Docked = false;
        _offEdge = false;
        _previewing = false;
        Placement.Docked = false;
        _decorationRetried = false;
        _bar.Expanded = false;
        _bar.IsVisible = false;
        _bar.Opacity = 1;
        _shell.IsVisible = true;
        _window.TransparencyLevelHint = [WindowTransparencyLevel.None];
        _window.ClearValue(Window.BackgroundProperty);
        _window.Opacity = 1;
        _window.Cursor = Cursor.Default;
        _window.Topmost = false;
        ApplyOpenSize();
        RestoreFrame();
        DockedChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Places the bubble again after the window was hidden, without picking a new edge.</summary>
    public void Refit()
    {
        if (!Docked)
            return;

        ApplyChrome(_collapsed);
    }

    /// <summary>Stops the placement timer. Call this when the window is closing.</summary>
    public void Stop()
    {
        _stopped = true;
        StopSettle();
    }

    private void ApplyChrome(ScreenEdgeAnchor anchor)
    {
        Docked = true;
        Placement.Docked = true;
        _shell.IsVisible = false;
        _bar.IsVisible = true;
        _window.CanResize = true;
        _window.WindowDecorations = WindowDecorations.None;
        _window.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        _window.ClearValue(Layoutable.MinWidthProperty);
        _window.ClearValue(Layoutable.MaxWidthProperty);
        _window.ClearValue(Layoutable.MaxHeightProperty);
        _window.Background = Brushes.Transparent;
        _window.SizeToContent = SizeToContent.Manual;
        ApplyShape(anchor, true);
        _window.Topmost = true;
        _window.Opacity = 1;
    }

    private void ApplyShape(ScreenEdgeAnchor anchor, bool settle)
    {
        if (_fitting)
            return;

        _fitting = true;
        try {
            _collapsed = anchor;
            _bar.Edge = anchor.Edge;
            _bar.InvalidateMeasure();
            _bar.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
            var screen = Screen();
            var shown = anchor;
            var side = anchor.Edge is ScreenEdge.Left or ScreenEdge.Right;
            var dipWidth = side ? _bar.Thickness : _bar.Length;
            var dipHeight = side ? _bar.Length : _bar.Thickness;
            if (screen != null && _bar.DesiredSize.Width > 0 && _bar.DesiredSize.Height > 0) {
                dipWidth = _bar.DesiredSize.Width;
                dipHeight = _bar.DesiredSize.Height;
                var area = screen.WorkingArea;
                shown = ScreenEdgeDock.Fit(anchor, Scale(screen, (int)Math.Ceiling(dipWidth)), Scale(screen, (int)Math.Ceiling(dipHeight)));
                shown = ScreenEdgeDock.Clamp(shown, area.X, area.Y, area.Width, area.Height);
            }

            var iconScale = screen == null || screen.Scaling <= 0 ? 1d : screen.Scaling;
            var alongPixels = side ? anchor.Y - shown.Y : anchor.X - shown.X;
            _bar.FaceOffset = Math.Max(0, alongPixels / iconScale);
            _bar.IconSlide = IconSlideFor(anchor, screen);
            _window.Width = dipWidth;
            _window.Height = dipHeight;
            var point = new PixelPoint(shown.X, shown.Y);
            _window.Position = point;
            PlaceNative(point, shown.Width, shown.Height);
            if (settle)
                BeginSettle(point, shown.Width, shown.Height, false);

            Placement.Edge = anchor.Edge;
            Placement.X = anchor.X;
            Placement.Y = anchor.Y;
        }
        finally {
            _fitting = false;
        }
    }

    private double IconSlideFor(ScreenEdgeAnchor anchor, Screen? screen)
    {
        if (screen == null)
            return 0;

        var area = screen.WorkingArea;
        var side = anchor.Edge is ScreenEdge.Left or ScreenEdge.Right;
        var start = side ? anchor.Y - area.Y : anchor.X - area.X;
        var span = side ? anchor.Height : anchor.Width;
        var limit = side ? area.Height : area.Width;
        var scale = screen.Scaling <= 0 ? 1d : screen.Scaling;
        var max = (_bar.Length - _bar.Thickness) / 2d;
        return ScreenEdgeDock.IconBias(start, limit - start - span, _bar.Length * scale, max);
    }

    private void Drag(EdgeDockPoint point)
    {
        var screen = Screen();
        if (screen is null)
            return;

        var area = screen.WorkingArea;
        _offEdge = ScreenEdgeDock.AwayFromEdges(area.X, area.Y, area.Width, area.Height, point.X, point.Y, ScreenEdgeDock.OffEdge);
        if (_offEdge) {
            ShowPreview(point);
            return;
        }

        if (_previewing)
            EndPreview();

        ApplyShape(AnchorAt(screen, point.X, point.Y), true);
    }

    private void FinishDrag()
    {
        if (_offEdge) {
            _offEdge = false;
            _previewing = false;
            _window.Opacity = 1;
            _window.Cursor = Cursor.Default;
            _bar.Opacity = 1;
            Undock();
            return;
        }

        if (_previewing) {
            EndPreview();
            if (Docked)
                ApplyShape(_collapsed, true);
        }

        if (Docked)
            DockedChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowPreview(EdgeDockPoint point)
    {
        StopSettle();
        if (!_previewing) {
            _previewing = true;
            _bar.Expanded = false;
            _bar.Opacity = 0;
            _shell.IsVisible = true;
            _window.WindowDecorations = WindowDecorations.None;
            _window.TransparencyLevelHint = [WindowTransparencyLevel.None];
            _window.Background = new SolidColorBrush(Color.Parse("#16181D"));
            _window.Opacity = 0.92;
            _window.Cursor = new(StandardCursorType.SizeAll);
            ApplyOpenSize();
        }

        var origin = new PixelPoint(point.X - 32, point.Y - 18);
        Placement.RestX = origin.X;
        Placement.RestY = origin.Y;
        _window.Position = origin;
        var pixels = _rest.Pixels(Screen()?.Scaling ?? 1);
        PlaceNative(origin, pixels.Width, pixels.Height);
    }

    private void EndPreview()
    {
        _previewing = false;
        _window.Opacity = 1;
        _window.Cursor = Cursor.Default;
        _bar.Opacity = 1;
        _shell.IsVisible = false;
        _bar.IsVisible = true;
        _window.CanResize = true;
        _window.WindowDecorations = WindowDecorations.None;
        _window.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        _window.Background = Brushes.Transparent;
        _window.SizeToContent = SizeToContent.Manual;
    }

    private void ApplyOpenSize()
    {
        _window.CanResize = _canResizeWhenOpen;
        _window.SizeToContent = SizeToContent.Manual;
        if (_minWidthWhenOpen is double min)
            _window.MinWidth = min;
        else
            _window.ClearValue(Layoutable.MinWidthProperty);

        _window.ClearValue(Layoutable.MaxWidthProperty);
        _window.ClearValue(Layoutable.MaxHeightProperty);
        _window.Width = _rest.Width;
        if (_rest.HeightToContent) {
            _window.ClearValue(Layoutable.HeightProperty);
            _window.SizeToContent = SizeToContent.Height;
        }
        else
            _window.Height = _rest.Height;
    }

    private void RestoreFrame()
    {
        var point = new PixelPoint(Placement.RestX, Placement.RestY);
        Restoring = true;
        try {
            if (_window.IsVisible)
                _window.Hide();

            _window.WindowDecorations = WindowDecorations.None;
            _window.WindowDecorations = WindowDecorations.Full;
            _window.Show();
        }
        finally {
            Restoring = false;
        }

        _window.Position = point;
        var pixels = _rest.Pixels(Screen()?.Scaling ?? 1);
        PlaceNative(point, pixels.Width, pixels.Height);
        BeginSettle(point, pixels.Width, pixels.Height, true);
    }

    private void BeginSettle(PixelPoint point, int width, int height, bool decorations)
    {
        var id = ++_settleId;
        _settling = true;
        _settleTicks = 0;
        _placePoint = point;
        _placeWidth = width;
        _placeHeight = height;
        _placeDecorations = decorations;
        Dispatcher.UIThread.Post(() => RunSettle(id), DispatcherPriority.Background);
    }

    private void RunSettle(int id)
    {
        if (id != _settleId || _stopped)
            return;

        _settleTicks = 0;
        ApplySettledPlace();
        _placeTimer ??= new() { Interval = TimeSpan.FromMilliseconds(50) };
        _placeTimer.Tick -= OnPlaceTick;
        _placeTimer.Tick += OnPlaceTick;
        _placeTimer.Start();
    }

    private void OnPlaceTick(object? sender, EventArgs e)
    {
        if (!_settling || _stopped) {
            _placeTimer?.Stop();
            return;
        }

        ApplySettledPlace();
        if (++_settleTicks >= 8)
            _placeTimer?.Stop();
    }

    private void ApplySettledPlace()
    {
        if (_placeDecorations && !Docked)
            KeepRestoredSize();

        if (_placeDecorations && !Docked && !_decorationRetried && _settleTicks >= 4 && !TitlePresent()) {
            _decorationRetried = true;
            RestoreFrame();
            return;
        }

        PlaceNative(_placePoint, _placeWidth, _placeHeight);
        _window.Position = _placePoint;
    }

    private void KeepRestoredSize()
    {
        _window.Width = _rest.Width;
        if (_minWidthWhenOpen is double min)
            _window.MinWidth = min;

        if (_rest.HeightToContent) {
            if (_window.SizeToContent != SizeToContent.Height)
                _window.SizeToContent = SizeToContent.Height;

            _placeHeight = 0;
        }
        else {
            _window.Height = _rest.Height;
            var pixels = _rest.Pixels(Screen()?.Scaling ?? 1);
            _placeHeight = pixels.Height;
        }

        _placeWidth = _rest.Pixels(Screen()?.Scaling ?? 1).Width;
    }

    private void StopSettle()
    {
        _settleId++;
        _settling = false;
        _placeTimer?.Stop();
    }

    private bool TitlePresent()
    {
        var handle = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        return X11WindowPlace.HasTitleBar(handle);
    }

    private void PlaceNative(PixelPoint point, int width, int height)
    {
        if (!OperatingSystem.IsLinux())
            return;

        var handle = _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
            return;

        X11WindowPlace.TryPlace(handle, point.X, point.Y, width, height);
    }

    private ScreenEdgeAnchor AnchorFromWindow(Screen screen)
    {
        var area = screen.WorkingArea;
        var origin = _window.Position;
        var far = _window.PointToScreen(new(Math.Max(_window.Bounds.Width, 1), Math.Max(_window.Bounds.Height, 1)));
        var (thickness, diameter) = BubblePixels(screen);
        return ScreenEdgeDock.Place(area.X, area.Y, area.Width, area.Height, origin.X, origin.Y, Math.Max(1, far.X - origin.X), Math.Max(1, far.Y - origin.Y), thickness, diameter);
    }

    private ScreenEdgeAnchor AnchorSaved(Screen screen)
    {
        var area = screen.WorkingArea;
        var (thickness, diameter) = BubblePixels(screen);
        return ScreenEdgeDock.Along(Placement.Edge, area.X, area.Y, area.Width, area.Height, Placement.Along, thickness, diameter);
    }

    private ScreenEdgeAnchor AnchorAt(Screen screen, int x, int y)
    {
        var area = screen.WorkingArea;
        var (thickness, diameter) = BubblePixels(screen);
        return ScreenEdgeDock.Place(area.X, area.Y, area.Width, area.Height, x, y, 1, 1, thickness, diameter);
    }

    private (int Thickness, int Diameter) BubblePixels(Screen screen) => (Scale(screen, (int)Math.Round(_bar.Thickness)), Scale(screen, (int)Math.Round(_bar.Length)));

    private static int Scale(Screen screen, int dip)
    {
        var scale = screen.Scaling <= 0 ? 1d : screen.Scaling;
        return Math.Max(1, (int)Math.Round(dip * scale));
    }

    private Screen? Screen() => _window.Screens?.ScreenFromWindow(_window) ?? _window.Screens?.Primary;
}