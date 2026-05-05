namespace Lyo.IO.Temp.Storage;

/// <summary>Metadata about a single file or directory entry returned by <see cref="IIOTempStorageProvider.EnumerateEntries" />.</summary>
/// <param name="FullPath">Absolute or provider-normalized path.</param>
/// <param name="IsDirectory">True if the entry is a directory.</param>
/// <param name="Length">File length in bytes; zero for directories unless the implementation sets otherwise.</param>
/// <param name="CreationTimeUtc">Creation time in UTC.</param>
public sealed record ProviderEntryInfo(string FullPath, bool IsDirectory, long Length, DateTimeOffset CreationTimeUtc) { }