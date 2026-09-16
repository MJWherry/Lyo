namespace Lyo.Story.Sqlite.Database;

public sealed class StoryPlaythroughVariableEntity
{
    public Guid PlaythroughId { get; set; }
    public string VariableKey { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; }

    public StoryPlaythroughEntity Playthrough { get; set; } = null!;
}
