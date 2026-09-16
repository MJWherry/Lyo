namespace Lyo.FileMetadataStore.Models;

/// <summary>A prior stored key for a file. Encryption key versions are not file history.</summary>
public sealed record FileRevision(Guid Id, Guid ParentId, string StoredKey, DateTime CreatedAt);
