using Lyo.Images.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Images;

/// <summary>Image service implementation using SixLabors.ImageSharp. Supports full EXIF metadata extraction (location, device, date taken, etc.).</summary>
/// <remarks>
/// DI registration via <c>Lyo.Images.Extensions</c> (<c>AddImageSharpImageService</c>) also registers <see cref="IImageDecorationService" /> when missing, so consumers can
/// resolve overlay/frame/caption/padding primitives without an extra registration step.
/// </remarks>
public class ImageSharpImageService : ImageServiceBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageSharpImageService" /> class.</summary>
    /// <param name="options">The image service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance.</param>
    public ImageSharpImageService(ImageServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics) { }
}