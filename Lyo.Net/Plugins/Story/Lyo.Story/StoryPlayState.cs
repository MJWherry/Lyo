namespace Lyo.Story;

/// <summary>Playthrough plus current node presentation and available moves.</summary>
public sealed class StoryPlayState
{
    public StoryPlaythrough Playthrough { get; set; } = new();

    public StoryNode? CurrentNode { get; set; }

    public IReadOnlyList<StoryMove> Moves { get; set; } = [];
}
