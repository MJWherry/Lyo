namespace Lyo.Images.Skia;

/// <summary>Shared constants used by the Skia Images library.</summary>
public static class Constants
{
    /// <summary>Metric names for the Skia image service.</summary>
    public static class Metrics
    {
        /// <summary>Histogram of resize duration.</summary>
        public const string ResizeDuration = "images.skia.resize.duration";

        /// <summary>Histogram of crop duration.</summary>
        public const string CropDuration = "images.skia.crop.duration";

        /// <summary>Histogram of rotate duration.</summary>
        public const string RotateDuration = "images.skia.rotate.duration";

        /// <summary>Histogram of watermark duration.</summary>
        public const string WatermarkDuration = "images.skia.watermark.duration";

        /// <summary>Histogram of format-convert duration.</summary>
        public const string ConvertDuration = "images.skia.convert.duration";

        /// <summary>Histogram of thumbnail duration.</summary>
        public const string ThumbnailDuration = "images.skia.thumbnail.duration";

        /// <summary>Histogram of metadata-read duration.</summary>
        public const string MetadataDuration = "images.skia.metadata.duration";

        /// <summary>Histogram of palette-extract duration.</summary>
        public const string PaletteDuration = "images.skia.palette.duration";

        /// <summary>Histogram of compress duration.</summary>
        public const string CompressDuration = "images.skia.compress.duration";

        /// <summary>Histogram of batch-process duration.</summary>
        public const string BatchProcessDuration = "images.skia.batch.process.duration";
    }
}