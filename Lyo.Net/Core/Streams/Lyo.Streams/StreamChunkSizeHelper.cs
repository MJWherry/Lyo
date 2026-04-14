namespace Lyo.Streams;

/// <summary>Options for customizing chunk size thresholds used by <see cref="StreamChunkSizeHelper" />.</summary>
public class StreamChunkSizeOptions
{
    /// <summary>Minimum chunk size (default 64KB). Used for small files under 1MB.</summary>
    public int MinChunkSize { get; set; } = 64 * 1024;

    /// <summary>Default chunk size (default 1MB). Used for medium files 1MB–100MB.</summary>
    public int DefaultChunkSize { get; set; } = 1024 * 1024;

    /// <summary>Maximum chunk size (default 10MB). Used for very large files over 10GB.</summary>
    public int MaxChunkSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>Size threshold in bytes for "small" files (default 1MB). Files below this use MinChunkSize.</summary>
    public long SmallFileThreshold { get; set; } = 1024 * 1024;

    /// <summary>Size threshold in bytes for "medium" files (default 100MB).</summary>
    public long MediumFileThreshold { get; set; } = 100 * 1024 * 1024;

    /// <summary>Size threshold in bytes for "large" files (default 1GB).</summary>
    public long LargeFileThreshold { get; set; } = 1024L * 1024 * 1024;

    /// <summary>Size threshold in bytes for "very large" files (default 10GB).</summary>
    public long VeryLargeFileThreshold { get; set; } = 10L * 1024 * 1024 * 1024;
}

/// <summary>Helper class for determining optimal chunk sizes for stream operations based on stream or file size.</summary>
/// <remarks>Uses size-based heuristics: smaller chunks for small files to avoid over-allocation, larger chunks for big files to reduce I/O overhead.</remarks>
public static class StreamChunkSizeHelper
{
    private const int DefaultChunkSize = 1024 * 1024; // 1MB

    private const int MinChunkSize = 64 * 1024; // 64KB

    private const int MaxChunkSize = 10 * 1024 * 1024; // 10MB

    /// <summary>Determines an optimal chunk size based on the stream size.</summary>
    /// <param name="stream">The stream to analyze</param>
    /// <param name="defaultChunkSize">Default chunk size to use if stream size cannot be determined</param>
    /// <param name="options">Custom thresholds. When null, uses default values.</param>
    /// <returns>Optimal chunk size in bytes</returns>
    public static int DetermineChunkSize(Stream? stream, int? defaultChunkSize = null, StreamChunkSizeOptions? options = null)
    {
        if (stream == null)
            return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;

        try {
            if (stream.CanSeek && stream.Length > 0)
                return DetermineChunkSize(stream.Length, options, defaultChunkSize);
        }
        catch {
            // If we can't determine size, use default
        }

        return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;
    }

    /// <summary>Determines an optimal chunk size based on file size.</summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="defaultChunkSize">Default chunk size to use if file size cannot be determined</param>
    /// <param name="options">Custom thresholds. When null, uses default values.</param>
    /// <returns>Optimal chunk size in bytes</returns>
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
            // If we can't determine size, use default
        }

        return defaultChunkSize ?? options?.DefaultChunkSize ?? DefaultChunkSize;
    }

    /// <summary>Determines an optimal chunk size based on data size.</summary>
    /// <param name="dataSize">Size of the data in bytes</param>
    /// <param name="defaultChunkSize">Default chunk size to use if size is 0 or unknown</param>
    /// <returns>Optimal chunk size in bytes</returns>
    public static int DetermineChunkSize(long dataSize, int? defaultChunkSize = null) => DetermineChunkSize(dataSize, null, defaultChunkSize);

    /// <summary>Determines an optimal chunk size based on data size with custom thresholds.</summary>
    /// <param name="dataSize">Size of the data in bytes</param>
    /// <param name="options">Custom thresholds. When null, uses default values.</param>
    /// <param name="defaultChunkSize">Default chunk size to use if size is 0 or unknown. Overrides options when specified.</param>
    /// <returns>Optimal chunk size in bytes</returns>
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