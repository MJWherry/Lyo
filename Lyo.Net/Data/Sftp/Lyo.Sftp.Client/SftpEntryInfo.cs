namespace Lyo.Sftp.Client;

/// <summary>Metadata for one remote SFTP directory entry.</summary>
/// <param name="FullPath">Absolute POSIX path of the entry.</param>
/// <param name="IsDirectory">True if the entry is a directory.</param>
/// <param name="Length">File size in bytes. Zero for directories.</param>
/// <param name="LastWriteTimeUtc">Last write time in UTC when the server reports it.</param>
public sealed record SftpEntryInfo(string FullPath, bool IsDirectory, long Length, DateTimeOffset LastWriteTimeUtc);