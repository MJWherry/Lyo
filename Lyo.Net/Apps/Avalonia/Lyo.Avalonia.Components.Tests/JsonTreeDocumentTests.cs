namespace Lyo.Desktop.Components.Tests;

public class JsonTreeDocumentTests
{
    private const string Sample = """{"id":"r2982","tags":["a",{"n":1}]}""";

    [Fact]
    public void VisibleRows_CollapsedNode_OmitsItsChildren()
    {
        var doc = Parse(Sample);
        var rows = doc.VisibleRows;
        Assert.Contains(rows, row => row.Key == "id" && row.Value == "r2982");
        Assert.Contains(rows, row => row.Key == "tags");
        Assert.DoesNotContain(rows, row => row.Value == "a");
        Assert.DoesNotContain(rows, row => row.Key == "n");
    }

    [Fact]
    public void Search_NestedValue_ExpandsAncestorsAndReportsTheMatch()
    {
        var doc = Parse("""{"outer":{"inner":"needle"}}""");
        doc.Search("needle");

        Assert.Equal(1, doc.SearchCurrent);
        Assert.Equal(1, doc.SearchTotal);
        var hit = Assert.Single(doc.VisibleRows, row => row.SearchHit);
        Assert.Equal("inner", hit.Key);
        Assert.Equal("needle", hit.Value);
    }

    [Fact]
    public void ExpandAll_OpensEveryContainer()
    {
        var doc = Parse(Sample);
        doc.ExpandAll();
        Assert.Contains(doc.VisibleRows, row => row.Value == "a");
        Assert.Contains(doc.VisibleRows, row => row.Key == "n" && row.Value == "1");
    }

    [Fact]
    public void CollapseAll_LeavesOnlyTheRoot()
    {
        var doc = Parse(Sample);
        doc.ExpandAll();
        doc.CollapseAll();
        var row = Assert.Single(doc.VisibleRows);
        Assert.Equal("", row.Key);
        Assert.False(row.Expanded);
    }

    [Fact]
    public void CopyText_String_ReturnsTheString()
    {
        var doc = Parse(Sample);
        var id = Assert.Single(doc.VisibleRows, row => row.Key == "id");
        Assert.Equal("r2982", id.CopyText);
    }

    [Fact]
    public void CopyText_Object_ReturnsIndentedJson()
    {
        var doc = Parse(Sample);
        var root = Assert.Single(doc.VisibleRows, row => row.Key == "");
        Assert.Contains("\"id\": \"r2982\"", root.CopyText, StringComparison.Ordinal);
        Assert.Contains("\n", root.CopyText, StringComparison.Ordinal);
    }

    private static JsonTreeDocument Parse(string json)
    {
        Assert.True(JsonTreeDocument.TryParse(json, out var doc));
        return doc!;
    }
}
