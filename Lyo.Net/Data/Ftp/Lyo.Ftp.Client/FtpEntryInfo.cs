namespace Lyo.Ftp.Client;

/// <summary>Metadata for a single remote FTP directory entry.</summary>
/// <param name="FullPath">Absolute POSIX path of the entry.</param>
/// <param name="IsDirectory">True when the entry is a directory.</param>
/// <param name="Length">File length in bytes; zero for directories.</param>
/// <param name="LastWriteTimeUtc">Last write time in UTC when available.</param>
public sealed record FtpEntryInfo(string FullPath, bool IsDirectory, long Length, DateTimeOffset LastWriteTimeUtc);