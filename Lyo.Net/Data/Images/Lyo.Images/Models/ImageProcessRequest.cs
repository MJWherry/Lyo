using Lyo.Common.Enums;

namespace Lyo.Images.Models;

/// <summary>Represents a batch image processing request (input/output streams plus one operation).</summary>
public class ImageProcessRequest
{
    /// <summary>Readable stream containing the source image.</summary>
    public Stream InputStream { get; set; } = null!;

    /// <summary>Writable stream that receives the processed image.</summary>
    public Stream OutputStream { get; set; } = null!;

    /// <summary>Operation to perform (resize, crop, rotate, etc.).</summary>
    public ImageOperation Operation { get; set; } = null!;

    /// <summary>Output format; when null, the implementation chooses a default (often matching input).</summary>
    public ImageFormat? TargetFormat { get; set; }

    /// <summary>Lossy quality 1–100 when <see cref="TargetFormat" /> is JPEG or similar; null uses service defaults.</summary>
    public int? Quality { get; set; }
}

/// <summary>Base class for image operations.</summary>
public abstract class ImageOperation { }

/// <summary>Resize operation.</summary>
public class ResizeOperation : ImageOperation
{
    /// <summary>Target width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Target height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>How width/height constrain the result.</summary>
    public ResizeMode Mode { get; set; } = ResizeMode.Max;
}

/// <summary>Crop operation.</summary>
public class CropOperation : ImageOperation
{
    /// <summary>Left edge of the crop rectangle in pixels.</summary>
    public int X { get; set; }

    /// <summary>Top edge of the crop rectangle in pixels.</summary>
    public int Y { get; set; }

    /// <summary>Crop width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Crop height in pixels.</summary>
    public int Height { get; set; }
}

/// <summary>Rotate operation.</summary>
public class RotateOperation : ImageOperation
{
    /// <summary>Clockwise rotation angle in degrees.</summary>
    public float Degrees { get; set; }
}

/// <summary>Watermark operation.</summary>
public class WatermarkOperation : ImageOperation
{
    /// <summary>Text to draw onto the image.</summary>
    public string WatermarkText { get; set; } = null!;

    /// <summary>Font, color, position, and opacity; null uses implementation defaults.</summary>
    public WatermarkOptions? Options { get; set; }
}

/// <summary>Compress operation.</summary>
public class CompressOperation : ImageOperation
{
    /// <summary>Target quality for lossy re-encode (1–100).</summary>
    public int Quality { get; set; }
}

/// <summary>Convert format operation.</summary>
public class ConvertFormatOperation : ImageOperation
{
    /// <summary>Destination container/codec.</summary>
    public ImageFormat TargetFormat { get; set; }
}