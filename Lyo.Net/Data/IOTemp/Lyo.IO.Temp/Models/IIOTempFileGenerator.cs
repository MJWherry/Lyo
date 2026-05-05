using Lyo.Common.Records;

namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
/// <summary>Generates random files and simulated directory trees within a session. Accessible via <see cref="IIOTempSession.Generator" />.</summary>
public interface IIOTempFileGenerator
{
    /// <summary>Creates a file filled with random bytes of the given size.</summary>
    /// <param name="sizeBytes">Exact size of the file in bytes.</param>
    /// <param name="name">Optional relative file name; generated if null or whitespace.</param>
    string CreateRandomFile(long sizeBytes, string? name = null);

    /// <summary>Creates a random-bytes file using <see cref="FileSizeUnitInfo.ConvertToBytes" />.</summary>
    string CreateRandomFile(FileSizeUnitInfo unit, double amount, string? name = null);

    /// <summary>Asynchronously creates a random-bytes file.</summary>
    Task<string> CreateRandomFileAsync(long sizeBytes, string? name = null, CancellationToken ct = default);

    /// <summary>Asynchronously creates a random-bytes file using unit conversion.</summary>
    Task<string> CreateRandomFileAsync(FileSizeUnitInfo unit, double amount, string? name = null, CancellationToken ct = default);

    /// <summary>Creates <paramref name="count" /> random-bytes files of the same size with generated names.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes);

    /// <summary>Creates <paramref name="count" /> random-bytes files using unit conversion for size.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, FileSizeUnitInfo unit, double amount);

    /// <summary>Creates <paramref name="count" /> random-bytes files. Each file's name is produced by <paramref name="nameSelector" /> given its 0-based index.</summary>
    IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes, Func<int, string> nameSelector);

    /// <summary>Asynchronously creates <paramref name="count" /> random-bytes files with generated names.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, CancellationToken ct = default);

    /// <summary>Asynchronously creates <paramref name="count" /> random-bytes files using unit conversion.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, FileSizeUnitInfo unit, double amount, CancellationToken ct = default);

    /// <summary>Creates <paramref name="count" /> random-bytes files asynchronously. Each file's name is produced by <paramref name="nameSelector" /> given its 0-based index.</summary>
    Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, Func<int, string> nameSelector, CancellationToken ct = default);

    /// <summary>Creates a plain-text file with <paramref name="lines" /> lines, each approximately <paramref name="charsPerLine" /> characters wide.</summary>
    string CreateTextFile(int lines, int charsPerLine, string? name = null);

    /// <summary>Asynchronously creates a plain-text file.</summary>
    Task<string> CreateTextFileAsync(int lines, int charsPerLine, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a CSV file with a header row and <paramref name="rows" /> data rows, each with <paramref name="columns" /> columns.</summary>
    string CreateCsvFile(int rows, int columns, string? name = null);

    /// <summary>Asynchronously creates a CSV file.</summary>
    Task<string> CreateCsvFileAsync(int rows, int columns, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a JSON file containing a nested object with <paramref name="keysPerObject" /> keys at each level and <paramref name="depth" /> levels of nesting. Leaf values are
    /// random strings, numbers, and booleans.
    /// </summary>
    string CreateJsonFile(int depth, int keysPerObject, string? name = null);

    /// <summary>Asynchronously creates a nested JSON file.</summary>
    Task<string> CreateJsonFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default);

    /// <summary>Creates a <c>.zip</c> archive whose internal structure follows <paramref name="spec" />. Returns the path of the created zip file.</summary>
    string CreateZipFile(TempDirectorySpec spec, string? name = null);

    /// <summary>Asynchronously creates a zip archive from <paramref name="spec" />.</summary>
    Task<string> CreateZipFileAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Creates an XML file containing a nested element tree with <paramref name="keysPerObject" /> child elements per node and <paramref name="depth" /> levels of nesting. Leaf
    /// values are random integers.
    /// </summary>
    string CreateXmlFile(int depth, int keysPerObject, string? name = null);

    /// <summary>Asynchronously creates a nested XML file.</summary>
    Task<string> CreateXmlFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Extracts a <c>.zip</c> archive into a new directory within the session and tracks all extracted files and directories. Per-file size limits from session options are
    /// validated against zip metadata before extraction. Returns the path of the created extraction directory.
    /// </summary>
    string ExtractZipFile(string zipPath, string? targetDirName = null);

    /// <summary>Asynchronously extracts a zip archive into the session.</summary>
    Task<string> ExtractZipFileAsync(string zipPath, string? targetDirName = null, CancellationToken ct = default);

    /// <summary>Creates a directory populated according to <paramref name="spec" /> (files + nested subdirectories).</summary>
    string SimulateDirectory(TempDirectorySpec spec, string? name = null);

    /// <summary>Creates a flat directory with <paramref name="fileCount" /> random files of <paramref name="fileSizeBytes" /> bytes each.</summary>
    string SimulateDirectory(int fileCount, long fileSizeBytes, string? name = null);

    /// <summary>Asynchronously simulates a directory tree from <paramref name="spec" />.</summary>
    Task<string> SimulateDirectoryAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default);

    /// <summary>Asynchronously creates a flat directory of random files.</summary>
    Task<string> SimulateDirectoryAsync(int fileCount, long fileSizeBytes, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Convenience shorthand for creating a multi-level directory tree. Each level has <paramref name="filesPerDirectory" /> random files of <paramref name="fileSizeBytes" />
    /// bytes, and <paramref name="dirsPerLevel" /> subdirectories (default 2). Depth 0 creates a single flat directory.
    /// </summary>
    string CreateDirectoryTree(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null);

    /// <summary>Asynchronously creates a multi-level directory tree.</summary>
    Task<string> CreateDirectoryTreeAsync(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null, CancellationToken ct = default);
}
