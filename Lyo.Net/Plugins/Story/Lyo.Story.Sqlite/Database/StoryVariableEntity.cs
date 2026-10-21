using Lyo.Story.Enums;

namespace Lyo.Story.Sqlite.Database;

public sealed class StoryVariableEntity
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Name { get; set; }

    public StoryVariableType Type { get; set; }

    public string? DefaultValue { get; set; }

    public string? Description { get; set; }

    public StoryEntity Story { get; set; } = null!;
}