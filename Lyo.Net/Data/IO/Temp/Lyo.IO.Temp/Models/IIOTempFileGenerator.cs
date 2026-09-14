using Lyo.Common.Metadata.Records;

namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
/// <summary>Builds random files and fake directory trees inside a session. Reached through <see cref="IIOTempSession.Generator" />.</summary>
public interface IIOTempFileGenerator
{
    /// <summary>Writes a file of exactly <paramref name="sizeBytes" /> random bytes.</summary>
    /// <param name="sizeBytes">File length in bytes.</param>
    /// <param name="name">Optional relative name; generated when null or whitespace.</param>
    string CreateRandomFile(long sizeBytes, string? name = null);

    /// <summary>Writes a random-byte file whose length comes from <see cref="FileSizeUnitInfo.ConvertToBytes" />.</summary>
    string CreateRandomFile(FileSizeUnitInfo unit, double amount, string? name = null);

    /// <summary>Writes a random-byte file asynchronously.</summary>
    Task<string> CreateRandomFileAsync(long sizeBytes, string? name = null, CancellationToken ct = default);

    /// <summary>Writes a random-byte file asynchronously after converting <paramref name="unit" /> and <paramref name="amount" />.</summary>
    Task<string> CreateRandomFileAsync(FileSizeUnitInfo unit, double amount, string? name = null, CancellationToken ct = default);

    /// <summary>Writes <paramref name="count" /> random-byte files of the same length, using generated names.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes);

    /// <summary>Writes <paramref name="count" /> random-byte files after converting size from a unit.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, FileSizeUnitInfo unit, double amount);

    /// <summary>Writes <paramref name="count" /> random-byte files. <paramref name="nameSelector" /> receives the 0-based index and returns each name.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes, Func<int, string> nameSelector);

    /// <summary>Writes <paramref name="count" /> random-byte files with generated names asynchronously.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, CancellationToken ct = default);

    /// <summary>Writes <paramref name="count" /> random-byte files asynchronously after converting size from a unit.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, FileSizeUnitInfo unit, double amount, CancellationToken ct = default);

    /// <summary>Writes <paramref name="count" /> random-byte files asynchronously. <paramref name="nameSelector" /> receives the 0-based index and returns each name.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, Func<int, string> nameSelector, CancellationToken ct = default);

    /// <summary>Writes a text file with <paramref name="lines" /> lines, each about <paramref name="charsPerLine" /> characters.</summary>
    string CreateTextFile(int lines, int charsPerLine, string? name = null);

    /// <summary>Writes a text file asynchronously.</summary>
    Task<string> CreateTextFileAsync(int lines, int charsPerLine, string? name = null, CancellationToken ct = default);

    /// <summary>Writes a CSV with a header plus <paramref name="rows" /> data rows and <paramref name="columns" /> columns each.</summary>
    string CreateCsvFile(int rows, int columns, string? name = null);

    /// <summary>Writes a CSV asynchronously.</summary>
    Task<string> CreateCsvFileAsync(int rows, int columns, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Writes JSON: nested objects with <paramref name="keysPerObject" /> keys per level and <paramref name="depth" /> levels. Leaves are random strings, numbers, or booleans.
    /// </summary>
    string CreateJsonFile(int depth, int keysPerObject, string? name = null);

    /// <summary>Writes nested JSON asynchronously.</summary>
    Task<string> CreateJsonFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default);

    /// <summary>Writes a <c>.zip</c> whose layout matches <paramref name="spec" />. Returns the zip file path.</summary>
    string CreateZipFile(TempDirectorySpec spec, string? name = null);

    /// <summary>Writes a zip from <paramref name="spec" /> asynchronously.</summary>
    Task<string> CreateZipFileAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Writes XML: nested elements with <paramref name="keysPerObject" /> children per node and <paramref name="depth" /> levels. Leaves are random integers.
    /// </summary>
    string CreateXmlFile(int depth, int keysPerObject, string? name = null);

    /// <summary>Writes nested XML asynchronously.</summary>
    Task<string> CreateXmlFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Unpacks a <c>.zip</c> into a new session directory and tracks every extracted file and folder. Per-file size limits from session options are checked against zip metadata
    /// before extract. Returns the extraction directory path.
    /// </summary>
    string ExtractZipFile(string zipPath, string? targetDirName = null);

    /// <summary>Unpacks a zip into the session asynchronously.</summary>
    Task<string> ExtractZipFileAsync(string zipPath, string? targetDirName = null, CancellationToken ct = default);

    /// <summary>Creates a directory filled according to <paramref name="spec" /> (files and nested folders).</summary>
    string SimulateDirectory(TempDirectorySpec spec, string? name = null);

    /// <summary>Creates a flat directory with <paramref name="fileCount" /> random files of <paramref name="fileSizeBytes" /> bytes each.</summary>
    string SimulateDirectory(int fileCount, long fileSizeBytes, string? name = null);

    /// <summary>Builds a directory tree from <paramref name="spec" /> asynchronously.</summary>
    Task<string> SimulateDirectoryAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a flat directory of random files asynchronously.</summary>
    Task<string> SimulateDirectoryAsync(int fileCount, long fileSizeBytes, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Shortcut for a multi-level tree. Each level gets <paramref name="filesPerDirectory" /> random files of <paramref name="fileSizeBytes" /> bytes and
    /// <paramref name="dirsPerLevel" /> child folders (default 2). Depth 0 is a single flat directory.
    /// </summary>
    string CreateDirectoryTree(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null);

    /// <summary>Builds a multi-level directory tree asynchronously.</summary>
    Task<string> CreateDirectoryTreeAsync(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null, CancellationToken ct = default);
}
