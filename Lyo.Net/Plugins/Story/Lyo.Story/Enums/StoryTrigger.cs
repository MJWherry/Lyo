namespace Lyo.Story.Enums;

/// <summary>When a <see cref="StoryReaction"/> is eligible. A trigger is a hook, not an outcome.</summary>
public enum StoryTrigger
{
    Unknown = 0,
    EnterNode = 1,
    LeaveNode = 2,
    ChoiceTaken = 3,
    Advanced = 4,
    VariableChanged = 5,
    StoryStarted = 6
}
