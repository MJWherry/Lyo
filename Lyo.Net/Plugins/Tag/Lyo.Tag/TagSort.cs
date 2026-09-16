namespace Lyo.Tag;

/// <summary>Field used to order <see cref="ITagStore.GetTopTagsAsync" /> results.</summary>
public enum TagSort
{
    /// <summary>Order by how many active assignments use the tag.</summary>
    Count = 0,

    /// <summary>Order by the tag name.</summary>
    Name = 1
}
