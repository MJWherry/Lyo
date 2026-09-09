using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.FFmpeg;

internal static class FFmpegTempHelper
{
    public static string CreateTempFilePath(string extension = ".tmp") => TempSpool.CreatePath("ffmpeg", extension);

    public static async Task<string> WriteStreamToTempFileAsync(Stream stream, string? extension, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var path = CreateTempFilePath(extension ?? ".tmp");
        await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            await stream.CopyToAsync(fs, ct).ConfigureAwait(false);

        return path;
    }

    public static async Task<string> WriteBytesToTempFileAsync(byte[] bytes, string? extension, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        var path = CreateTempFilePath(extension ?? ".tmp");
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        return path;
    }

    public static Task<byte[]> ReadTempFileToBytesAsync(string path, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllBytesAsync(path, ct);
    }

    public static async Task CopyTempFileToStreamAsync(string path, Stream outputStream, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await fs.CopyToAsync(outputStream, ct).ConfigureAwait(false);
    }

    public static void TryDelete(string path) => TempSpool.TryDelete(path);
}
