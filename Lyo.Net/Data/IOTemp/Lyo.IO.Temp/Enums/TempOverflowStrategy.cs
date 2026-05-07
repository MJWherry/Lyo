namespace Lyo.IO.Temp.Enums;

/// <summary>
/// Behaviour when <see cref="Lyo.IO.Temp.Models.IOTempSessionOptions.MaxFileCount" />, <see cref="Lyo.IO.Temp.Models.IOTempSessionOptions.MaxTotalSizeBytes" />, or per-file
/// limits would be exceeded.
/// </summary>
public enum TempOverflowStrategy
{
    /// <summary>Throw an exception rather than evicting existing data.</summary>
    ThrowException,

    /// <summary>Delete tracked files with the earliest creation time until within limits.</summary>
    DeleteOldest,

    /// <summary>Delete tracked files with the largest size until within limits.</summary>
    DeleteLargest
}