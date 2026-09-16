namespace Lyo.Story.Postgres.Database;

public sealed class StoryActionEntity
{
    public Guid Id { get; set; }
    public Guid StoryId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Name { get; set; }

    public StoryEntity Story { get; set; } = null!;
    public ICollection<StoryEffectEntity> Effects { get; set; } = [];
}
