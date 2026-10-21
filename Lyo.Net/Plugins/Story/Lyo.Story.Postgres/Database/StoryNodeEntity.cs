using Lyo.Story.Enums;

namespace Lyo.Story.Postgres.Database;

public sealed class StoryNodeEntity
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public Guid? SceneId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Title { get; set; }

    public bool IsEnding { get; set; }

    public StoryPresentationKind PresentationKind { get; set; }

    public string? Body { get; set; }

    public string? ContentType { get; set; }

    public string? MediaRef { get; set; }

    public StoryEntity Story { get; set; } = null!;

    public StorySceneEntity? Scene { get; set; }

    public ICollection<StoryChoiceEntity> Choices { get; set; } = [];
}