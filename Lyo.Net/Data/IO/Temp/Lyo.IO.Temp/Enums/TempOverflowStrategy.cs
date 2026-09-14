namespace Lyo.IO.Temp.Enums;

/// <summary>
/// What happens when <see cref="Lyo.IO.Temp.Models.IOTempSessionOptions.MaxFileCount" />, <see cref="Lyo.IO.Temp.Models.IOTempSessionOptions.MaxTotalSizeBytes" />, or a per-file
/// cap would be crossed.
/// </summary>
public enum TempOverflowStrategy
{
    /// <summary>Fail instead of deleting existing files.</summary>
    ThrowException,

    /// <summary>Drop tracked files with the oldest creation time until the session is back under the cap.</summary>
    DeleteOldest,

    /// <summary>Drop tracked files with the largest size until the session is back under the cap.</summary>
    DeleteLargest
}