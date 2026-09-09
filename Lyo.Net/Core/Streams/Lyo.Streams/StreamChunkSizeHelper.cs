// ReSharper disable UnusedMember.Local

namespace Lyo.Streams;

/// <summary>Tunable chunk-size thresholds consumed by <see cref="StreamChunkSizeHelper" />.</summary>
public class StreamChunkSizeOptions
{
    /// <summary>Smallest chunk (default 64KB). Used for files under 1MB.</summary>
    public int MinChunkSize { get; set; } = 64 * 1024;

    /// <summary>Typical chunk (default 1MB). Used for medium files from 1MB–100MB.</summary>
    public int DefaultChunkSize { get; set; } = 1024 * 1024;

    /// <summary>Largest chunk (default 10MB). Used for files over 10GB.</summary>
    public int MaxChunkSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>Byte cutoff for "small" files (default 1MB). Smaller files use MinChunkSize.</summary>
    public long SmallFileThreshold { get; set; } = 1024 * 1024;

    /// <summary>Byte cutoff for "medium" files (default 100MB).</summary>
    public long MediumFileThreshold { get; set; } = 100 * 1024 * 1024;

    /// <summary>Byte cutoff for "large" files (default 1GB).</summary>
    public long LargeFileThreshold { get; set; } = 1024L * 1024 * 1024;

    /// <summary>Byte cutoff for "very large" files (default 10GB).</summary>
    public long VeryLargeFileThreshold { get; set; } = 10L * 1024 * 1024 * 1024;
}

/// <summary>Picks a chunk size for stream work from the stream or file length.</summary>
/// <remarks>Size heuristics: smaller chunks on small files to avoid over-allocation, larger chunks on big files to cut I/O overhead.</remarks>
public static class StreamChunkSizeHelper
{
    private const int DefaultChunkSize = 1024 * 1024; // 1MB

    private const int MinChunkSize = 64 * 1024; // 64KB

    private const int MaxChunkSize = 10 * 1024 * 1024; // 10MB

    /// <summary>Picks a chunk size from the stream length.</summary>
    /// <param name="stream">Stream whose length is inspected</param>
    /// <param name="defaultChunkSize">Fallback chunk size when length cannot be read</param>
    /// <param name="options">Custom thresholds. Null uses built-in defaults.</param>
    /// <returns>Chunk size in bytes</returns>
    public static int DetermineChunkSize(Stream? stream, int? defaultChunkSize = null, StreamChunkSizeOptions? options = null)
    {
        if (stream == null)
            return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;

        try {
            if (stream is { CanSeek: true, Length: > 0 })
                return DetermineChunkSize(stream.Length, options, defaultChunkSize);
        }
        catch {
            // Length unavailable; fall back to the default chunk
        }

        return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;
    }

    /// <summary>Picks a chunk size from a file's length.</summary>
    /// <param name="filePath">Path of the file to measure</param>
    /// <param name="defaultChunkSize">Fallback chunk size when the file size cannot be read</param>
    /// <param name="options">Custom thresholds. Null uses built-in defaults.</param>
    /// <returns>Chunk size in bytes</returns>
    public static int DetermineChunkSize(string? filePath, int? defaultChunkSize = null, StreamChunkSizeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;

        try {
            if (File.Exists(filePath)) {
                var fileInfo = new FileInfo(filePath);
                return DetermineChunkSize(fileInfo.Length, options, defaultChunkSize);
            }
        }
        catch {
            // Size unavailable; fall back to the default chunk
        }

        return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;
    }

    /// <summary>Picks a chunk size from a known data length.</summary>
    /// <param name="dataSize">Payload size in bytes</param>
    /// <param name="defaultChunkSize">Fallback chunk size when size is 0 or unknown</param>
    /// <returns>Chunk size in bytes</returns>
    public static int DetermineChunkSize(long dataSize, int? defaultChunkSize = null) => DetermineChunkSize(dataSize, null, defaultChunkSize);

    /// <summary>Picks a chunk size from a known data length and optional custom thresholds.</summary>
    /// <param name="dataSize">Payload size in bytes</param>
    /// <param name="options">Custom thresholds. Null uses built-in defaults.</param>
    /// <param name="defaultChunkSize">Fallback when size is 0 or unknown. Overrides options when set.</param>
    /// <returns>Chunk size in bytes</returns>
    public static int DetermineChunkSize(long dataSize, StreamChunkSizeOptions? options, int? defaultChunkSize = null)
    {
        if (dataSize <= 0)
            return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;

        var opts = options ?? new StreamChunkSizeOptions();
        if (dataSize < opts.SmallFileThreshold)
            return opts.MinChunkSize;

        if (dataSize < opts.MediumFileThreshold)
            return opts.DefaultChunkSize;

        if (dataSize < opts.LargeFileThreshold)
            return 2 * 1024 * 1024;

        if (dataSize < opts.VeryLargeFileThreshold)
            return 5 * 1024 * 1024;

        return opts.MaxChunkSize;
    }
}