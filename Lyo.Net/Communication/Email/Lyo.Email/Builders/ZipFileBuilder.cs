using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Email.Builders;

/// <summary>Fluent helper that builds a ZIP attachment for an email.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ZipFileBuilder
{
    private readonly ZipArchive _archive;

    private readonly MemoryStream _zipStream = new();

    private bool _isOpen = true;

    private ZipFileBuilder() => _archive = new(_zipStream, ZipArchiveMode.Create, true);

    /// <summary>Adds a file from a byte array.</summary>
    /// <param name="fileName">Entry name inside the archive.</param>
    /// <param name="data">File bytes.</param>
    /// <returns>This builder for further calls.</returns>
    public ZipFileBuilder AddFile(string fileName, byte[] data)
    {
        EnsureOpen();
        var entry = _archive.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);
        return this;
    }

    /// <summary>Adds a file from a stream.</summary>
    /// <param name="fileName">Entry name inside the archive.</param>
    /// <param name="data">Stream of file bytes.</param>
    /// <returns>This builder for further calls.</returns>
    public ZipFileBuilder AddFile(string fileName, Stream data)
    {
        EnsureOpen();
        var entry = _archive.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        data.CopyTo(stream);
        return this;
    }

    /// <summary>Adds a text file.</summary>
    /// <param name="fileName">Entry name inside the archive.</param>
    /// <param name="textContent">Text to store.</param>
    /// <param name="encoding">Optional encoding; UTF-8 when omitted.</param>
    /// <returns>This builder for further calls.</returns>
    public ZipFileBuilder AddFile(string fileName, string textContent, Encoding? encoding = null)
    {
        EnsureOpen();
        encoding ??= Encoding.UTF8;
        var data = encoding.GetBytes(textContent);
        return AddFile(fileName, data);
    }

    /// <summary>Adds several files from a dictionary.</summary>
    /// <param name="files">Map of archive entry name to file bytes.</param>
    /// <returns>This builder for further calls.</returns>
    public ZipFileBuilder AddFiles(Dictionary<string, byte[]> files)
    {
        EnsureOpen();
        foreach (var file in files)
            AddFile(file.Key, file.Value);

        return this;
    }

    /// <summary>Adds several files from disk paths.</summary>
    /// <param name="filePaths">Paths to include.</param>
    /// <returns>This builder for further calls.</returns>
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

    /// <summary>Adds one file from a path, optionally renaming the entry.</summary>
    /// <param name="filePath">Path of the file to add.</param>
    /// <param name="entryName">Optional archive name; defaults to the file name.</param>
    /// <returns>This builder for further calls.</returns>
    public ZipFileBuilder AddFileFromPath(string filePath, string? entryName = null)
    {
        EnsureOpen();
        entryName ??= Path.GetFileName(filePath);
        var data = File.ReadAllBytes(filePath);
        return AddFile(entryName, data);
    }

    /// <summary>Adds every file under a directory, keeping relative paths.</summary>
    /// <param name="directoryPath">Directory to include.</param>
    /// <param name="entryPrefix">Optional prefix prepended to each entry name.</param>
    /// <returns>This builder for further calls.</returns>
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

    /// <summary>Relative path from a base directory to a file, using an API that works on the targeted frameworks.</summary>
    /// <param name="basePath">Base directory.</param>
    /// <param name="file">Target file.</param>
    /// <returns>Path of <paramref name="file" /> relative to <paramref name="basePath" />.</returns>
    private static string GetRelativePathCompat(string basePath, string file)
    {
#if NETSTANDARD2_0
        return GetRelativePathNetStandard(basePath, file);
#else
        return Path.GetRelativePath(basePath, file);
#endif
    }

    /// <summary>Records a compression level for later adds. Not applied yet because level is chosen per entry.</summary>
    /// <param name="level">Level to use.</param>
    /// <returns>This builder for further calls.</returns>
    public ZipFileBuilder SetCompressionLevel(CompressionLevel level)
        =>
            // Compression level is chosen when each entry is created
            // Applying this would mean storing the level and reading it in AddFile
            this;

    /// <summary>Finishes the archive and returns its bytes.</summary>
    /// <returns>ZIP file bytes.</returns>
    /// <remarks>Build() consumes the builder. Start a new instance to make another ZIP.</remarks>
    public byte[] Build()
    {
        if (!_isOpen)
            return _zipStream.ToArray();

        _archive.Dispose();
        _isOpen = false;
        return _zipStream.ToArray();
    }

    /// <summary>Finishes the archive and writes it to disk.</summary>
    /// <param name="outputPath">Destination path for the ZIP.</param>
    public void BuildToFile(string outputPath)
    {
        var data = Build();
        File.WriteAllBytes(outputPath, data);
    }

    /// <summary>Finishes the archive and returns it as a stream.</summary>
    /// <returns>A MemoryStream of the ZIP bytes.</returns>
    public Stream BuildToStream()
    {
        var data = Build();
        return new MemoryStream(data);
    }

    /// <summary>Starts a new ZipFileBuilder.</summary>
    /// <returns>A fresh builder.</returns>
    public static ZipFileBuilder New() => new();

    private void EnsureOpen()
    {
        if (!_isOpen)
            OperationHelpers.ThrowIf(true, "ZipFileBuilder has already been built. Create a new instance to build another zip file.");
    }

    /// <summary>Disposes the archive and its backing stream.</summary>
    public void Dispose()
    {
        _archive.Dispose();
        _zipStream.Dispose();
    }

    /// <summary>Diagnostic snapshot of the builder.</summary>
    /// <returns>Text covering open state and entry count.</returns>
    public override string ToString() => $"ZipFileBuilder: IsOpen={_isOpen}, Entries={_archive.Entries.Count}";

#if NETSTANDARD2_0
    // netstandard2.0 path
    private static string GetRelativePathNetStandard(string basePath, string targetPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetPath);
        var baseUri = new Uri(AppendSlash(basePath));
        var targetUri = new Uri(targetPath);
        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendSlash(string path) => !path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path + Path.DirectorySeparatorChar : path;
#endif
}