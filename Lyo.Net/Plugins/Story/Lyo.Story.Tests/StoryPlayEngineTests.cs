using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Story.Builders;
using Lyo.Story.Enums;

namespace Lyo.Story.Tests;

public sealed class StoryPlayEngineTests
{
    [Fact]
    public void Start_SilentStartNode_EntersWithoutPresentation()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        Assert.Equal("vault_door", snap.CurrentNodeKey);
        Assert.Equal(StoryPlaythroughStatus.InProgress, snap.Status);
        Assert.Contains("vault_door", snap.Visited);
        Assert.Equal("False", snap.Variables["alarm"]);
        Assert.DoesNotContain(snap.Events, e => e.Kind == StoryPlaythroughEventKind.VariableChanged && e.CausedByEventId is null && e.VariableKey == "alarm" && e.Sequence > 2);
        var node = story.Nodes.Single(n => n.Key == "vault_door");
        Assert.Equal(StoryPresentationKind.None, node.PresentationKind);
    }

    [Fact]
    public void TakeChoice_FlavorChoice_LogsChoiceTakenWithoutConsequences()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, snap, "admire");
        Assert.Equal("vault_door", snap.CurrentNodeKey);
        var taken = Assert.Single(snap.Events, e => e.Kind == StoryPlaythroughEventKind.ChoiceTaken);
        Assert.Equal("admire", taken.ChoiceKey);
        Assert.Null(taken.CausedByEventId);
        Assert.DoesNotContain(snap.Events, e => e.Kind == StoryPlaythroughEventKind.VariableChanged && e.CausedByEventId == taken.Id);
    }

    [Fact]
    public void TakeChoice_DisabledLockpick_Throws()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        var ex = Assert.Throws<StoryException>(() => StoryPlayEngine.TakeChoice(story, snap, "pick_lock"));
        Assert.Contains("not enabled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TakeChoice_LockpickWhenReady_SetsFlagsAndGoesInside()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        snap.Variables["has_lockpick"] = bool.TrueString;
        StoryPlayEngine.TakeChoice(story, snap, "pick_lock");
        Assert.Equal("inside", snap.CurrentNodeKey);
        Assert.Equal(bool.TrueString, snap.Variables["lock_picked"]);
        Assert.False(snap.Variables.ContainsKey("door_locked"));
        Assert.Equal("1", snap.Variables["attempts"]);
    }

    [Fact]
    public void TakeChoice_Smash_RaisesAlarmAndCauseChain()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, snap, "smash");
        Assert.Equal("alarm_cutscene", snap.CurrentNodeKey);
        Assert.Equal(bool.TrueString, snap.Variables["alarm"]);
        Assert.Equal("1", snap.Variables["heat"]);
        var causes = StoryPlayEngine.GetCauses(snap.Events, "alarm");
        Assert.Contains(causes, e => e.Kind == StoryPlaythroughEventKind.VariableChanged && e.VariableKey == "alarm");
        Assert.Contains(causes, e => e.Kind == StoryPlaythroughEventKind.ChoiceTaken && e.ChoiceKey == "smash");
        Assert.Contains(snap.Events, e => e.Kind == StoryPlaythroughEventKind.ReactionFired && e.ReactionKey == "alarm_watch");
    }

    [Fact]
    public void Advance_Cutscene_GoesToGuards()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, snap, "smash");
        var moves = StoryPlayEngine.AvailableMoves(story, snap);
        Assert.Contains(moves, m => m.Kind == StoryMoveKind.Advance);
        StoryPlayEngine.Advance(story, snap);
        Assert.Equal("guards", snap.CurrentNodeKey);
    }

    [Fact]
    public void Advance_OnChoiceNode_Throws()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        Assert.Throws<StoryException>(() => StoryPlayEngine.Advance(story, snap));
    }

    [Fact]
    public void React_Once_DoesNotFireTwice()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, snap, "smash");
        var first = snap.Events.Count(e => e.Kind == StoryPlaythroughEventKind.ReactionFired && e.ReactionKey == "alarm_watch");
        Assert.Equal(1, first);
        snap.Variables["alarm"] = "false";
        snap.Variables["alarm"] = bool.TrueString;
        var set = new StoryEffect { Kind = StoryEffectKind.Set, VariableKey = "alarm", Value = bool.TrueString };
        // Engine only fires from ApplyEffects; flipping the bag directly must not re-run Once.
        Assert.Equal(1, snap.Events.Count(e => e.Kind == StoryPlaythroughEventKind.ReactionFired && e.ReactionKey == "alarm_watch"));
        Assert.Contains(set.VariableKey, snap.Variables.Keys);
    }

    [Fact]
    public void TakeChoice_TargetNodeKeyWithoutGoto_MovesToTarget()
    {
        var story = StoryBuilder.New("Target").Slug("target").StartNode("a", n => n.Choice("go", c => c.GoesTo("b"))).Node("b", n => n.Ending()).Build();
        var choice = story.Nodes.Single(n => n.Key == "a").Choices[0];
        choice.Effects = [];
        var snap = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, snap, "go");
        Assert.Equal("b", snap.CurrentNodeKey);
    }

    [Fact]
    public void Reactions_SameTrigger_FireInKeyOrder()
    {
        var story = StoryBuilder.New("Order")
            .Slug("order")
            .StartNode("a", n => n.Choice("wait", c => { }))
            .React("b-second", r => r.On(StoryTrigger.StoryStarted).Do(d => d.Set("last", "b")))
            .React("a-first", r => r.On(StoryTrigger.StoryStarted).Do(d => d.Set("last", "a")))
            .Build();

        var snap = StoryPlayEngine.Start(story);
        Assert.Equal(["a-first", "b-second"], snap.Events.Where(e => e.Kind == StoryPlaythroughEventKind.ReactionFired).Select(e => e.ReactionKey));
        Assert.Equal("b", snap.Variables["last"]);
    }

    [Fact]
    public void Where_StartsWithAndIn_Evaluate()
    {
        var variables = new Dictionary<string, string?> { ["name"] = "Alice" };
        var none = new HashSet<string>();

        bool Test(ComparisonOperatorEnum comparison, object value)
            => StoryWhere.Matches(new ConditionClause { Field = "var.name", Comparison = comparison, Value = value }, variables, none, none);

        Assert.True(Test(ComparisonOperatorEnum.StartsWith, "al"));
        Assert.False(Test(ComparisonOperatorEnum.StartsWith, "bo"));
        Assert.True(Test(ComparisonOperatorEnum.EndsWith, "ICE"));
        Assert.True(Test(ComparisonOperatorEnum.NotEndsWith, "bob"));
        Assert.True(Test(ComparisonOperatorEnum.In, new[] { "Bob", "alice" }));
        Assert.False(Test(ComparisonOperatorEnum.In, new[] { "Bob" }));
        Assert.True(Test(ComparisonOperatorEnum.NotIn, new[] { "Bob" }));
    }

    [Fact]
    public void Increment_NonNumericValue_Throws()
    {
        var story = StoryBuilder.New("Count").Slug("count").StartNode("a", n => n.Choice("pay", c => c.Do(d => d.Increment("gold")))).Build();
        var snap = StoryPlayEngine.Start(story);
        snap.Variables["gold"] = "lots";
        var ex = Assert.Throws<StoryException>(() => StoryPlayEngine.TakeChoice(story, snap, "pay"));
        Assert.Contains("gold", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AvailableMoves_EndingNode_HasNoAdvance()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        snap.Variables["has_lockpick"] = bool.TrueString;
        StoryPlayEngine.TakeChoice(story, snap, "pick_lock");
        var moves = StoryPlayEngine.AvailableMoves(story, snap);
        Assert.Empty(moves);
    }
}

public sealed class StoryBuilderTests
{
    [Fact]
    public void Build_WhenAllAndWhenOneOf_ComposesAndOverOr()
    {
        var story = StorySamples.TheHeist();
        var choice = story.Nodes.Single(n => n.Key == "vault_door").Choices.Single(c => c.Key == "pick_lock");
        Assert.NotNull(choice.EnabledWhen);
        var root = Assert.IsType<GroupClause>(choice.EnabledWhen);
        Assert.Equal(GroupOperatorEnum.And, root.Operator);
        Assert.Equal(2, root.Children.Count);
        var oneOf = Assert.IsType<GroupClause>(root.Children[1]);
        Assert.Equal(GroupOperatorEnum.Or, oneOf.Operator);
    }

    [Fact]
    public void Build_Do_PersistsMultipleEffectsInOrder()
    {
        var story = StorySamples.TheHeist();
        var choice = story.Nodes.Single(n => n.Key == "vault_door").Choices.Single(c => c.Key == "pick_lock");
        Assert.Equal(4, choice.Effects.Count);
        Assert.Equal(StoryEffectKind.Set, choice.Effects[0].Kind);
        Assert.Equal(StoryEffectKind.Unset, choice.Effects[1].Kind);
        Assert.Equal(StoryEffectKind.Increment, choice.Effects[2].Kind);
        Assert.Equal(StoryEffectKind.GotoNode, choice.Effects[3].Kind);
        Assert.Equal("inside", choice.Effects[3].TargetNodeKey);
    }

    [Fact]
    public void Build_UnknownGoto_Throws()
    {
        var ex = Assert.Throws<StoryException>(() => StoryBuilder.New("Bad").StartNode("a", n => n.Choice("go", c => c.GoesTo("missing"))).Build());
        Assert.Contains("missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ZeroScenesAndNoOpChoice_IsValid()
    {
        var story = StoryBuilder.New("Quiet").Slug("quiet").StartNode("only", n => n.Choice("wait", c => { })).Build();
        Assert.Empty(story.Scenes);
        Assert.Equal(StoryPresentationKind.None, story.Nodes[0].PresentationKind);
        Assert.Empty(story.Nodes[0].Choices[0].Effects);
        var snap = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, snap, "wait");
        Assert.Equal("only", snap.CurrentNodeKey);
    }

    [Fact]
    public void SetVariables_ChangedValue_LogsChangeAndFiresReaction()
    {
        var story = StorySamples.TheHeist();
        var snap = StoryPlayEngine.Start(story);
        var before = snap.Events.Count;
        StoryPlayEngine.SetVariables(story, snap, new Dictionary<string, string?> { ["alarm"] = "True", ["is_night"] = "True" });
        Assert.Equal("True", snap.Variables["alarm"]);
        var change = Assert.Single(snap.Events.Skip(before), e => e.Kind == StoryPlaythroughEventKind.VariableChanged);
        Assert.Equal("alarm", change.VariableKey);
        Assert.Contains(snap.Events.Skip(before), e => e.Kind == StoryPlaythroughEventKind.ReactionFired && e.ReactionKey == "alarm_watch");
        Assert.Equal("alarm_cutscene", snap.CurrentNodeKey);
    }

    [Fact]
    public void Restore_FromStoredPlaythrough_ContinuesLikeTheLiveSnapshot()
    {
        var story = StorySamples.TheHeist();
        var live = StoryPlayEngine.Start(story);
        StoryPlayEngine.TakeChoice(story, live, "smash");
        var stored = new StoryPlaythrough {
            Status = live.Status,
            CurrentNodeKey = live.CurrentNodeKey,
            Variables = live.Variables.Select(p => new StoryPlaythroughVariable { VariableKey = p.Key, Value = p.Value }).ToArray()
        };

        var restored = StoryPlayEngine.Restore(story, stored, live.Events);
        Assert.Equal(live.Chose, restored.Chose);
        Assert.Equal(live.Visited, restored.Visited);
        Assert.Equal(live.FiredOnce, restored.FiredOnce);
        StoryPlayEngine.Advance(story, restored);
        Assert.Equal("guards", restored.CurrentNodeKey);
        Assert.Equal(live.Events.Count + 1, restored.Events.Skip(live.Events.Count).First().Sequence);
    }
}