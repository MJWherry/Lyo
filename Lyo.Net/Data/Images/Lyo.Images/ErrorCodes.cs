namespace Lyo.Images;

/// <summary>Error codes used by image services.</summary>
public static class ImageErrorCodes
{
    /// <summary>Failed to resize image.</summary>
    public const string ResizeFailed = "IMAGE_RESIZE_FAILED";

    /// <summary>Failed to crop image.</summary>
    public const string CropFailed = "IMAGE_CROP_FAILED";

    /// <summary>Failed to rotate image.</summary>
    public const string RotateFailed = "IMAGE_ROTATE_FAILED";

    /// <summary>Failed to add watermark.</summary>
    public const string WatermarkFailed = "IMAGE_WATERMARK_FAILED";

    /// <summary>Failed to convert image format.</summary>
    public const string ConvertFormatFailed = "IMAGE_CONVERT_FORMAT_FAILED";

    /// <summary>Failed to generate thumbnail.</summary>
    public const string GenerateThumbnailFailed = "IMAGE_GENERATE_THUMBNAIL_FAILED";

    /// <summary>Failed to get image metadata.</summary>
    public const string GetMetadataFailed = "IMAGE_GET_METADATA_FAILED";

    /// <summary>Failed to get image palette.</summary>
    public const string GetPaletteFailed = "IMAGE_GET_PALETTE_FAILED";

    /// <summary>Failed to compress image.</summary>
    public const string CompressFailed = "IMAGE_COMPRESS_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "IMAGE_OPERATION_CANCELLED";

    /// <summary>File operation failed.</summary>
    public const string FileOperationFailed = "IMAGE_FILE_OPERATION_FAILED";

    /// <summary>Stream operation failed.</summary>
    public const string StreamOperationFailed = "IMAGE_STREAM_OPERATION_FAILED";

    /// <summary>Invalid image format.</summary>
    public const string InvalidFormat = "IMAGE_INVALID_FORMAT";

    /// <summary>Invalid image dimensions.</summary>
    public const string InvalidDimensions = "IMAGE_INVALID_DIMENSIONS";

    /// <summary>Failed to composite center overlay.</summary>
    public const string CompositeOverlayFailed = "IMAGE_COMPOSITE_OVERLAY_FAILED";

    /// <summary>Failed to composite QR decorative frame.</summary>
    public const string QrFrameCompositeFailed = "IMAGE_QR_FRAME_COMPOSITE_FAILED";
}