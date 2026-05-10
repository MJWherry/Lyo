using Lyo.EntityReference.Models;

namespace Lyo.Tag;

/// <summary>Represents a tag attached to an entity (canonical entity-ref row + tag fields).</summary>
public sealed class TagRecord : EntityRefRow
{
    /// <summary>Gets or sets the tag display value (e.g. "urgent", "follow-up").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the tag type (e.g. "tag", "category"). Defaults to "tag".</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>Gets or sets an optional URL-friendly slug for this tag assignment.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets the entity reference for what is tagged.</summary>
    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who applied the tag.</summary>
    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
