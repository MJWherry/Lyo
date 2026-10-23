using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lyo.Desktop.Components;

/// <summary>Named HSL hues shared with the web chips. A negative hue is a neutral chip.</summary>
public static class ChipHues
{
    public const int Coral = 18;
    public const int Amber = 52;
    public const int Lime = 84;
    public const int Green = 152;
    public const int Teal = 174;
    public const int Cyan = 192;
    public const int Blue = 228;
    public const int Indigo = 252;
    public const int Violet = 274;
    public const int Purple = 292;
    public const int Magenta = 312;
    public const int Rose = 332;

    /// <summary>Named hues in wheel order, Coral through Rose.</summary>
    public static readonly int[] List = [
        Coral, Amber, Lime, Green, Teal, Cyan, Blue, Indigo, Violet, Purple,
        Magenta, Rose
    ];

    /// <summary>Fill for <paramref name="hue" /> at 72% saturation and 50% lightness. Negative hues are a neutral gray.</summary>
    public static Color Fill(int hue)
    {
        if (hue < 0)
            return Color.FromRgb(0x3A, 0x41, 0x50);

        var degrees = (hue % 360 + 360) % 360;
        const double saturation = 0.72;
        const double lightness = 0.50;
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var sector = degrees / 60d;
        var x = chroma * (1 - Math.Abs(sector % 2 - 1));
        var (r, g, b) = sector switch {
            < 1 => (chroma, x, 0d),
            < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma),
            var _ => (chroma, 0d, x)
        };

        var match = lightness - chroma / 2;
        return Color.FromRgb(Channel(r + match), Channel(g + match), Channel(b + match));
    }

    private static byte Channel(double unit) => (byte)Math.Clamp((int)Math.Round(unit * 255), 0, 255);
}

/// <summary>Label and hue for one chip. Hue below zero is neutral.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ChipSpec(string Label, int Hue);

/// <summary>Small filled label, the desktop match for a web chip.</summary>
public class Chip : Border
{
    /// <summary>Text inside the chip. Blank becomes an em dash.</summary>
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<Chip, string?>(nameof(Text));

    /// <summary>Hue in degrees. Below zero uses a neutral fill.</summary>
    public static readonly StyledProperty<int> HueProperty = AvaloniaProperty.Register<Chip, int>(nameof(Hue), -1);

    private readonly TextBlock _label;

    /// <summary>Text inside the chip.</summary>
    public string? Text {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Hue in degrees. Below zero uses a neutral fill.</summary>
    public int Hue {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public Chip()
    {
        CornerRadius = new(9);
        Padding = new(8, 0);
        Height = 20;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        _label = new() { FontSize = 11, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Child = _label;
        Apply();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == HueProperty)
            Apply();
    }

    private void Apply()
    {
        _label.Text = string.IsNullOrWhiteSpace(Text) ? "—" : Text;
        Background = new SolidColorBrush(ChipHues.Fill(Hue));
    }
}