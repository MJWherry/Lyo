namespace Lyo.Images;

/// <summary>Shared constants for the Images library.</summary>
public static class Constants
{
    /// <summary>Image-service metric names.</summary>
    public static class Metrics
    {
        /// <summary>Resize duration metric.</summary>
        public const string ResizeDuration = "images.resize.duration";

        /// <summary>Crop duration metric.</summary>
        public const string CropDuration = "images.crop.duration";

        /// <summary>Rotate duration metric.</summary>
        public const string RotateDuration = "images.rotate.duration";

        /// <summary>Watermark duration metric.</summary>
        public const string WatermarkDuration = "images.watermark.duration";

        /// <summary>Format-convert duration metric.</summary>
        public const string ConvertDuration = "images.convert.duration";

        /// <summary>Thumbnail duration metric.</summary>
        public const string ThumbnailDuration = "images.thumbnail.duration";

        /// <summary>Metadata duration metric.</summary>
        public const string MetadataDuration = "images.metadata.duration";

        /// <summary>Palette-extract duration metric.</summary>
        public const string PaletteDuration = "images.palette.duration";

        /// <summary>Compress duration metric.</summary>
        public const string CompressDuration = "images.compress.duration";

        /// <summary>Batch-process duration metric.</summary>
        public const string BatchProcessDuration = "images.batch.process.duration";

        /// <summary>Centered-overlay composite duration metric.</summary>
        public const string CompositeOverlayDuration = "images.composite.overlay.duration";
    }
}