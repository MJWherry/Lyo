using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Tests;

public sealed class ReportDesignTreeExpandTests
{
    [Fact]
    public void AncestorKeys_NestedBlock_ExpandsSubsectionAndParents()
    {
        var block = new Block { Content = "inner" };
        var nested = new Section { Id = "nested", Controls = [block] };
        var child = new Section { Id = "child", Subsections = [nested] };
        var top = new Section { Id = "top", Subsections = [child] };
        var keys = ReportDesignTreeExpand.AncestorKeys([top], new() { Section = nested, Block = block });
        Assert.Contains(ReportDesignTreeExpand.SectionKey(child), keys);
        Assert.Contains(ReportDesignTreeExpand.SectionKey(nested), keys);
        Assert.Contains(ReportDesignTreeExpand.SectionKey(top), keys);
    }

    [Fact]
    public void AncestorKeys_BlockInCollapsedParent_IncludesTopSection()
    {
        var block = new Block { Content = "x" };
        var section = new Section { Id = "s", Controls = [block] };
        var keys = ReportDesignTreeExpand.AncestorKeys([section], new() { Section = section, Block = block });
        Assert.Contains(ReportDesignTreeExpand.SectionKey(section), keys);
    }

    [Fact]
    public void AncestorKeys_SelectedGrid_IncludesGridKey()
    {
        var grid = new Grid { Title = "kpi" };
        var section = new Section { Id = "s1", Controls = [grid] };
        var keys = ReportDesignTreeExpand.AncestorKeys([section], new() { Section = section, Grid = grid });
        Assert.Contains(ReportDesignTreeExpand.GridKey(section, grid), keys);
        Assert.Contains(ReportDesignTreeExpand.SectionKey(section), keys);
    }

    [Fact]
    public void AncestorKeys_SelectedCardInGrid_IncludesGridKey()
    {
        var card = new Card { Label = "Revenue" };
        var grid = new Grid { Controls = [card] };
        var section = new Section { Id = "s1", Controls = [grid] };
        var keys = ReportDesignTreeExpand.AncestorKeys([section], new() { Section = section, Card = card, Grid = grid });
        Assert.Contains(ReportDesignTreeExpand.GridKey(section, grid), keys);
        Assert.Contains(ReportDesignTreeExpand.SectionKey(section), keys);
    }

    [Fact]
    public void AncestorKeys_SelectedTableColumn_IncludesTableKey()
    {
        var column = new TableColumn { Header = "Name" };
        var table = new Table { Title = "Lines", Columns = [column] };
        var section = new Section { Id = "s1", Controls = [table] };
        var keys = ReportDesignTreeExpand.AncestorKeys([section], new() { Section = section, TableColumn = column });
        Assert.Contains(ReportDesignTreeExpand.TableKey(section, table), keys);
        Assert.Contains(ReportDesignTreeExpand.SectionKey(section), keys);
    }

    [Fact]
    public void SectionKey_MissingSectionId_DoesNotWriteId()
    {
        var section = new Section { Title = "S" };
        Assert.True(string.IsNullOrWhiteSpace(section.Id));
        var key = ReportDesignTreeExpand.SectionKey(section);
        Assert.True(string.IsNullOrWhiteSpace(section.Id));
        Assert.Equal(key, ReportDesignTreeExpand.SectionKey(section));
    }
}
