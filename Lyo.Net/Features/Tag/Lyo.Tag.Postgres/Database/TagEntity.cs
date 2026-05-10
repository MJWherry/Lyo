using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Tag.Postgres.Database;

/// <summary>Entity for storing tags in PostgreSQL.</summary>
public sealed class TagEntity : EntityRefEntityBase
{
    /// <summary>Tag display value (e.g. "urgent").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tag type discriminator (e.g. "tag", "category").</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>Optional URL-friendly slug for this assignment.</summary>
    public string Slug { get; set; } = string.Empty;
}
