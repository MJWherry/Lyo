namespace Lyo.Tag;

/// <summary>One tag name and type, with how many active assignments use that pair.</summary>
public sealed class TagCount
{
    /// <summary>Tag display value, such as "urgent".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tag type that namespaces <see cref="Name" />. Default assignments use "tag".</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>Active assignment rows for this name and tag type. Soft-deleted rows are not included.</summary>
    public int Count { get; set; }
}
