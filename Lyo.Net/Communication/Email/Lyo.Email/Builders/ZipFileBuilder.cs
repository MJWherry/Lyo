using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Email.Builders;

/// <summary>Builder for creating ZIP file attachments for emails.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ZipFileBuilder
{
    private readonly ZipArchive _archive;

    private readonly MemoryStream _zipStream = new();

    private bool _isOpen = true;

    private ZipFileBuilder() => _archive = new(_zipStream, ZipArchiveMode.Create, true);

    /// <summary>Adds a file to the ZIP archive from byte array data.</summary>
    /// <param name="fileName">The name of the file within the ZIP archive.</param>
    /// <param name="data">The file data as a byte array.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddFile(string fileName, byte[] data)
    {
        EnsureOpen();
        var entry = _archive.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);
        return this;
    }

    /// <summary>Adds a file to the ZIP archive from a stream.</summary>
    /// <param name="fileName">The name of the file within the ZIP archive.</param>
    /// <param name="data">The file data as a stream.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddFile(string fileName, Stream data)
    {
        EnsureOpen();
        var entry = _archive.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        data.CopyTo(stream);
        return this;
    }

    /// <summary>Adds a text file to the ZIP archive.</summary>
    /// <param name="fileName">The name of the file within the ZIP archive.</param>
    /// <param name="textContent">The text content to add.</param>
    /// <param name="encoding">Optional encoding. Defaults to UTF-8 if not provided.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddFile(string fileName, string textContent, Encoding? encoding = null)
    {
        EnsureOpen();
        encoding ??= Encoding.UTF8;
        var data = encoding.GetBytes(textContent);
        return AddFile(fileName, data);
    }

    /// <summary>Adds multiple files to the ZIP archive from a dictionary.</summary>
    /// <param name="files">Dictionary where key is the file name within the ZIP and value is the file data.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddFiles(Dictionary<string, byte[]> files)
    {
        EnsureOpen();
        foreach (var file in files)
            AddFile(file.Key, file.Value);

        return this;
    }

    /// <summary>Adds multiple files to the ZIP archive from file paths.</summary>
    /// <param name="filePaths">Array of file paths to include in the ZIP.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddFiles(params string[] filePaths)
    {
        EnsureOpen();
        foreach (var path in filePaths) {
            var fileName = Path.GetFileName(path);
            var data = File.ReadAllBytes(path);
            AddFile(fileName, data);
        }

        return this;
    }

    /// <summary>Adds a file to the ZIP archive from a file path with optional custom entry name.</summary>
    /// <param name="filePath">The path to the file to add.</param>
    /// <param name="entryName">Optional custom name for the file within the ZIP. If not provided, the file name is used.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddFileFromPath(string filePath, string? entryName = null)
    {
        EnsureOpen();
        entryName ??= Path.GetFileName(filePath);
        var data = File.ReadAllBytes(filePath);
        return AddFile(entryName, data);
    }

    /// <summary>Adds all files from a directory to the ZIP archive, preserving directory structure.</summary>
    /// <param name="directoryPath">The path to the directory to add.</param>
    /// <param name="entryPrefix">Optional prefix to add to all entry names.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder AddDirectory(string directoryPath, string entryPrefix = "")
    {
        EnsureOpen();
        foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)) {
            var relativePath = GetRelativePathCompat(directoryPath, file);
            var entryName = string.IsNullOrEmpty(entryPrefix) ? relativePath : Path.Combine(entryPrefix, relativePath);
            var data = File.ReadAllBytes(file);
            AddFile(entryName.Replace("\\", "/"), data);
        }

        return this;
    }

    private static string GetRelativePathCompat(string basePath, string file)
    {
#if NETSTANDARD2_0
        return GetRelativePathNetStandard(basePath, file);
#else
        return Path.GetRelativePath(basePath, file);
#endif
    }

    /// <summary>Sets the compression level for subsequent file additions. Note: Currently not implemented as compression level is set per entry.</summary>
    /// <param name="level">The compression level to use.</param>
    /// <returns>The ZipFileBuilder instance for method chaining.</returns>
    public ZipFileBuilder SetCompressionLevel(CompressionLevel level)
        =>
            // Note: Compression level is set per entry when created
            // This would require storing the level and using it in AddFile methods
            this;

    /// <summary>Builds the ZIP archive and returns it as a byte array.</summary>
    /// <returns>The ZIP file data as a byte array.</returns>
    /// <remarks>After calling Build(), the builder cannot be used again. Create a new instance for another ZIP file.</remarks>
    public byte[] Build()
    {
        if (!_isOpen)
            return _zipStream.ToArray();

        _archive.Dispose();
        _isOpen = false;
        return _zipStream.ToArray();
    }

    /// <summary>Builds the ZIP archive and writes it to a file.</summary>
    /// <param name="outputPath">The path where the ZIP file should be written.</param>
    public void BuildToFile(string outputPath)
    {
        var data = Build();
        File.WriteAllBytes(outputPath, data);
    }

    /// <summary>Builds the ZIP archive and returns it as a stream.</summary>
    /// <returns>A MemoryStream containing the ZIP file data.</returns>
    public Stream BuildToStream()
    {
        var data = Build();
        return new MemoryStream(data);
    }

    /// <summary>Creates a new ZipFileBuilder instance.</summary>
    /// <returns>A new ZipFileBuilder instance.</returns>
    public static ZipFileBuilder New() => new();

    private void EnsureOpen()
    {
        if (!_isOpen)
            OperationHelpers.ThrowIf(true, "ZipFileBuilder has already been built. Create a new instance to build another zip file.");
    }

    public void Dispose()
    {
        _archive.Dispose();
        _zipStream.Dispose();
    }

    public override string ToString() => $"ZipFileBuilder: IsOpen={_isOpen}, Entries={_archive.Entries.Count}";

#if NETSTANDARD2_0
    // Fallback for netstandard2.0
    private static string GetRelativePathNetStandard(string basePath, string targetPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(basePath, nameof(basePath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetPath, nameof(targetPath));
        var baseUri = new Uri(AppendSlash(basePath));
        var targetUri = new Uri(targetPath);
        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendSlash(string path) => !path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path + Path.DirectorySeparatorChar : path;
#endif
}