using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Abstractions;

/// <summary>Tag assignment and tag-grouped browsing on a file storage service. Object keys stay where they are.</summary>
public interface IFileStorageTags
{
    /// <summary>Adds a manual tag. Reserved types (<c>file-type</c>, <c>imported</c>) are rejected. A repeat of the same pair is a no-op.</summary>
    Task<FileTag> AddTagAsync(Guid fileId, string tagType, string value, string? slug = null, CancellationToken ct = default);

    /// <summary>Removes a manual tag. Returns false when it was not stored. Reserved types are rejected.</summary>
    Task<bool> RemoveTagAsync(Guid fileId, string tagType, string value, CancellationToken ct = default);

    /// <summary>Tags currently stored for a file, including automatic ones.</summary>
    Task<IReadOnlyList<FileTag>> GetTagsAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Built-in tag types, then manual types in use. The tree uses this list for group-by and sort.</summary>
    Task<IReadOnlyList<FileTagTypeInfo>> GetTagTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Writes missing <c>file-type</c> and <c>imported</c> tags for every stored file. An existing import time is left as it is. Pending direct uploads are skipped. Returns how
    /// many stored files were checked.
    /// </summary>
    Task<int> StampAutomaticTagsAsync(CancellationToken ct = default);

    /// <summary>One level of the virtual folder tree. <paramref name="path" /> is the slash-joined folder names already chosen.</summary>
    Task<(IReadOnlyList<FileStorageFolderEntry> Entries, bool Truncated)> ListTagGroupAsync(
        IReadOnlyList<FileTagGroupKey> groupBy,
        string? path,
        int maxKeys = 10_000,
        CancellationToken ct = default);
}