namespace Lyo.IO.Temp.Models;

/// <summary>Session state at one instant: tracked files, directories, byte total, and creation time.</summary>
/// <param name="SessionDirectory">Session root path when the snapshot was taken.</param>
/// <param name="Files">Copy of tracked file paths.</param>
/// <param name="Directories">Copy of tracked directory paths.</param>
/// <param name="TotalBytesUsed">Byte total when the snapshot was taken.</param>
/// <param name="CreatedAt">When the session was created (UTC).</param>
public record TempSessionSnapshot(string SessionDirectory, IReadOnlyList<string> Files, IReadOnlyList<string> Directories, long TotalBytesUsed, DateTimeOffset CreatedAt);