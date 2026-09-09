using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Tests;

public sealed class ReportDesignSelectionTests
{
    [Fact]
    public void Matches_Section_IsNotAControl()
    {
        var card = new Card { Label = "A" };
        var section = new Section { Title = "S", Controls = [card] };
        Assert.False(new ReportDesignSelection { Section = section, Card = card }.Matches(section));
        Assert.True(new ReportDesignSelection { Section = section }.Matches(section));
        Assert.True(new ReportDesignSelection { Section = section, Card = card }.Matches(card));
    }

    [Fact]
    public void Matches_SameInstanceOnTreeAndCanvas_ReturnsTrue()
    {
        var block = new Block { Content = "live" };
        var section = new Section { Title = "S", Controls = [block] };
        var tree = new ReportDesignSelection { Section = section, Block = block };
        var canvasHit = new ReportDesignSelection { Section = section, Block = block };
        Assert.True(tree.Matches(block));
        Assert.True(canvasHit.Matches(block));
        Assert.True(new ReportDesignSelection { Section = section }.Matches(section));
    }

    [Fact]
    public void Matches_TableColumnAndRow_ReturnsTrue()
    {
        var column = new TableColumn { Header = "Name" };
        var row = new TableRow { Cells = ["Ada"] };
        var table = new Table { Columns = [column], Rows = [row] };
        var grid = new Grid { Title = "kpi" };
        Assert.True(new ReportDesignSelection { Table = table }.Matches(table));
        Assert.True(new ReportDesignSelection { TableColumn = column }.Matches(column));
        Assert.True(new ReportDesignSelection { TableRow = row }.Matches(row));
        Assert.True(new ReportDesignSelection { Grid = grid }.Matches(grid));
        Assert.True(new ReportDesignSelection { Control = table }.Matches(table));
    }

    [Fact]
    public void Relocate_AddedBlock_KeepsLiveInstance()
    {
        var source = new Section { Id = "src", Controls = [new Block { Content = "keep" }] };
        var dest = new Section { Id = "dst" };
        var added = new Block { Content = "new" };
        source.Controls.Add(added);
        var selection = new ReportDesignSelection { Section = source, Block = added };
        Assert.True(selection.Matches(source.Controls[^1]));
        var item = SectionBody.Enumerate(source).Single(i => ReferenceEquals(i.Control, added));
        Assert.True(SectionBody.Relocate(source, dest, item, 0));
        Assert.Same(added, SectionBody.Enumerate(dest)[0].Control);
        Assert.True(new ReportDesignSelection { Section = dest, Block = added }.Matches(added));
    }
}
