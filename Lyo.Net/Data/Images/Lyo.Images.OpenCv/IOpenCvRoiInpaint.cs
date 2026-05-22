using Lyo.Result;

namespace Lyo.Images.OpenCv;

/// <summary>
/// OpenCV inpaint on a <b>single ROI</b> delivered as PNG bytes (color). Implementations are thread-safe if underlying OpenCV usage is re-entrant for distinct buffers (typical for stateless decode/inpaint/encode per call).
/// </summary>
public interface IOpenCvRoiInpaint
{
    /// <summary>
    /// Decodes a color PNG ROI, fills a binary mask rectangle with 255 (inpaint) and 0 elsewhere, runs <c>Cv2.Inpaint</c>, returns PNG-encoded BGR output matching the decoded ROI size.
    /// </summary>
    /// <param name="roiColorPng">PNG bytes (e.g. from ImageSharp <c>SaveAsPng</c>).</param>
    /// <param name="maskX">Left of mask rectangle in ROI pixels.</param>
    /// <param name="maskY">Top of mask rectangle in ROI pixels.</param>
    /// <param name="maskWidth">Mask width (clamped).</param>
    /// <param name="maskHeight">Mask height (clamped).</param>
    /// <param name="radius">Inpaint radius (clamped 1–64).</param>
    /// <param name="algorithm">Telea or Navier–Stokes.</param>
    /// <returns>PNG bytes on success; failure codes <c>OpenCvInpaint.DecodeFailed</c> or <c>OpenCvInpaint.InpaintError</c>.</returns>
    Result<byte[]> InpaintColorRoiPng(
        ReadOnlyMemory<byte> roiColorPng,
        int maskX,
        int maskY,
        int maskWidth,
        int maskHeight,
        int radius,
        OpenCvInpaintAlgorithm algorithm = OpenCvInpaintAlgorithm.Telea);
}
