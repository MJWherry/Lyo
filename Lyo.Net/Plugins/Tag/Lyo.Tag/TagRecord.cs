using Lyo.EntityReference.Models;

namespace Lyo.Tag;

/// <summary>Model for a tag attached to an entity (canonical entity-ref row + tag fields).</summary>
public sealed class TagRecord : EntityRelationRow
{
    /// <summary>Tag display value (e.g. "urgent", "follow-up") for this record.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tag type (e.g. "tag", "category"). Defaults to "tag".</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>An optional URL-friendly slug for this tag assignment stored here.</summary>
    public string Slug { get; set; } = string.Empty;
}