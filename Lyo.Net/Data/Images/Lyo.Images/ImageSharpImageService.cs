using Lyo.Images.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Images;

/// <summary>Image service implementation using SixLabors.ImageSharp. Supports full EXIF metadata extraction (location, device, date taken, etc.).</summary>
public class ImageSharpImageService : ImageServiceBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageSharpImageService" /> class.</summary>
    /// <param name="options">The image service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance.</param>
    public ImageSharpImageService(ImageServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics) { }
}