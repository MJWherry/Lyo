namespace Lyo.IO.Temp.Models;

/// <summary>A point-in-time snapshot of a session's state: tracked files, directories, total bytes, and creation time.</summary>
public record TempSessionSnapshot(
    string SessionDirectory,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Directories,
    long TotalBytesUsed,
    DateTimeOffset CreatedAt
);
