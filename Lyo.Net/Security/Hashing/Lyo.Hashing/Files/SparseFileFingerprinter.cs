using Lyo.Common.Records;
using Lyo.Exceptions;

namespace Lyo.Hashing.Files;

/// <summary>
/// Lightweight content sampling + MD5 for file change detection (e.g. directory snapshots). Same algorithm as historic <c>Lyo.FileSystemWatcher.Utilities.Fingerprint</c> /
/// metadata-only helper.
/// </summary>
public static class SparseFileFingerprinter
{
    public const int DefaultSampleSize = 128;

    public const int DefaultVeryLargeSampleSize = 64;

    /// <summary>100 MiB (<see cref="FileSizeUnitInfo.Megabyte" /> × 100) — extra sample points for large files.</summary>
    public static readonly long DefaultLargeFileThreshold = FileSizeUnitInfo.Megabyte.ConvertToBytes(100);

    /// <summary>1 GiB (<see cref="FileSizeUnitInfo.Gigabyte" />) — sparse path with mtime + smaller content sample.</summary>
    public static readonly long DefaultVeryLargeThreshold = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1);

    /// <summary>MD5 of (size + samples [+ mod time for very large]). Returns <see langword="null" /> if <paramref name="path" /> does not exist.</summary>
    public static Task<byte[]?> FingerprintAsync(string path, long fileSize, CancellationToken ct = default, FileFingerprintOptions? options = null)
    {
        ExceptionThrower.ThrowIfDirectoryNotAccessible(path);
        if (!File.Exists(path))
            return Task.FromResult<byte[]?>(null);

        var o = options ?? FileFingerprintOptions.Default;
        if (fileSize > o.VeryLargeThreshold) {
            var fileInfo = new FileInfo(path);
            var combined = new byte[8 + o.VeryLargeSampleSize + 8];
            var sizeBytes = BitConverter.GetBytes(fileSize);
            var modTimeBytes = BitConverter.GetBytes(fileInfo.LastWriteTimeUtc.Ticks);
            Array.Copy(sizeBytes, 0, combined, 0, 8);
            Array.Copy(modTimeBytes, 0, combined, combined.Length - 8, 8);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, o.VeryLargeSampleSize * 2)) {
                var firstRead = stream.Read(combined, 8, o.VeryLargeSampleSize);
                if (firstRead < o.VeryLargeSampleSize)
                    Array.Clear(combined, 8 + firstRead, o.VeryLargeSampleSize - firstRead);
            }

            return Task.FromResult<byte[]?>(Hasher.ComputeMd5(combined));
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, o.SampleSize * 2);
        ct.ThrowIfCancellationRequested();
        var firstBytes = new byte[o.SampleSize];
        var firstBytesRead = fs.Read(firstBytes, 0, o.SampleSize);
        var middleBytesRead = 0;
        var lastBytesRead = 0;
        byte[]? middleBytes = null;
        byte[]? lastBytes = null;
        if (fileSize > o.LargeFileThreshold) {
            if (fileSize > o.SampleSize * 2) {
                middleBytes = new byte[o.SampleSize];
                fs.Seek(fileSize / 2, SeekOrigin.Begin);
                middleBytesRead = fs.Read(middleBytes, 0, o.SampleSize);
            }

            if (fileSize > o.SampleSize) {
                lastBytes = new byte[o.SampleSize];
                fs.Seek(-o.SampleSize, SeekOrigin.End);
                lastBytesRead = fs.Read(lastBytes, 0, o.SampleSize);
            }
        }
        else if (fileSize > o.SampleSize) {
            lastBytes = new byte[o.SampleSize];
            fs.Seek(-o.SampleSize, SeekOrigin.End);
            lastBytesRead = fs.Read(lastBytes, 0, o.SampleSize);
        }

        var capacity = 8 + firstBytesRead + middleBytesRead + lastBytesRead;
        var combined2 = new byte[capacity];
        var offset = 0;
        var sizeBytes2 = BitConverter.GetBytes(fileSize);
        Array.Copy(sizeBytes2, 0, combined2, offset, 8);
        offset += 8;
        Array.Copy(firstBytes, 0, combined2, offset, firstBytesRead);
        offset += firstBytesRead;
        if (middleBytes != null && middleBytesRead > 0) {
            Array.Copy(middleBytes, 0, combined2, offset, middleBytesRead);
            offset += middleBytesRead;
        }

        if (lastBytes != null && lastBytesRead > 0)
            Array.Copy(lastBytes, 0, combined2, offset, lastBytesRead);

        return Task.FromResult<byte[]?>(Hasher.ComputeMd5(combined2));
    }

    /// <summary>MD5 of size + last-write time (no file I/O on content).</summary>
    public static string MetadataOnlyHex(long fileSize, DateTime lastWriteTimeUtc)
    {
        var combined = new byte[16];
        BitConverter.GetBytes(fileSize).CopyTo(combined, 0);
        BitConverter.GetBytes(lastWriteTimeUtc.Ticks).CopyTo(combined, 8);
        return HexEncoding.ToHexString(Hasher.ComputeMd5(combined));
    }
}