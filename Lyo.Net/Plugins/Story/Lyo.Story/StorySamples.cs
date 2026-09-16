using Lyo.Story.Builders;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>Canonical sample graphs for tests and Tools.Postgres seed.</summary>
public static class StorySamples
{
    /// <summary>Mixed quest: silent beats, one cutscene, optional text, All/OneOf conditions, named action, Once reaction.</summary>
    public static Story TheHeist()
        => StoryBuilder.New("The Heist")
            .Slug("the-heist")
            .Description("Sample play graph used by tests and seed.")
            .Language("en")
            .Variable("alarm", StoryVariableType.Boolean, false)
            .Variable("heat", StoryVariableType.Integer, 0)
            .Variable("has_lockpick", StoryVariableType.Boolean, false)
            .Variable("is_night", StoryVariableType.Boolean, true)
            .Variable("guards_away", StoryVariableType.Boolean, false)
            .Variable("lock_picked", StoryVariableType.Boolean, false)
            .Variable("door_locked", StoryVariableType.Boolean, true)
            .Variable("attempts", StoryVariableType.Integer, 0)
            .Action("raise_alarm", a => a.Do(d => d
                .Set("alarm", true)
                .Increment("heat")
                .GoesTo("alarm_cutscene")))
            .StartNode("vault_door", n => n
                .Choice("pick_lock", "Pick the lock", c => c
                    .WhenAll(w => w
                        .Flag("has_lockpick")
                        .NotFlag("alarm"))
                    .WhenOneOf(w => w
                        .Flag("is_night")
                        .Flag("guards_away"))
                    .Do(d => d
                        .Set("lock_picked", true)
                        .Unset("door_locked")
                        .Increment("attempts")
                        .GoesTo("inside")))
                .Choice("smash", "Smash the glass", c => c.Do(d => d.Invoke("raise_alarm")))
                .Choice("admire", "Admire the door", c => { }))
            .Node("inside", n => n.Ending())
            .Node("alarm_cutscene", n => n
                .Cutscene("media:heist-alarm")
                .On(StoryTrigger.Advanced, r => r.Do(d => d.GoesTo("guards"))))
            .Node("guards", n => n.Ending())
            .React("alarm_watch", r => r
                .On(StoryTrigger.VariableChanged, "alarm")
                .WhenAll(w => w.Eq("alarm", true))
                .Once()
                .Do(d => d.GoesTo("alarm_cutscene")))
            .Build();
}
