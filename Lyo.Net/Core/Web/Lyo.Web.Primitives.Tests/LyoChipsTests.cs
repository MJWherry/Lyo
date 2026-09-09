using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoChipsTests
{
    [Fact]
    public void FromEnum_Spec_Overload_Keeps_Named_Hue()
    {
        var spec = LyoChips.FromEnum<DayOfWeek>("Friday", _ => LyoChips.Of("Friday", LyoChipHue.Lime));
        Assert.Equal("Friday", spec.Label);
        Assert.Equal(LyoChipHue.Lime.Style, spec.Style);
        Assert.Equal(Color.Default, spec.Color);
    }
}
