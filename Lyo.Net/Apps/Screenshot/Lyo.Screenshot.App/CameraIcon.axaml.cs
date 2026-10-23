using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Lyo.Screenshot.App;

/// <summary>Camera mark used on the dock button and the screen-edge bubble.</summary>
public partial class CameraIcon : UserControl
{
    public CameraIcon()
    {
        InitializeComponent();
        Mark.Data = Geometry.Parse(CameraMark.Data);
        Mark.Fill = Foreground;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Mark.Fill = Foreground;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ForegroundProperty && Mark is not null)
            Mark.Fill = Foreground;
    }
}