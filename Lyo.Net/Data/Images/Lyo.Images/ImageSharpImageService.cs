using Lyo.Images.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Images;

/// <summary>ImageSharp-backed <see cref="IImageService" />. Extracts full EXIF (location, device, date taken, and related tags).</summary>
/// <remarks>
/// DI via <c>Lyo.Images.Extensions</c> (<c>AddImageSharpImageService</c>) also registers <see cref="IImageDecorationService" /> when missing, so callers can
/// resolve overlay/frame/caption/padding primitives without a second registration.
/// </remarks>
public class ImageSharpImageService : ImageServiceBase
{
    /// <summary>Builds a new <see cref="ImageSharpImageService" />.</summary>
    /// <param name="options">Image-service options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="metrics">Optional metrics sink.</param>
    public ImageSharpImageService(ImageServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics) { }
}