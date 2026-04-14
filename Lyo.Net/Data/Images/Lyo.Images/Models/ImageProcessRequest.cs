using Lyo.Common.Enums;

namespace Lyo.Images.Models;

/// <summary>Represents a batch image processing request.</summary>
public class ImageProcessRequest
{
    public Stream InputStream { get; set; } = null!;

    public Stream OutputStream { get; set; } = null!;

    public ImageOperation Operation { get; set; } = null!;

    public ImageFormat? TargetFormat { get; set; }

    public int? Quality { get; set; }
}

/// <summary>Base class for image operations.</summary>
public abstract class ImageOperation { }

/// <summary>Resize operation.</summary>
public class ResizeOperation : ImageOperation
{
    public int Width { get; set; }

    public int Height { get; set; }

    public ResizeMode Mode { get; set; } = ResizeMode.Max;
}

/// <summary>Crop operation.</summary>
public class CropOperation : ImageOperation
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}

/// <summary>Rotate operation.</summary>
public class RotateOperation : ImageOperation
{
    public float Degrees { get; set; }
}

/// <summary>Watermark operation.</summary>
public class WatermarkOperation : ImageOperation
{
    public string WatermarkText { get; set; } = null!;

    public WatermarkOptions? Options { get; set; }
}

/// <summary>Compress operation.</summary>
public class CompressOperation : ImageOperation
{
    public int Quality { get; set; }
}

/// <summary>Convert format operation.</summary>
public class ConvertFormatOperation : ImageOperation
{
    public ImageFormat TargetFormat { get; set; }
}