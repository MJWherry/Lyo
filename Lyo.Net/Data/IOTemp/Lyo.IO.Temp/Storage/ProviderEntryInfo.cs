namespace Lyo.IO.Temp.Storage;

/// <summary>Metadata about a single file or directory entry returned by <see cref="IIOTempStorageProvider.EnumerateEntries" />.</summary>
public sealed record ProviderEntryInfo(string FullPath, bool IsDirectory, long Length, DateTimeOffset CreationTimeUtc) { }