using Lyo.Desktop.Components;

namespace Lyo.Web.Automation.Studio.Tests;

public class NetworkTypeFilterTests
{
    [Fact]
    public void Matches_BlankSelection_KeepsEveryType()
    {
        Assert.True(NetworkTypeFilter.Matches("image", null));
        Assert.True(NetworkTypeFilter.Matches("xhr", "  "));
        Assert.True(NetworkTypeFilter.Matches(null, "all"));
    }

    [Fact]
    public void Matches_Type_KeepsOnlyThatType()
    {
        Assert.True(NetworkTypeFilter.Matches("xhr", "xhr"));
        Assert.False(NetworkTypeFilter.Matches("image", "xhr"));
        Assert.True(NetworkTypeFilter.Matches(null, "other"));
        Assert.False(NetworkTypeFilter.Matches("document", "other"));
    }

    [Fact]
    public void Chip_MethodStatusAndType_UseDistinctHues()
    {
        Assert.Equal(ChipHues.Green, NetworkChip.Method("get").Hue);
        Assert.Equal(ChipHues.Blue, NetworkChip.Method("POST").Hue);
        Assert.Equal(ChipHues.Green, NetworkChip.Status(200).Hue);
        Assert.Equal(ChipHues.Amber, NetworkChip.Status(404).Hue);
        Assert.Equal("xhr", NetworkChip.Type("XHR").Label);
        Assert.Equal(ChipHues.Teal, NetworkChip.Type("fetch").Hue);
    }
}