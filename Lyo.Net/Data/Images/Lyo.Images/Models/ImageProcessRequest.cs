using System.Diagnostics;
using Lyo.Common.Core.Enums;

namespace Lyo.Images.Models;

/// <summary>One batch image item: input/output streams and a single operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ImageProcessRequest
{
    /// <summary>Readable stream of the source image.</summary>
    public Stream InputStream { get; set; } = null!;

    /// <summary>Writable stream for the processed image.</summary>
    public Stream OutputStream { get; set; } = null!;

    /// <summary>Operation to run (resize, crop, rotate, and similar).</summary>
    public ImageOperation Operation { get; set; } = null!;

    /// <summary>Output format. Null lets the backend pick a default (often the input format).</summary>
    public ImageFormat? TargetFormat { get; set; }

    /// <summary>Lossy quality 1–100 when <see cref="TargetFormat" /> is JPEG or similar. Null uses service defaults.</summary>
    public int? Quality { get; set; }

    public override string ToString() => $"ImageProcessRequest: operation={Operation}, format={TargetFormat}, quality={Quality}";
}

/// <summary>Base type for image operations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class ImageOperation
{
    public override string ToString() => GetType().Name;
}

/// <summary>Resize the image.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ResizeOperation : ImageOperation
{
    /// <summary>Target width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Target height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>How width and height constrain the result.</summary>
    public ResizeMode Mode { get; set; } = ResizeMode.Max;

    public override string ToString() => $"ResizeOperation: {Width}x{Height}, mode={Mode}";
}

/// <summary>Crop the image.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CropOperation : ImageOperation
{
    /// <summary>Left edge of the crop rectangle, in pixels.</summary>
    public int X { get; set; }

    /// <summary>Top edge of the crop rectangle, in pixels.</summary>
    public int Y { get; set; }

    /// <summary>Crop width, in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Crop height, in pixels.</summary>
    public int Height { get; set; }

    public override string ToString() => $"CropOperation: ({X},{Y}) {Width}x{Height}";
}

/// <summary>Rotate the image.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class RotateOperation : ImageOperation
{
    /// <summary>Clockwise rotation in degrees.</summary>
    public float Degrees { get; set; }

    public override string ToString() => $"RotateOperation: {Degrees}°";
}

/// <summary>Draw a watermark.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class WatermarkOperation : ImageOperation
{
    /// <summary>Text to draw on the image.</summary>
    public string WatermarkText { get; set; } = null!;

    /// <summary>Font, color, position, and opacity. Null uses backend defaults.</summary>
    public WatermarkOptions? Options { get; set; }

    public override string ToString() => $"WatermarkOperation: text={WatermarkText}, position={Options?.Position}";
}

/// <summary>Compress the image.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CompressOperation : ImageOperation
{
    /// <summary>Target quality for a lossy re-encode (1–100).</summary>
    public int Quality { get; set; }

    public override string ToString() => $"CompressOperation: quality={Quality}";
}

/// <summary>Convert the image format.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ConvertFormatOperation : ImageOperation
{
    /// <summary>Destination container or codec.</summary>
    public ImageFormat TargetFormat { get; set; }

    public override string ToString() => $"ConvertFormatOperation: format={TargetFormat}";
}