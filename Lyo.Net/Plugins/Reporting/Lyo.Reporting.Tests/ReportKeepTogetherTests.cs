using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Tests;

public sealed class ReportKeepTogetherTests
{
    [Theory]
    [InlineData(ContentType.Chart, true)]
    [InlineData(ContentType.Component, true)]
    [InlineData(ContentType.Address, true)]
    [InlineData(ContentType.Totals, true)]
    [InlineData(ContentType.Checkbox, true)]
    [InlineData(ContentType.Notes, true)]
    [InlineData(ContentType.PageBreak, false)]
    public void Effective_DefaultBlock_ReturnsExpected(ContentType type, bool expected)
        => Assert.Equal(expected, ReportKeepTogether.Effective(new Block { ContentType = type }));

    [Fact]
    public void Effective_PageBreakKeepTogetherTrue_ReturnsFalse()
        => Assert.False(ReportKeepTogether.Effective(new Block { ContentType = ContentType.PageBreak, KeepTogether = true }));

    [Fact]
    public void Effective_DefaultCardAndGrid_IsOn()
    {
        Assert.True(ReportKeepTogether.Effective(new Card()));
        Assert.True(ReportKeepTogether.Effective(new Grid()));
        Assert.False(ReportKeepTogether.Effective(new Grid { KeepTogether = false }));
    }

    [Fact]
    public void Effective_DefaultTable_IsOff()
    {
        Assert.False(ReportKeepTogether.Effective(new Table()));
        Assert.True(ReportKeepTogether.Effective(new Table { KeepTogether = true }));
        Assert.False(ReportKeepTogether.Effective(new Table { KeepTogether = false }));
    }

    [Fact]
    public void Deserialize_OldJson_KeepTogetherIsNull()
    {
        const string json = """{"title":"Old","sections":[{"contentBlocks":[{"contentType":0,"content":"Hi"}],"grids":[{"title":"T"}]}]}""";
        var report = ReportJson.Deserialize<object>(json);
        Assert.Null(Assert.Single(report.Sections[0].Controls.OfType<Block>()).KeepTogether);
        Assert.Null(Assert.Single(report.Sections[0].Controls.OfType<Grid>()).KeepTogether);
        Assert.DoesNotContain("keepTogether", ReportJson.Serialize(report), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_ExplicitKeepTogether_RoundTrips()
    {
        var report = new Report<object> {
            Sections = [
                new() {
                    Controls = [
                        new Block { Content = "Hi", KeepTogether = false },
                        new Table { Title = "T", KeepTogether = true },
                        new Grid { Title = "G", KeepTogether = false }
                    ]
                }
            ]
        };
        var back = ReportJson.Deserialize<object>(ReportJson.Serialize(report));
        Assert.False(Assert.Single(back.Sections[0].Controls.OfType<Block>()).KeepTogether);
        Assert.True(Assert.Single(back.Sections[0].Controls.OfType<Table>()).KeepTogether);
        Assert.False(Assert.Single(back.Sections[0].Controls.OfType<Grid>()).KeepTogether);
    }
}
