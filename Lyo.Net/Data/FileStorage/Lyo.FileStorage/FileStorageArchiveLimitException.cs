namespace Lyo.FileStorage;

/// <summary>Raised when an archive request exceeds <see cref="Models.FileStorageArchiveOptions" /> file-count or uncompressed-size caps.</summary>
public sealed class FileStorageArchiveLimitException(string message) : Exception(message);
