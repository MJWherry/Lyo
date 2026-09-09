namespace Lyo.Compression;

/// <summary>Error codes used by compression services.</summary>
public static class CompressionErrorCodes
{
    /// <summary>Compress failed.</summary>
    public const string CompressFailed = "COMPRESSION_FAILED";

    /// <summary>Decompress failed.</summary>
    public const string DecompressFailed = "DECOMPRESSION_FAILED";

    /// <summary>Operation was canceled.</summary>
    public const string OperationCancelled = "COMPRESSION_OPERATION_CANCELLED";

    /// <summary>Input exceeds the maximum allowed size.</summary>
    public const string InputTooLarge = "COMPRESSION_INPUT_TOO_LARGE";

    /// <summary>Input is too small.</summary>
    public const string InputTooSmall = "COMPRESSION_INPUT_TOO_SMALL";

    /// <summary>A file operation failed.</summary>
    public const string FileOperationFailed = "COMPRESSION_FILE_OPERATION_FAILED";
}