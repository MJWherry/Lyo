using Lyo.Story.Enums;

namespace Lyo.Story.Sqlite.Database;

public sealed class StoryEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? GameKey { get; set; }
    public string? Description { get; set; }
    public string? Language { get; set; }
    public StoryStatus Status { get; set; }
    public Guid? StartNodeId { get; set; }
    public string? CoverImageRef { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }

    public StoryNodeEntity? StartNode { get; set; }
    public ICollection<StorySceneEntity> Scenes { get; set; } = [];
    public ICollection<StoryNodeEntity> Nodes { get; set; } = [];
    public ICollection<StoryVariableEntity> Variables { get; set; } = [];
    public ICollection<StoryActionEntity> Actions { get; set; } = [];
    public ICollection<StoryReactionEntity> Reactions { get; set; } = [];
    public ICollection<StoryPlaythroughEntity> Playthroughs { get; set; } = [];
}
