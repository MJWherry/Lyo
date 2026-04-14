namespace Lyo.Images;

/// <summary>Consolidated constants for the Images library.</summary>
public static class Constants
{
    /// <summary>Constants for image service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for resize operations.</summary>
        public const string ResizeDuration = "images.resize.duration";

        /// <summary>Duration metric for crop operations.</summary>
        public const string CropDuration = "images.crop.duration";

        /// <summary>Duration metric for rotate operations.</summary>
        public const string RotateDuration = "images.rotate.duration";

        /// <summary>Duration metric for watermark operations.</summary>
        public const string WatermarkDuration = "images.watermark.duration";

        /// <summary>Duration metric for convert format operations.</summary>
        public const string ConvertDuration = "images.convert.duration";

        /// <summary>Duration metric for thumbnail generation operations.</summary>
        public const string ThumbnailDuration = "images.thumbnail.duration";

        /// <summary>Duration metric for metadata operations.</summary>
        public const string MetadataDuration = "images.metadata.duration";

        /// <summary>Duration metric for palette extraction operations.</summary>
        public const string PaletteDuration = "images.palette.duration";

        /// <summary>Duration metric for compress operations.</summary>
        public const string CompressDuration = "images.compress.duration";

        /// <summary>Duration metric for batch processing operations.</summary>
        public const string BatchProcessDuration = "images.batch.process.duration";

        /// <summary>Duration metric for center overlay (composite) operations.</summary>
        public const string CompositeOverlayDuration = "images.composite.overlay.duration";
    }
}