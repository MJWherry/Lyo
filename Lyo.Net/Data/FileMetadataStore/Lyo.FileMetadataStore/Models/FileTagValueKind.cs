namespace Lyo.FileMetadataStore.Models;

/// <summary>How a stored tag string is parsed when a grouping row asks for a folder name.</summary>
public enum FileTagValueKind
{
    /// <summary>The stored text.</summary>
    String = 0,

    /// <summary>A <see cref="Guid" />.</summary>
    Guid = 1,

    /// <summary>A boolean.</summary>
    Boolean = 2,

    /// <summary>An integer (<c>int</c> or <c>long</c>).</summary>
    Integer = 3,

    /// <summary>A number (<c>decimal</c> or <c>double</c>).</summary>
    Number = 4,

    /// <summary>A UTC date and time. <c>datetimeoffset</c> is converted to UTC.</summary>
    DateTime = 5,

    /// <summary>A time of day.</summary>
    TimeOnly = 6,

    /// <summary>A duration.</summary>
    TimeSpan = 7
}