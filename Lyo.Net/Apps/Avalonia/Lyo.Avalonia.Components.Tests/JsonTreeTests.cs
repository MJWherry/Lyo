namespace Lyo.Desktop.Components.Tests;

public class JsonTreeTests
{
    [Fact]
    public void TryParse_Object_NestsPropertiesAndArrays()
    {
        var ok = JsonTree.TryParse("""{"id":"r2982","tags":["a",{"n":1}]}""", out var nodes);

        Assert.True(ok);
        var root = Assert.Single(nodes);
        Assert.Equal(2, root.Children.Count);
        Assert.Contains("id", root.Children[0].Label, StringComparison.Ordinal);
        Assert.Contains("r2982", root.Children[0].Label, StringComparison.Ordinal);
        Assert.Equal(2, root.Children[1].Children.Count);
        Assert.Contains("n", root.Children[1].Children[1].Label, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_Css_ReturnsFalse()
    {
        var ok = JsonTree.TryParse(".item{display:block}", out var nodes);

        Assert.False(ok);
        Assert.Empty(nodes);
    }
}
