using Lyo.Story.Enums;

namespace Lyo.Story.Sqlite.Database;

public sealed class StoryPlaythroughEntity
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public string? PlayerRef { get; set; }

    public StoryPlaythroughStatus Status { get; set; }

    public Guid? CurrentNodeId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public StoryEntity Story { get; set; } = null!;

    public StoryNodeEntity? CurrentNode { get; set; }

    public ICollection<StoryPlaythroughVariableEntity> Variables { get; set; } = [];

    public ICollection<StoryPlaythroughEventEntity> Events { get; set; } = [];
}