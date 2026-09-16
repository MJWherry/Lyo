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
        Assert.Contains($"{LyoChipHue.CssVariable}:{LyoChipHue.Blue.Normalized}", LyoChipHue.Blue.Style, StringComparison.Ordinal);
        Assert.Contains("var(--mud-palette-surface)", LyoChipHue.Blue.Style, StringComparison.Ordinal);
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

    [Fact]
    public void All_NamedHues_AreAtLeast18DegreesApart()
    {
        var hues = LyoChipHue.All;
        for (var i = 0; i < hues.Count; i++) {
            for (var j = i + 1; j < hues.Count; j++)
                Assert.True(CircularDistance(hues[i].Normalized, hues[j].Normalized) >= 18, $"{hues[i].Normalized} and {hues[j].Normalized}");
        }
    }

    [Fact]
    public void All_NamedHues_StayClearOfStatusColors()
    {
        int[] anchors = [4, 36, 122, 207];
        foreach (var hue in LyoChipHue.All) {
            foreach (var anchor in anchors)
                Assert.True(CircularDistance(hue.Normalized, anchor) >= 12, $"{hue.Normalized} vs {anchor}");
        }
    }

    [Fact]
    public void All_ListsNamedHuesInWheelOrder()
    {
        Assert.Equal(
            [
                LyoChipHue.Coral, LyoChipHue.Amber, LyoChipHue.Lime, LyoChipHue.Green,
                LyoChipHue.Teal, LyoChipHue.Cyan, LyoChipHue.Blue, LyoChipHue.Indigo,
                LyoChipHue.Violet, LyoChipHue.Purple, LyoChipHue.Magenta, LyoChipHue.Rose
            ],
            LyoChipHue.All);

        var degrees = LyoChipHue.All.Select(hue => hue.Normalized).ToArray();
        for (var i = 1; i < degrees.Length; i++)
            Assert.True(degrees[i] > degrees[i - 1]);
    }

    [Fact]
    public void FromEnum_Hue_SetsStyle()
    {
        var spec = LyoChips.FromEnum<DayOfWeek>("Friday", _ => LyoChipHue.Lime, _ => "event");
        Assert.Equal("Friday", spec.Label);
        Assert.Equal(LyoChipHue.Lime.Style, spec.Style);
        Assert.Equal(Color.Default, spec.Color);
        Assert.Equal("event", spec.Icon);

        var none = LyoChips.FromEnum<DayOfWeek>("Friday", _ => (LyoChipHue?)null);
        Assert.Equal("Friday", none.Label);
        Assert.Null(none.Style);
        Assert.Equal(Color.Default, none.Color);

        var unknown = LyoChips.FromEnum<DayOfWeek>("Notsaday", _ => LyoChipHue.Lime);
        Assert.Equal("Notsaday", unknown.Label);
        Assert.Null(unknown.Style);
        Assert.Equal(Color.Default, unknown.Color);
    }

    private static int CircularDistance(int a, int b)
    {
        var delta = Math.Abs(a - b) % LyoChipHue.CircleDegrees;
        return Math.Min(delta, LyoChipHue.CircleDegrees - delta);
    }
}
