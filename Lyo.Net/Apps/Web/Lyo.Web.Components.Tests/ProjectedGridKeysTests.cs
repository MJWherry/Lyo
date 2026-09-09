using Lyo.Web.Components.DataGrid;

namespace Lyo.Web.Components.Tests;

public class ProjectedGridKeysTests
{
    [Fact]
    public void RowsFromKeys_BuildsIdDictionaries()
    {
        var rows = ProjectedGridKeys.RowsFromKeys([["a"], [1], []]);
        Assert.Equal(2, rows.Count);
        Assert.Equal("a", ProjectedValueHelper.GetValue(rows[0], "Id"));
        Assert.Equal(1, ProjectedValueHelper.GetValue(rows[1], "Id"));
    }
}
