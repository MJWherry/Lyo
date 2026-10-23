namespace Lyo.Story.Enums;

/// <summary>What happened on a playthrough. Consequence tracking uses <c>CausedByEventId</c>, not this kind alone.</summary>
public enum StoryPlaythroughEventKind
{
    Unknown = 0,
    StoryStarted = 1,
    NodeEntered = 2,
    ChoiceTaken = 3,
    Advanced = 4,
    TriggerFired = 5,
    ActionInvoked = 6,
    ReactionFired = 7,
    VariableChanged = 8,
    PlaythroughEnded = 9
}