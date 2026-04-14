namespace Lyo.Images.Skia;

/// <summary>Consolidated constants for the Skia Images library.</summary>
public static class Constants
{
    /// <summary>Constants for Skia image service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for resize operations.</summary>
        public const string ResizeDuration = "images.skia.resize.duration";

        /// <summary>Duration metric for crop operations.</summary>
        public const string CropDuration = "images.skia.crop.duration";

        /// <summary>Duration metric for rotate operations.</summary>
        public const string RotateDuration = "images.skia.rotate.duration";

        /// <summary>Duration metric for watermark operations.</summary>
        public const string WatermarkDuration = "images.skia.watermark.duration";

        /// <summary>Duration metric for convert format operations.</summary>
        public const string ConvertDuration = "images.skia.convert.duration";

        /// <summary>Duration metric for thumbnail generation operations.</summary>
        public const string ThumbnailDuration = "images.skia.thumbnail.duration";

        /// <summary>Duration metric for metadata operations.</summary>
        public const string MetadataDuration = "images.skia.metadata.duration";

        /// <summary>Duration metric for palette extraction operations.</summary>
        public const string PaletteDuration = "images.skia.palette.duration";

        /// <summary>Duration metric for compress operations.</summary>
        public const string CompressDuration = "images.skia.compress.duration";

        /// <summary>Duration metric for batch processing operations.</summary>
        public const string BatchProcessDuration = "images.skia.batch.process.duration";
    }
}