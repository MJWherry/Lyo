using Lyo.Story.Web.Components;

namespace Lyo.Story.Web.Components.Tests;

public class StoryGraphCsharpTests
{
    [Fact]
    public void Format_HeistShape_EmitsStartNodeAndWhenAll()
    {
        var text = StoryGraphCsharp.Format(StoryDraftFixtures.Snippet());

        Assert.Contains("StoryBuilder.New(\"The Heist\")", text, StringComparison.Ordinal);
        Assert.Contains(".Slug(\"the-heist\")", text, StringComparison.Ordinal);
        Assert.Contains("StoryVariableType.Boolean", text, StringComparison.Ordinal);
        Assert.Contains(".StartNode(\"vault_door\"", text, StringComparison.Ordinal);
        Assert.Contains(".WhenAll(", text, StringComparison.Ordinal);
    }
}
