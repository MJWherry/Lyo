namespace Lyo.IO.Temp.Models;

/// <summary>A point-in-time snapshot of a session's state: tracked files, directories, total bytes, and creation time.</summary>
/// <param name="SessionDirectory">Session root path at snapshot time.</param>
/// <param name="Files">Copy of tracked file paths.</param>
/// <param name="Directories">Copy of tracked directory paths.</param>
/// <param name="TotalBytesUsed">Byte total at snapshot time.</param>
/// <param name="CreatedAt">Session creation timestamp (UTC).</param>
public record TempSessionSnapshot(string SessionDirectory, IReadOnlyList<string> Files, IReadOnlyList<string> Directories, long TotalBytesUsed, DateTimeOffset CreatedAt);