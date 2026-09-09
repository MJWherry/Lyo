namespace Lyo.IO.Temp.Storage;

/// <summary>File or directory metadata from <see cref="IIOTempStorageProvider.EnumerateEntries" />.</summary>
/// <param name="FullPath">Absolute or provider-normalized path.</param>
/// <param name="IsDirectory">True when the entry is a directory.</param>
/// <param name="Length">File length in bytes; zero for directories unless the implementation sets something else.</param>
/// <param name="CreationTimeUtc">Creation time in UTC.</param>
public sealed record ProviderEntryInfo(string FullPath, bool IsDirectory, long Length, DateTimeOffset CreationTimeUtc) { }