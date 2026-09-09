using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoChipHueTests
{
    [Fact]
    public void Normalized_Wraps_Around_The_Wheel()
    {
        Assert.Equal(10, new LyoChipHue(LyoChipHue.CircleDegrees + 10).Normalized);
        Assert.Equal(LyoChipHue.CircleDegrees - 10, new LyoChipHue(-10).Normalized);
        Assert.Equal(LyoChipHue.Blue.Normalized, new LyoChipHue(LyoChipHue.Blue.Degrees).Normalized);
    }

    [Fact]
    public void Style_Assigns_The_Css_Variable()
    {
        Assert.Equal($"{LyoChipHue.CssVariable}:{LyoChipHue.Blue.Normalized}", LyoChipHue.Blue.Style);
        Assert.Equal(LyoChipHue.Blue.Style, LyoChips.HueStyle(LyoChipHue.Blue));
        Assert.Equal(LyoChipHue.Blue.Style, LyoChips.HueStyle(LyoChipHue.Blue.Degrees));
    }

    [Fact]
    public void Of_Hue_Overload_Uses_Named_Style()
    {
        var spec = LyoChips.Of("Html", LyoChipHue.Blue);
        Assert.Equal("Html", spec.Label);
        Assert.Equal(LyoChipHue.Blue.Style, spec.Style);
        Assert.Equal(Color.Default, spec.Color);
    }
}
