namespace Lyo.Story.Enums;

/// <summary>One step of a response list on a choice, action, or reaction.</summary>
public enum StoryEffectKind
{
    Unknown = 0,
    Set = 1,
    Unset = 2,
    Increment = 3,
    GotoNode = 4,
    EndPlaythrough = 5,
    InvokeAction = 6
}