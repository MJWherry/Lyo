namespace Lyo.Images;

/// <summary>Error codes from image services.</summary>
public static class ImageErrorCodes
{
    /// <summary>Resize failed.</summary>
    public const string ResizeFailed = "IMAGE_RESIZE_FAILED";

    /// <summary>Crop failed.</summary>
    public const string CropFailed = "IMAGE_CROP_FAILED";

    /// <summary>Rotate failed.</summary>
    public const string RotateFailed = "IMAGE_ROTATE_FAILED";

    /// <summary>Watermark failed.</summary>
    public const string WatermarkFailed = "IMAGE_WATERMARK_FAILED";

    /// <summary>Format conversion failed.</summary>
    public const string ConvertFormatFailed = "IMAGE_CONVERT_FORMAT_FAILED";

    /// <summary>Thumbnail generation failed.</summary>
    public const string GenerateThumbnailFailed = "IMAGE_GENERATE_THUMBNAIL_FAILED";

    /// <summary>Metadata read failed.</summary>
    public const string GetMetadataFailed = "IMAGE_GET_METADATA_FAILED";

    /// <summary>Palette extraction failed.</summary>
    public const string GetPaletteFailed = "IMAGE_GET_PALETTE_FAILED";

    /// <summary>Compress failed.</summary>
    public const string CompressFailed = "IMAGE_COMPRESS_FAILED";

    /// <summary>Operation cancelled.</summary>
    public const string OperationCancelled = "IMAGE_OPERATION_CANCELLED";

    /// <summary>File I/O failed.</summary>
    public const string FileOperationFailed = "IMAGE_FILE_OPERATION_FAILED";

    /// <summary>Stream I/O failed.</summary>
    public const string StreamOperationFailed = "IMAGE_STREAM_OPERATION_FAILED";

    /// <summary>Image format is invalid.</summary>
    public const string InvalidFormat = "IMAGE_INVALID_FORMAT";

    /// <summary>Image dimensions are invalid.</summary>
    public const string InvalidDimensions = "IMAGE_INVALID_DIMENSIONS";

    /// <summary>Overlay composite onto the background failed.</summary>
    public const string CompositeOverlayFailed = "IMAGE_COMPOSITE_OVERLAY_FAILED";

    /// <summary>Decorative frame draw failed.</summary>
    public const string FrameCompositeFailed = "IMAGE_FRAME_COMPOSITE_FAILED";

    /// <summary>Caption-band composite failed.</summary>
    public const string CaptionCompositeFailed = "IMAGE_CAPTION_COMPOSITE_FAILED";

    /// <summary>Outer padding/shadow apply failed.</summary>
    public const string OuterPaddingCompositeFailed = "IMAGE_OUTER_PADDING_COMPOSITE_FAILED";
}