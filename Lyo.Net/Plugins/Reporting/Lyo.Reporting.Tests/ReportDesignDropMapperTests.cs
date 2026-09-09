using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Tests;

public sealed class ReportDesignDropMapperTests
{
    [Fact]
    public void TryRelocate_DestId_MovesBlock()
    {
        var source = new Section { Id = "src", Controls = [new Block { Content = "keep" }, new Block { Content = "move" }] };
        var dest = new Section { Id = "dst", Controls = [new Block { Content = "existing" }] };
        var item = SectionBody.Enumerate(source)[1];
        Assert.True(ReportDesignDropMapper.TryRelocate([source, dest], item, "dst", 0));
        Assert.Equal("move", ((Block)SectionBody.Enumerate(dest)[0].Control!).Content);
        Assert.Single(source.Controls);
    }

    [Fact]
    public void TryRelocate_MissingDest_ReturnsFalse()
    {
        var source = new Section { Id = "src", Controls = [new Block { Content = "a" }] };
        var item = SectionBody.Enumerate(source)[0];
        Assert.False(ReportDesignDropMapper.TryRelocate([source], item, "nope", 0));
        Assert.Single(source.Controls);
    }

    [Fact]
    public void TryRelocate_SubsectionCycle_ReturnsFalse()
    {
        var child = new Section { Id = "child" };
        var parent = new Section { Id = "parent", Subsections = [child] };
        var item = SectionBody.Enumerate(parent).Single(i => i.Kind == SectionBodyKind.Subsection);
        Assert.False(ReportDesignDropMapper.TryRelocate([parent], item, "child", 0));
        Assert.Single(parent.Subsections);
    }
}
