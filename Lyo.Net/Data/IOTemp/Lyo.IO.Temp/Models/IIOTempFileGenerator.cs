using Lyo.Common.Records;

namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Generates random files and simulated directory trees within a session.
/// Accessible via <see cref="IIOTempSession.Generator"/>.
/// </summary>
public interface IIOTempFileGenerator
{

    string CreateRandomFile(long sizeBytes, string? name = null);

    string CreateRandomFile(FileSizeUnitInfo unit, double amount, string? name = null);

    Task<string> CreateRandomFileAsync(long sizeBytes, string? name = null, CancellationToken ct = default);

    Task<string> CreateRandomFileAsync(FileSizeUnitInfo unit, double amount, string? name = null, CancellationToken ct = default);

    IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes);

    IReadOnlyList<string> CreateRandomFiles(int count, FileSizeUnitInfo unit, double amount);

    /// <summary>Creates <paramref name="count"/> random-bytes files. Each file's name is produced by <paramref name="nameSelector"/> given its 0-based index.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes, Func<int, string> nameSelector);

    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, CancellationToken ct = default);

    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, FileSizeUnitInfo unit, double amount, CancellationToken ct = default);

    /// <summary>Creates <paramref name="count"/> random-bytes files asynchronously. Each file's name is produced by <paramref name="nameSelector"/> given its 0-based index.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, Func<int, string> nameSelector, CancellationToken ct = default);

    /// <summary>Creates a plain-text file with <paramref name="lines"/> lines, each approximately <paramref name="charsPerLine"/> characters wide.</summary>
    string CreateTextFile(int lines, int charsPerLine, string? name = null);

    Task<string> CreateTextFileAsync(int lines, int charsPerLine, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a CSV file with a header row and <paramref name="rows"/> data rows, each with <paramref name="columns"/> columns.</summary>
    string CreateCsvFile(int rows, int columns, string? name = null);

    Task<string> CreateCsvFileAsync(int rows, int columns, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a JSON file containing a nested object with <paramref name="keysPerObject"/> keys at each level
    /// and <paramref name="depth"/> levels of nesting. Leaf values are random strings, numbers, and booleans.
    /// </summary>
    string CreateJsonFile(int depth, int keysPerObject, string? name = null);

    Task<string> CreateJsonFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a <c>.zip</c> archive whose internal structure follows <paramref name="spec"/>. Returns the path of the created zip file.</summary>
    string CreateZipFile(TempDirectorySpec spec, string? name = null);

    Task<string> CreateZipFileAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Creates an XML file containing a nested element tree with <paramref name="keysPerObject"/> child elements per node
    /// and <paramref name="depth"/> levels of nesting. Leaf values are random integers.
    /// </summary>
    string CreateXmlFile(int depth, int keysPerObject, string? name = null);

    Task<string> CreateXmlFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Extracts a <c>.zip</c> archive into a new directory within the session and tracks all extracted files and directories.
    /// Per-file size limits from session options are validated against zip metadata before extraction.
    /// Returns the path of the created extraction directory.
    /// </summary>
    string ExtractZipFile(string zipPath, string? targetDirName = null);

    Task<string> ExtractZipFileAsync(string zipPath, string? targetDirName = null, CancellationToken ct = default);

    /// <summary>Creates a directory populated according to <paramref name="spec"/> (files + nested subdirectories).</summary>
    string SimulateDirectory(TempDirectorySpec spec, string? name = null);

    /// <summary>Creates a flat directory with <paramref name="fileCount"/> random files of <paramref name="fileSizeBytes"/> bytes each.</summary>
    string SimulateDirectory(int fileCount, long fileSizeBytes, string? name = null);

    Task<string> SimulateDirectoryAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default);

    Task<string> SimulateDirectoryAsync(int fileCount, long fileSizeBytes, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Convenience shorthand for creating a multi-level directory tree.
    /// Each level has <paramref name="filesPerDirectory"/> random files of <paramref name="fileSizeBytes"/> bytes,
    /// and <paramref name="dirsPerLevel"/> subdirectories (default 2). Depth 0 creates a single flat directory.
    /// </summary>
    string CreateDirectoryTree(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null);

    Task<string> CreateDirectoryTreeAsync(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null, CancellationToken ct = default);
}
