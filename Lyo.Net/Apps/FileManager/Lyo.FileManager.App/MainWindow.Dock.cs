using Avalonia.Interactivity;
using Avalonia.Media;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Lyo.FileManager.App;

public partial class MainWindow
{
    private void ConfigureDock()
    {
        _edge.Placement.Apply(_bubble.Docked, _bubble.Edge, _bubble.X, _bubble.Y, _bubble.RestX, _bubble.RestY);
        DockBar.Icon = new ShapePath {
            Width = 18,
            Height = 16,
            Stretch = Stretch.Uniform,
            Fill = Brushes.White,
            Data = Geometry.Parse("M1,3 H7 L9,5 H17 V15 H1 Z")
        };

        DockBar.Background = new SolidColorBrush(Color.Parse("#2a3140"));
        DockBar.Click += (_, _) => _edge.Undock();
        _edge.DockedChanged += (_, _) => SaveSettings();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_edge.Restoring)
            return;

        if (_edge.Placement.Docked)
            _edge.Show(true);
    }

    private void OnDock(object? sender, RoutedEventArgs e)
    {
        if (_edge.Docked)
            return;

        _edge.Show(false);
    }

    private void CopyBubble()
    {
        var place = _edge.Placement.Values();
        _bubble.Docked = place.Docked;
        _bubble.Edge = place.Edge;
        _bubble.X = place.X;
        _bubble.Y = place.Y;
        _bubble.RestX = place.RestX;
        _bubble.RestY = place.RestY;
    }
}