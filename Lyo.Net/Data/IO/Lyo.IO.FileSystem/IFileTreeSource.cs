using Lyo.Exceptions;

namespace Lyo.IO.FileSystem;

/// <summary>List-only seam for a folder tree. HTTP clients implement this without a full <see cref="IFileSystem" />.</summary>
public interface IFileTreeSource
{
    /// <summary>Immediate child files and directories of <paramref name="path" />. Empty when the directory is missing.</summary>
    Task<IReadOnlyList<FileSystemEntry>> ListChildrenAsync(string path, CancellationToken ct = default);
}

/// <summary>Adapts <see cref="IFileSystem.ListDirectoryAsync" /> onto <see cref="IFileTreeSource" />.</summary>
public sealed class FileSystemTreeSource : IFileTreeSource
{
    private readonly IFileSystem _fileSystem;

    /// <summary>Wraps <paramref name="fileSystem" />.</summary>
    public FileSystemTreeSource(IFileSystem fileSystem)
    {
        ArgumentHelpers.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemEntry>> ListChildrenAsync(string path, CancellationToken ct = default)
        => _fileSystem.ListDirectoryAsync(path, ct);
}
