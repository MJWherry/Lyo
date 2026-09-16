using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Lyo.Screenshot;

namespace Lyo.Screenshot.App;

public partial class RegionOverlay : Window
{
    private PixelPoint? _anchor;
    private PixelPoint _current;

    public RegionOverlay() => InitializeComponent();

    public CaptureRegion? Result { get; private set; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var screens = Screens?.All;
        if (screens is null || screens.Count == 0)
            return;
        var left = screens.Min(screen => screen.Bounds.X);
        var top = screens.Min(screen => screen.Bounds.Y);
        var right = screens.Max(screen => screen.Bounds.Right);
        var bottom = screens.Max(screen => screen.Bounds.Bottom);
        Position = new(left, top);
        var scale = RenderScaling <= 0 ? 1 : RenderScaling;
        Width = (right - left) / scale;
        Height = (bottom - top) / scale;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _anchor = this.PointToScreen(e.GetPosition(this));
        _current = _anchor.Value;
        UpdateSelection();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_anchor is null)
            return;
        _current = this.PointToScreen(e.GetPosition(this));
        UpdateSelection();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_anchor is null)
            return;
        _current = this.PointToScreen(e.GetPosition(this));
        var region = ToRegion();
        _anchor = null;
        if (region.Width < 4 || region.Height < 4) {
            Selection.IsVisible = false;
            return;
        }

        Result = region;
        Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Result = null;
        Close();
        e.Handled = true;
    }

    private void UpdateSelection()
    {
        if (_anchor is null)
            return;
        var region = ToRegion();
        var scale = RenderScaling <= 0 ? 1 : RenderScaling;
        Selection.IsVisible = true;
        Selection.Margin = new((region.X - Position.X) / scale, (region.Y - Position.Y) / scale, 0, 0);
        Selection.Width = region.Width / scale;
        Selection.Height = region.Height / scale;
    }

    private CaptureRegion ToRegion()
    {
        var anchor = _anchor ?? _current;
        var x = Math.Min(anchor.X, _current.X);
        var y = Math.Min(anchor.Y, _current.Y);
        return new(x, y, Math.Abs(_current.X - anchor.X), Math.Abs(_current.Y - anchor.Y));
    }
}
