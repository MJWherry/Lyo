namespace Lyo.Ffmpeg;

internal static class FfmpegTempHelper
{
    public static string CreateTempFilePath(string extension = ".tmp") => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);

    public static async Task<string> WriteStreamToTempFileAsync(Stream stream, string? extension, CancellationToken ct)
    {
        var path = CreateTempFilePath(extension ?? ".tmp");
        await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            await stream.CopyToAsync(fs, ct).ConfigureAwait(false);

        return path;
    }

    public static async Task<string> WriteBytesToTempFileAsync(byte[] bytes, string? extension, CancellationToken ct)
    {
        var path = CreateTempFilePath(extension ?? ".tmp");
#if NETSTANDARD2_0
        await Task.Run(() => File.WriteAllBytes(path, bytes), ct).ConfigureAwait(false);
#else
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
#endif
        return path;
    }

    public static async Task<byte[]> ReadTempFileToBytesAsync(string path, CancellationToken ct)
    {
#if NETSTANDARD2_0
        return await Task.Run(() => File.ReadAllBytes(path), ct).ConfigureAwait(false);
#else
        return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
#endif
    }

    public static async Task CopyTempFileToStreamAsync(string path, Stream outputStream, CancellationToken ct)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await fs.CopyToAsync(outputStream, ct).ConfigureAwait(false);
    }

    public static void TryDelete(string path)
    {
        try {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch {
            // Best-effort cleanup
        }
    }
}