namespace Lyo.FileMetadataStore.Models;

/// <summary>How folder names of a tag type are ordered.</summary>
public enum FileTagSort
{
    /// <summary>Ordinal, ignore case.</summary>
    Ordinal = 0,

    /// <summary>Calendar or clock order for a date-time or time of day.</summary>
    Chronological = 1,

    /// <summary>Numeric order, so 2 comes before 10.</summary>
    Numeric = 2,

    /// <summary>Duration order.</summary>
    Duration = 3
}