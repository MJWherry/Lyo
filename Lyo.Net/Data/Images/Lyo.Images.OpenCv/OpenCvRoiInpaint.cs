using Lyo.Result;

namespace Lyo.Images.OpenCv;

/// <summary>
/// Static entry points for ROI inpaint without DI. Uses a shared <see cref="OpenCvRoiInpaintService"/> instance (stateless per call).
/// Prefer injecting <see cref="IOpenCvRoiInpaint"/> in applications that already use Microsoft DI.
/// </summary>
public static class OpenCvRoiInpaint
{
    private static readonly Lazy<IOpenCvRoiInpaint> Shared = new(static () => new OpenCvRoiInpaintService());

    /// <summary>Shared default implementation (same instance for all static calls).</summary>
    public static IOpenCvRoiInpaint SharedService => Shared.Value;

    /// <inheritdoc cref="IOpenCvRoiInpaint.InpaintColorRoiPng"/>
    public static Result<byte[]> InpaintColorRoiPng(
        ReadOnlyMemory<byte> roiColorPng,
        int maskX,
        int maskY,
        int maskWidth,
        int maskHeight,
        int radius,
        OpenCvInpaintAlgorithm algorithm = OpenCvInpaintAlgorithm.Telea) =>
        Shared.Value.InpaintColorRoiPng(roiColorPng, maskX, maskY, maskWidth, maskHeight, radius, algorithm);

    /// <summary>Convenience: <see cref="OpenCvInpaintAlgorithm.Telea"/>.</summary>
    public static Result<byte[]> InpaintTelea(
        ReadOnlyMemory<byte> roiColorPng,
        int maskX,
        int maskY,
        int maskWidth,
        int maskHeight,
        int radius) =>
        InpaintColorRoiPng(roiColorPng, maskX, maskY, maskWidth, maskHeight, radius, OpenCvInpaintAlgorithm.Telea);
}
