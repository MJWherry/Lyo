using Lyo.Common;

namespace Lyo.Tag;

/// <summary>Represents a tag attached to an entity. Uses EntityRef structure.</summary>
public sealed class TagRecord
{
    /// <summary>Gets or sets the unique identifier of the tag assignment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being tagged (e.g. "Docket", "Person").</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id being tagged.</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tag value (e.g. "urgent", "follow-up").</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of who applied the tag (e.g. "User"), or null if system-applied.</summary>
    public string? FromEntityType { get; set; }

    /// <summary>Gets or sets the entity id of who applied the tag, or null if system-applied.</summary>
    public string? FromEntityId { get; set; }

    /// <summary>Gets or sets when the tag was applied.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for what is tagged.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who applied the tag, or null if not tracked.</summary>
    public EntityRef? FromEntity => FromEntityType != null && FromEntityId != null ? new EntityRef(FromEntityType, FromEntityId) : null;
}