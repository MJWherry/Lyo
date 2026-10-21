namespace Lyo.Story;

/// <summary>Raised when a story graph, move, or playthrough is invalid.</summary>
public sealed class StoryException(string message)
    : Exception(message);