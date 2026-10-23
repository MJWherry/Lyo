namespace Lyo.Story.Web.Components.Tests;

public class StoryGraphDiagramTests
{
    [Fact]
    public void Create_HeistShape_PutsStartShapeOnThePage()
    {
        var draft = StoryDraftFixtures.HeistShape();
        var picture = StoryGraphDiagram.Create("The Heist", draft);
        var layout = StoryGraphLayout.Build(draft);
        var startLayout = layout.Nodes.Single(node => node.Key == "vault_door");
        var page = Assert.Single(picture.Document.Pages);
        var start = page.Cells.Single(cell => cell.IsVertex && cell.Value == "vault_door");
        Assert.Equal(startLayout.X, start.Geometry!.X!.Value);
        Assert.Equal(startLayout.Y, start.Geometry.Y!.Value);
    }
}