using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage;

/// <summary>Deletes, moves, copies, or renames a raw physical object by key. Hosts that must not reference the physical file system should call this instead.</summary>
public static class FileStoragePhysicalMutate
{
    /// <summary>Deletes the object at <paramref name="physicalKey" />. Throws when the key is unsafe or the file is missing.</summary>
    public static async Task DeleteAsync(IFileStorageService fileStorage, string physicalKey, CancellationToken ct = default)
    {
        var (fs, path, _) = await FileStoragePhysicalPath.ResolveExistingAsync(fileStorage, physicalKey, ct).ConfigureAwait(false);
        await fs.DeleteFileAsync(path, ct).ConfigureAwait(false);
    }

    /// <summary>Moves the object to <c>{destinationPathPrefix}/{leaf}</c> (root when the prefix is empty). Returns the new key.</summary>
    public static Task<string> MoveToPrefixAsync(
        IFileStorageService fileStorage,
        string physicalKey,
        string? destinationPathPrefix,
        CancellationToken ct = default)
        => RelocateAsync(fileStorage, physicalKey, FileStoragePhysicalPath.JoinKey(destinationPathPrefix, FileStoragePhysicalPath.Leaf(FileHelpers.NormalizeAndValidatePathPrefix(physicalKey))), copy: false, ct);

    /// <summary>Copies the object to <c>{destinationPathPrefix}/{leaf}</c>. Returns the new key.</summary>
    public static Task<string> CopyToPrefixAsync(
        IFileStorageService fileStorage,
        string physicalKey,
        string? destinationPathPrefix,
        CancellationToken ct = default)
        => RelocateAsync(fileStorage, physicalKey, FileStoragePhysicalPath.JoinKey(destinationPathPrefix, FileStoragePhysicalPath.Leaf(FileHelpers.NormalizeAndValidatePathPrefix(physicalKey))), copy: true, ct);

    /// <summary>Renames the last segment of <paramref name="physicalKey" />. Returns the new key.</summary>
    public static Task<string> RenameAsync(
        IFileStorageService fileStorage,
        string physicalKey,
        string newFileName,
        CancellationToken ct = default)
    {
        var sourceKey = FileHelpers.NormalizeAndValidatePathPrefix(physicalKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKey);
        var destKey = FileStoragePhysicalPath.JoinKey(FileStoragePhysicalPath.ParentKey(sourceKey), newFileName);
        return RelocateAsync(fileStorage, sourceKey, destKey, copy: false, ct);
    }

    private static async Task<string> RelocateAsync(
        IFileStorageService fileStorage,
        string physicalKey,
        string destKey,
        bool copy,
        CancellationToken ct)
    {
        var (fs, sourcePath, sourceKey) = await FileStoragePhysicalPath.ResolveExistingAsync(fileStorage, physicalKey, ct).ConfigureAwait(false);
        if (string.Equals(sourceKey, destKey, StringComparison.Ordinal))
            throw new ArgumentException("Source and destination physical keys are the same.");

        var destPath = FileStoragePhysicalPath.ToFullPath(fs, destKey);
        if (copy)
            await fs.CopyFileAsync(sourcePath, destPath, ct: ct).ConfigureAwait(false);
        else
            await fs.MoveAsync(sourcePath, destPath, ct: ct).ConfigureAwait(false);

        return destKey;
    }
}
