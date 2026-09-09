using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Tests;

public class ProjectedColumnRegistryCardTests
{
    private static RenderFragment<object?> Cell => _ => builder => builder.AddContent(0, "cell");

    [Fact]
    public void GetCardColumns_ColumnsWithoutCellMarkup_AreExcluded()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell);
        registry.Register("Actions", null, null);

        var columns = registry.GetCardColumns();

        Assert.Equal(["Name"], columns.Select(c => c.Field));
    }

    [Fact]
    public void GetCardColumns_VisibleFieldNames_FilterTheResult()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell);
        registry.Register("State", "State", null, cell: Cell);

        var columns = registry.GetCardColumns(["State"]);

        Assert.Equal(["State"], columns.Select(c => c.Field));
    }

    [Fact]
    public void GetCardColumns_EmptyVisibleList_KeepsEveryColumn()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell);
        registry.Register("State", "State", null, cell: Cell);

        var columns = registry.GetCardColumns([]);

        Assert.Equal(["Name", "State"], columns.Select(c => c.Field));
    }

    [Fact]
    public void GetCardColumns_DuplicateField_IsRegisteredOnce()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell);
        registry.Register(" name ", "Name again", null, cell: Cell);

        var columns = registry.GetCardColumns();

        Assert.Single(columns);
    }

    [Fact]
    public void GetCardColumns_ExplicitCardTitle_WinsOverTheNameHeuristic()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell);
        registry.Register("Subject", "Subject", null, cell: Cell, cardTitle: true);

        var title = registry.GetCardColumns().Single(c => c.IsTitle);

        Assert.Equal("Subject", title.Field);
    }

    [Fact]
    public void GetCardColumns_NoExplicitTitle_PicksTheFirstPreferredLabel()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("State", "State", null, cell: Cell);
        registry.Register("JobDefinition.Name", "Name", null, cell: Cell);

        var title = registry.GetCardColumns().Single(c => c.IsTitle);

        Assert.Equal("JobDefinition.Name", title.Field);
    }

    [Fact]
    public void GetCardColumns_TitleFallsBackToTheFieldLeaf_WhenNoTitleIsDeclared()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Person.FullName", null, null, cell: Cell);

        var title = registry.GetCardColumns().Single(c => c.IsTitle);

        Assert.Equal("Person.FullName", title.Field);
    }

    [Fact]
    public void GetCardColumns_IdentifierColumns_AreNeverThePickedTitle()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Key", "Key", null, cell: Cell, identifier: true);

        Assert.DoesNotContain(registry.GetCardColumns(), c => c.IsTitle);
    }

    [Fact]
    public void GetCardColumns_NoPreferredLabel_LeavesTheCardWithoutAHeading()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("State", "State", null, cell: Cell);
        registry.Register("CreatedTimestamp", "Created", null, cell: Cell);

        Assert.DoesNotContain(registry.GetCardColumns(), c => c.IsTitle);
    }

    [Fact]
    public void GetCardColumns_SortableFlag_IsCarriedThrough()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell, sortable: true);
        registry.Register("State", "State", null, cell: Cell);

        var columns = registry.GetCardColumns();

        Assert.True(columns.Single(c => c.Field == "Name").Sortable);
        Assert.False(columns.Single(c => c.Field == "State").Sortable);
    }

    [Fact]
    public void GetCardColumns_AfterClear_IsEmpty()
    {
        var registry = new ProjectedColumnRegistry();
        registry.Register("Name", "Name", null, cell: Cell);
        registry.Clear();

        Assert.Empty(registry.GetCardColumns());
    }
}
