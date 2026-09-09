using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Tag.Postgres.Database;

/// <summary>Postgres row that holds tags in PostgreSQL.</summary>
public sealed class TagEntity : EntityRelationEntityBase
{
    /// <summary>Visible tag value (e.g. "urgent").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tag-type discriminator (e.g. "tag", "category").</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>URL-friendly slug for this assignment. May be omitted.</summary>
    public string Slug { get; set; } = string.Empty;
}