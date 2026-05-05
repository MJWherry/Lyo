using System.Text;
using Lyo.Exceptions;

namespace Lyo.IO.Temp.Storage;

/// <summary>Default <see cref="IIOTempStorageProvider" /> implementation backed by <see cref="System.IO" />.</summary>
// ReSharper disable once InconsistentNaming
public sealed class FileSystemIOTempStorageProvider : IIOTempStorageProvider
{
    /// <summary>Initializes a provider rooted at <paramref name="rootPath" />.</summary>
    public FileSystemIOTempStorageProvider(string rootPath)
    {
        ArgumentHelpers.ThrowIfNull(rootPath);
        RootPath = rootPath;
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    /// <inheritdoc />
    public IEnumerable<ProviderEntryInfo> EnumerateEntries(string path)
    {
        var root = new DirectoryInfo(path);
        if (!root.Exists)
            yield break;

        foreach (var dir in root.EnumerateDirectories())
            yield return new(dir.FullName, true, 0, new(dir.CreationTimeUtc, TimeSpan.Zero));

        foreach (var file in root.EnumerateFiles())
            yield return new(file.FullName, false, file.Length, new(file.CreationTimeUtc, TimeSpan.Zero));
    }

    /// <inheritdoc />
    public void EnsureDirectoryAccessible(string path)
    {
        ExceptionThrower.ThrowIfDirectoryNotAccessible(path);
        var probePath = Path.Combine(path, $".rw-check-{Guid.NewGuid():N}.tmp");
        try {
            File.WriteAllText(probePath, "rw");
            ExceptionThrower.ThrowIfFileNotAccessible(probePath);
        }
        finally {
            if (File.Exists(probePath))
                File.Delete(probePath);
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public void TouchFile(string path)
    {
        using var _ = File.Create(path);
    }

    /// <inheritdoc />
    public void WriteAllBytes(string path, byte[] data) => File.WriteAllBytes(path, data);

    /// <inheritdoc />
    public void WriteAllText(string path, string text, Encoding encoding) => File.WriteAllText(path, text, encoding);

    /// <inheritdoc />
    public void AppendAllText(string path, string text, Encoding encoding) => File.AppendAllText(path, text, encoding);

    /// <inheritdoc />
    public void DeleteFile(string path) => File.Delete(path);

    /// <inheritdoc />
    public void MoveFile(string source, string dest)
    {
        try {
            File.Move(source, dest);
        }
        catch (IOException) {
            File.Copy(source, dest);
            File.Delete(source);
        }
    }

    /// <inheritdoc />
    public void CopyFile(string source, string dest) => File.Copy(source, dest);

    /// <inheritdoc />
    public Stream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);

    /// <inheritdoc />
    public Stream OpenCreate(string path) => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.None);

    /// <inheritdoc />
    public Stream OpenAppend(string path) => new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, 4096, FileOptions.None);

    /// <inheritdoc />
    public long GetFileLength(string path) => new FileInfo(path).Length;

    /// <inheritdoc />
    public DateTimeOffset GetFileCreationTimeUtc(string path) => new(new FileInfo(path).CreationTimeUtc, TimeSpan.Zero);

    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return File.WriteAllBytesAsync(path, data, ct);
#else
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                File.WriteAllBytes(path, data);
            }, ct);
#endif
    }

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return File.WriteAllTextAsync(path, text, encoding, ct);
#else
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                File.WriteAllText(path, text, encoding);
            }, ct);
#endif
    }

    /// <inheritdoc />
    public Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return File.AppendAllTextAsync(path, text, encoding, ct);
#else
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                File.AppendAllText(path, text, encoding);
            }, ct);
#endif
    }

    /// <inheritdoc />
    public async Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await source.CopyToAsync(dest, ct);
#else
        using var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await source.CopyToAsync(dest, 81920, ct).ConfigureAwait(false);
#endif
    }

    /// <inheritdoc />
    public async Task CopyFileAsync(string source, string dest, CancellationToken ct)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await src.CopyToAsync(dst, ct);
#else
        using var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await src.CopyToAsync(dst, 81920, ct).ConfigureAwait(false);
#endif
    }
}