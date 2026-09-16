using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.Story.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Story.Tests;

/// <summary>Behavior every <see cref="IStoryStore" /> backend must share. Derived classes supply a migrated service provider.</summary>
public abstract class StoryStoreContractTests
{
    protected abstract IServiceProvider Services { get; }

    private IStoryStore Store => Services.GetRequiredService<IStoryStore>();

    [Fact]
    public async Task SaveStoryGraph_RoundTripsChoicesAndEffects()
    {
        var ct = TestCt;
        var story = StorySamples.TheHeist();
        story.Slug = $"heist-{LyoGuid.CreateV7():N}";
        await Store.SaveStoryGraphAsync(story, ct);
        var loaded = await Store.GetStoryByIdAsync(story.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal("vault_door", loaded.StartNodeKey);
        var pick = loaded.Nodes.Single(n => n.Key == "vault_door").Choices.Single(c => c.Key == "pick_lock");
        Assert.Equal(4, pick.Effects.Count);
        Assert.NotNull(pick.EnabledWhen);
        Assert.Contains(loaded.Actions, a => a.Key == "raise_alarm");
        Assert.Contains(loaded.Reactions, r => r.Key == "alarm_watch");
        var cutscene = loaded.Nodes.Single(n => n.Key == "alarm_cutscene");
        Assert.Equal(StoryPresentationKind.Cutscene, cutscene.PresentationKind);
        Assert.Equal("media:heist-alarm", cutscene.MediaRef);
    }

    [Fact]
    public async Task ListStories_OtherTenant_Hidden()
    {
        var ct = TestCt;
        var tenant = LyoGuid.CreateV7();
        var game = $"game-{LyoGuid.CreateV7():N}";
        var mine = StorySamples.TheHeist();
        mine.Slug = $"mine-{LyoGuid.CreateV7():N}";
        mine.TenantId = tenant;
        mine.GameKey = game;
        var theirs = StorySamples.TheHeist();
        theirs.Slug = $"theirs-{LyoGuid.CreateV7():N}";
        theirs.TenantId = LyoGuid.CreateV7();
        theirs.GameKey = game;
        await Store.SaveStoryGraphAsync(mine, ct);
        await Store.SaveStoryGraphAsync(theirs, ct);

        var listed = await Store.ListStoriesAsync(tenant, game, ct);

        var story = Assert.Single(listed);
        Assert.Equal(mine.Id, story.Id);
        Assert.Equal(tenant, story.TenantId);
        Assert.Equal(game, story.GameKey);
        Assert.Equal(tenant, (await Store.GetStoryByIdAsync(mine.Id, ct))?.TenantId);
    }

    [Fact]
    public async Task Playthrough_SmashAdvance_MatchesEngineCauses()
    {
        var ct = TestCt;
        var story = StorySamples.TheHeist();
        story.Slug = $"play-{LyoGuid.CreateV7():N}";
        await Store.SaveStoryGraphAsync(story, ct);

        var started = await Store.StartPlaythroughAsync(story.Id, playerRef: "tester", ct);
        Assert.Equal("vault_door", started.Playthrough.CurrentNodeKey);
        Assert.Contains(started.Moves, m => m.ChoiceKey == "smash");

        var smashed = await Store.TakeChoiceAsync(started.Playthrough.Id, "smash", ct);
        Assert.Equal("alarm_cutscene", smashed.Playthrough.CurrentNodeKey);
        Assert.Contains(smashed.Moves, m => m.Kind == StoryMoveKind.Advance);

        var advanced = await Store.AdvanceAsync(started.Playthrough.Id, ct);
        Assert.Equal("guards", advanced.Playthrough.CurrentNodeKey);

        var causes = await Store.GetCausesAsync(started.Playthrough.Id, "alarm", ct);
        Assert.Contains(causes, e => e.Kind == StoryPlaythroughEventKind.VariableChanged && e.VariableKey == "alarm");
        Assert.Contains(causes, e => e.Kind == StoryPlaythroughEventKind.ChoiceTaken && e.ChoiceKey == "smash");
    }

    [Fact]
    public async Task SaveStoryGraphAsync_WithPlaythroughs_ThrowsConflictAndKeepsThem()
    {
        var ct = TestCt;
        var story = StorySamples.TheHeist();
        story.Slug = $"keep-{LyoGuid.CreateV7():N}";
        await Store.SaveStoryGraphAsync(story, ct);
        var started = await Store.StartPlaythroughAsync(story.Id, playerRef: "tester", ct);

        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveStoryGraphAsync(story, ct));

        var kept = await Store.GetPlaythroughAsync(started.Playthrough.Id, ct);
        Assert.NotNull(kept);
        Assert.NotEmpty(await Store.GetEventsAsync(started.Playthrough.Id, ct));
    }

    [Fact]
    public async Task SaveStoryGraph_DuplicateSlugDifferentId_ThrowsConflict()
    {
        var ct = TestCt;
        var slug = $"dup-{LyoGuid.CreateV7():N}";
        var first = StorySamples.TheHeist();
        first.Slug = slug;
        await Store.SaveStoryGraphAsync(first, ct);
        var second = StorySamples.TheHeist();
        second.Id = LyoGuid.CreateV7();
        second.Slug = slug;
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveStoryGraphAsync(second, ct));
    }

    [Fact]
    public async Task DeleteStoryAsync_WithPlaythroughs_RemovesStoryAndPlay()
    {
        var ct = TestCt;
        var story = StorySamples.TheHeist();
        story.Slug = $"delete-{LyoGuid.CreateV7():N}";
        await Store.SaveStoryGraphAsync(story, ct);
        var started = await Store.StartPlaythroughAsync(story.Id, playerRef: "tester", ct);

        await Store.DeleteStoryAsync(story.Id, ct);
        await Store.DeleteStoryAsync(story.Id, ct);

        Assert.Null(await Store.GetStoryByIdAsync(story.Id, ct));
        Assert.Null(await Store.GetPlaythroughAsync(started.Playthrough.Id, ct));
    }

    [Fact]
    public async Task SetVariablesAsync_ChangedValue_PersistsAndFiresReactions()
    {
        var ct = TestCt;
        var story = StorySamples.TheHeist();
        story.Slug = $"vars-{LyoGuid.CreateV7():N}";
        await Store.SaveStoryGraphAsync(story, ct);
        var started = await Store.StartPlaythroughAsync(story.Id, playerRef: "tester", ct);

        var state = await Store.SetVariablesAsync(started.Playthrough.Id, new Dictionary<string, string?> { ["alarm"] = "True" }, ct);

        Assert.Equal("alarm_cutscene", state.Playthrough.CurrentNodeKey);
        Assert.Contains(state.Playthrough.Variables, v => v.VariableKey == "alarm" && v.Value == "True");
        var events = await Store.GetEventsAsync(started.Playthrough.Id, ct);
        Assert.Contains(events, e => e.Kind == StoryPlaythroughEventKind.ReactionFired && e.ReactionKey == "alarm_watch");
    }
}
