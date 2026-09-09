using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Tests;

public sealed class ReportSectionBodyTests
{
    [Fact]
    public void Enumerate_AllZeroOrder_IsControlsThenSubsections()
    {
        var section = new Section {
            Controls = [new Block { Content = "b1" }, new Block { Content = "b2" }, new Grid { Title = "g1" }],
            Subsections = [new() { Title = "child", Order = 0 }]
        };
        var items = SectionBody.Enumerate(section);
        Assert.Equal(
            [SectionBodyKind.Control, SectionBodyKind.Control, SectionBodyKind.Control, SectionBodyKind.Subsection],
            items.Select(i => i.Kind).ToList());
        Assert.Equal("b1", ((Block)items[0].Control!).Content);
        Assert.Equal("b2", ((Block)items[1].Control!).Content);
        Assert.Equal("g1", ((Grid)items[2].Control!).Title);
        Assert.Equal("child", items[3].Subsection!.Title);
        Assert.True(SectionBody.IsLegacy(section));
    }

    [Fact]
    public void Enumerate_MixedOrders_InterleavesControlsAndSubsections()
    {
        var child = new Section { Title = "child", Order = 2 };
        var section = new Section {
            Controls = [new Block { Content = "first", Order = 1 }, new Block { Content = "after", Order = 3 }],
            Subsections = [child]
        };
        var items = SectionBody.Enumerate(section);
        Assert.Equal(SectionBodyKind.Control, items[0].Kind);
        Assert.Equal(SectionBodyKind.Subsection, items[1].Kind);
        Assert.Equal(SectionBodyKind.Control, items[2].Kind);
        Assert.Equal("first", ((Block)items[0].Control!).Content);
        Assert.Equal("child", items[1].Subsection!.Title);
        Assert.Equal("after", ((Block)items[2].Control!).Content);
        Assert.False(SectionBody.IsLegacy(section));
    }

    [Fact]
    public void Move_BlockAndTable_Interleaves()
    {
        var section = new Section {
            Controls = [new Block { Content = "b1" }, new Block { Content = "b2" }, new Table { Title = "t1" }]
        };
        var items = SectionBody.Enumerate(section);
        Assert.True(SectionBody.Move(section, items[2], -1));
        var after = SectionBody.Enumerate(section);
        Assert.Equal("b1", ((Block)after[0].Control!).Content);
        Assert.Equal("t1", ((Table)after[1].Control!).Title);
        Assert.Equal("b2", ((Block)after[2].Control!).Content);
        Assert.False(SectionBody.IsLegacy(section));
    }

    [Fact]
    public void Relocate_Block_MovesToOtherSection()
    {
        var source = new Section { Title = "S", Controls = [new Block { Content = "keep" }, new Block { Content = "move" }] };
        var dest = new Section { Title = "D", Controls = [new Block { Content = "existing" }] };
        var moving = SectionBody.Enumerate(source)[1];
        Assert.True(SectionBody.Relocate(source, dest, moving, 0));
        Assert.Single(source.Controls);
        Assert.Equal("keep", ((Block)source.Controls[0]).Content);
        Assert.Equal(2, dest.Controls.Count);
        Assert.Equal("move", ((Block)SectionBody.Enumerate(dest)[0].Control!).Content);
        Assert.Same(moving.Control, SectionBody.Enumerate(dest)[0].Control);
    }

    [Fact]
    public void Relocate_Subsection_RejectsSelfParent()
    {
        var child = new Section { Title = "child" };
        var parent = new Section { Title = "parent", Subsections = [child] };
        var item = SectionBody.Enumerate(parent).Single(i => i.Kind == SectionBodyKind.Subsection);
        Assert.False(SectionBody.Relocate(parent, child, item, 0));
        Assert.Single(parent.Subsections);
    }

    [Fact]
    public void Contains_NestedSubsection_IsTrue()
    {
        var inner = new Section { Title = "inner" };
        var mid = new Section { Title = "mid", Subsections = [inner] };
        var root = new Section { Title = "root", Subsections = [mid] };
        Assert.True(SectionBody.Contains(root, inner));
        Assert.True(SectionBody.Contains(root, root));
        Assert.False(SectionBody.Contains(inner, root));
    }

    [Fact]
    public void RelocateToGrid_NestedGrid_ReturnsFalse()
    {
        var nested = new Grid { Title = "inner" };
        var source = new Section { Controls = [nested] };
        var dest = new Grid { Title = "dest" };
        Assert.False(SectionBody.RelocateToGrid(source, nested, dest, 0));
        Assert.Same(nested, source.Controls[0]);
        Assert.Empty(dest.Controls);
    }

    [Fact]
    public void RelocateToGrid_Card_MovesOntoGrid()
    {
        var card = new Card { Label = "A" };
        var source = new Section { Controls = [card] };
        var dest = new Grid { Title = "kpi" };
        Assert.True(SectionBody.RelocateToGrid(source, card, dest, 0));
        Assert.Empty(source.Controls);
        Assert.Same(card, dest.Controls[0]);
    }

    [Fact]
    public void MoveControlTo_Cards_Reorders()
    {
        var a = new Card { Label = "A" };
        var b = new Card { Label = "B" };
        var c = new Card { Label = "C" };
        var grid = new Grid { Controls = [a, b, c] };
        Assert.True(SectionBody.MoveControlTo(grid.Controls, a, 3));
        Assert.Equal(["B", "C", "A"], grid.Controls.OfType<Card>().Select(x => x.Label).ToList());
    }

    [Fact]
    public void MoveTableColumnTo_Cells_Permutes()
    {
        var table = new Table {
            Columns = [new() { Header = "A" }, new() { Header = "B" }, new() { Header = "C" }],
            Rows = [new() { Cells = ["a", "b", "c"] }]
        };
        Assert.True(SectionBody.MoveTableColumnTo(table, 0, 3));
        Assert.Equal(["B", "C", "A"], table.Columns.Select(x => x.Header).ToList());
        Assert.Equal(["b", "c", "a"], table.Rows[0].Cells);
    }

    [Fact]
    public void MoveTableColumn_Cells_Permutes()
    {
        var table = new Table {
            Columns = [new() { Header = "A" }, new() { Header = "B" }, new() { Header = "C" }],
            Rows = [new() { Cells = ["a", "b", "c"] }, new() { Cells = ["1", "2", "3"] }]
        };
        Assert.True(SectionBody.MoveTableColumn(table, 0, 1));
        Assert.Equal(["B", "A", "C"], table.Columns.Select(c => c.Header).ToList());
        Assert.Equal(["b", "a", "c"], table.Rows[0].Cells);
        Assert.Equal(["2", "1", "3"], table.Rows[1].Cells);
    }

    [Fact]
    public void CollectTables_FindsTablesInsideGridsAndSubsections()
    {
        var inner = new Table { Title = "nested" };
        var direct = new Table { Title = "direct" };
        var section = new Section {
            Controls = [new Grid { Controls = [inner] }, direct],
            Subsections = [new() { Controls = [new Table { Title = "child" }] }]
        };
        var titles = SectionBody.CollectTables([section]).Select(t => t.Title).ToList();
        Assert.Equal(["nested", "direct", "child"], titles);
    }

    [Fact]
    public void Serialize_AfterMove_DoesNotDuplicate()
    {
        var report = new Report<object> {
            Title = "T",
            Sections = [
                new() {
                    Title = "S",
                    Controls = [new Block { Content = "b1" }, new Block { Content = "b2" }, new Table { Title = "t1" }]
                }
            ]
        };
        var section = report.Sections[0];
        SectionBody.Move(section, SectionBody.Enumerate(section)[2], -1);
        var json = ReportJson.Serialize(report);
        var back = ReportJson.Deserialize<object>(json);
        Assert.Single(back.Sections);
        Assert.Equal(2, back.Sections[0].Controls.OfType<Block>().Count());
        Assert.Single(back.Sections[0].Controls.OfType<Table>());
        var kinds = SectionBody.Enumerate(back.Sections[0]).Select(i => i.Control).ToList();
        Assert.Equal("b1", ((Block)kinds[0]!).Content);
        Assert.Equal("t1", ((Table)kinds[1]!).Title);
        Assert.Equal("b2", ((Block)kinds[2]!).Content);
    }

    [Fact]
    public void Deserialize_LegacyColumnsContentBlocksAndTableGrids_Migrate()
    {
        const string json = """
            {
              "title": "Legacy",
              "sections": [
                {
                  "title": "S",
                  "columns": [{ "label": "Revenue", "value": "10" }],
                  "contentBlocks": [{ "content": "hello" }],
                  "grids": [
                    {
                      "title": "Lines",
                      "columns": [{ "header": "Name", "field": "Name" }],
                      "rows": [{ "cells": ["Ada"] }]
                    }
                  ]
                }
              ]
            }
            """;
        var report = ReportJson.Deserialize<object>(json);
        var section = Assert.Single(report.Sections);
        var kpi = Assert.Single(section.Controls.OfType<Grid>());
        var card = Assert.Single(kpi.Controls.OfType<Card>());
        Assert.Equal("Revenue", card.Label);
        Assert.Equal("hello", Assert.Single(section.Controls.OfType<Block>()).Content);
        var table = Assert.Single(SectionBody.CollectTables([section]));
        Assert.Equal("Lines", table.Title);
        Assert.Equal("Name", table.Columns[0].Header);
        var written = ReportJson.Serialize(report);
        Assert.Contains("\"controls\"", written, StringComparison.Ordinal);
        Assert.DoesNotContain("contentBlocks", written, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("columnBandOrder", written, StringComparison.OrdinalIgnoreCase);
    }
}
