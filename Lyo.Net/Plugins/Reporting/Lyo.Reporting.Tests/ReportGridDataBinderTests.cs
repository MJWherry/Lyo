using System.Globalization;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Tests;

public sealed class ReportGridDataBinderTests
{
    [Fact]
    public void Apply_FieldMap_MapsColumns()
    {
        var table = new Table {
            DataSourceKind = DataSourceKind.FromParameter,
            DataParameterKey = "Lines",
            FieldMap = { ["Name"] = "client" },
            Columns = [new() { Header = "Name", Field = "Name" }, new() { Header = "Total", Field = "Total" }]
        };

        TableDataBinder.Apply(
            table, [
                new Dictionary<string, object?> { ["client"] = "Ada", ["Total"] = 12 },
                new Dictionary<string, object?> { ["client"] = "Lin", ["Total"] = 3 }
            ]);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Ada", table.Rows[0].Cells[0]);
        Assert.Equal(12, Convert.ToInt32(table.Rows[0].Cells[1], CultureInfo.InvariantCulture));
        Assert.Equal("Lin", table.Rows[1].Cells[0]);
    }

    [Fact]
    public void Apply_MissingRows_ClearsTable()
    {
        var table = new Table { Columns = [new() { Header = "A" }], Rows = [new() { Cells = ["keep"] }] };
        TableDataBinder.Apply(table, null);
        Assert.Empty(table.Rows);
    }

    [Fact]
    public void ProjectRows_SourceTable_IsUnchanged()
    {
        var table = new Table { Columns = [new() { Field = "Name" }], Rows = [new() { Cells = ["keep"] }] };
        var projected = TableDataBinder.ProjectRows(table, [new Dictionary<string, object?> { ["Name"] = "Ada" }]);
        Assert.Equal("keep", table.Rows[0].Cells[0]);
        Assert.Equal("Ada", projected[0].Cells[0]);
    }

    [Fact]
    public void ParseRows_JsonArray_ReadsRows()
    {
        var rows = TableDataBinder.ParseRows("""[{"Name":"Ada","Total":1}]""");
        Assert.Single(rows);
        Assert.Equal("Ada", rows[0]["Name"]);
    }
}
