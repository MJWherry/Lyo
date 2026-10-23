using Lyo.Story.Enums;

namespace Lyo.Story.Web.Components.Tests;

public class StoryGraphChecksTests
{
    [Fact]
    public void Check_UnknownGoto_ReportsTarget()
    {
        var missing = StoryGraphChecks.Check(StoryDraftFixtures.Goto("missing"));
        Assert.Contains(missing, issue => issue.Contains("missing", StringComparison.Ordinal));
        var known = StoryGraphChecks.Check(StoryDraftFixtures.Goto("inside"));
        Assert.DoesNotContain(known, issue => issue.Contains("Goto", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_UnknownAction_ReportsInvoke()
    {
        var draft = StoryDraftFixtures.Goto("inside");
        draft.Nodes[0].Choices[0].Effects.Add(new() { Kind = StoryEffectKind.InvokeAction, InvokeActionKey = "nope" });
        var issues = StoryGraphChecks.Check(draft);
        Assert.Contains(issues, issue => issue.Contains("nope", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_DuplicateNodeKey_ReportsKey()
    {
        var draft = new StoryDraft { Nodes = [new() { Key = "vault" }, new() { Key = "vault" }] };
        var issues = StoryGraphChecks.Check(draft);
        Assert.Contains(issues, issue => issue.Contains("vault", StringComparison.Ordinal));
    }
}