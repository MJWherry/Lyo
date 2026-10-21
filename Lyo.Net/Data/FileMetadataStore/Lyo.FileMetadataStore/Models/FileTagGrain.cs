namespace Lyo.FileMetadataStore.Models;

/// <summary>Which part of a parsed tag value becomes the folder name. The grouping row's kind decides which grains are legal.</summary>
public enum FileTagGrain
{
    /// <summary>The whole parsed value. Legal for string, guid, bool, integer, and number.</summary>
    Value = 0,

    /// <summary>Calendar year.</summary>
    Year = 1,

    /// <summary>Calendar month.</summary>
    Month = 2,

    /// <summary>Calendar day.</summary>
    Day = 3,

    /// <summary>Hour of a date-time or time of day.</summary>
    Hour = 4,

    /// <summary>Minute of a date-time or time of day.</summary>
    Minute = 5,

    /// <summary>Second of a date-time or time of day.</summary>
    Second = 6,

    /// <summary>Whole days of a duration.</summary>
    Days = 7,

    /// <summary>Hours of a duration. The remainder after days when a coarser key is already in the path.</summary>
    Hours = 8,

    /// <summary>Minutes of a duration. The remainder after hours when a coarser key is already in the path.</summary>
    Minutes = 9,

    /// <summary>Seconds of a duration. The remainder after minutes when a coarser key is already in the path.</summary>
    Seconds = 10
}