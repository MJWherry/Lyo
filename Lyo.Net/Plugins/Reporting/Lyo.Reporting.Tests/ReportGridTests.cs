using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Tests;

public sealed class ReportGridTests
{
    [Fact]
    public void ResolveTemplateColumns_Default_IsRepeatTwo()
        => Assert.Equal("repeat(2, 1fr)", new Grid().ResolveTemplateColumns());

    [Fact]
    public void ResolveTemplateColumns_ColumnCount_IgnoresBareIntegerTemplate()
    {
        var grid = new Grid { ColumnCount = 3, TemplateColumns = "2" };
        Assert.Equal("repeat(3, 1fr)", grid.ResolveTemplateColumns());
    }

    [Fact]
    public void ResolveTemplateColumns_TrackList_Wins()
        => Assert.Equal("1fr 2fr", new Grid { ColumnCount = 4, TemplateColumns = "1fr 2fr" }.ResolveTemplateColumns());

    [Fact]
    public void ResolveTemplateColumns_RepeatAutoFit_Wins()
        => Assert.Equal(
            "repeat(auto-fit, minmax(220px, 1fr))",
            new Grid { TemplateColumns = "repeat(auto-fit, minmax(220px, 1fr))" }.ResolveTemplateColumns());
}
