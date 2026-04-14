namespace Lyo.Compression;

/// <summary>Error codes used by Compression services.</summary>
public static class CompressionErrorCodes
{
    /// <summary>Failed to compress data.</summary>
    public const string CompressFailed = "COMPRESSION_FAILED";

    /// <summary>Failed to decompress data.</summary>
    public const string DecompressFailed = "DECOMPRESSION_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "COMPRESSION_OPERATION_CANCELLED";

    /// <summary>Input data exceeds maximum allowed size.</summary>
    public const string InputTooLarge = "COMPRESSION_INPUT_TOO_LARGE";

    /// <summary>Input data is too small.</summary>
    public const string InputTooSmall = "COMPRESSION_INPUT_TOO_SMALL";

    /// <summary>File operation failed.</summary>
    public const string FileOperationFailed = "COMPRESSION_FILE_OPERATION_FAILED";
}