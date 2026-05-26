using Lyo.Result;
using OpenCvSharp;

namespace Lyo.Images.OpenCv;

/// <summary>Default <see cref="IOpenCvRoiInpaint" /> using OpenCvSharp decode, binary mask, inpaint, and PNG encode.</summary>
public sealed class OpenCvRoiInpaintService : IOpenCvRoiInpaint
{
    /// <inheritdoc />
    public Result<byte[]> InpaintColorRoiPng(
        ReadOnlyMemory<byte> roiColorPng,
        int maskX,
        int maskY,
        int maskWidth,
        int maskHeight,
        int radius,
        OpenCvInpaintAlgorithm algorithm = OpenCvInpaintAlgorithm.Telea)
    {
        radius = Math.Clamp(radius, 1, 64);
        using var src = Cv2.ImDecode(roiColorPng.Span, ImreadModes.Color);
        if (src.Empty())
            return Result<byte[]>.Failure("OpenCV could not decode ROI PNG.", "OpenCvInpaint.DecodeFailed");

        var w = src.Width;
        var h = src.Height;
        var mx = Math.Clamp(maskX, 0, Math.Max(0, w - 1));
        var my = Math.Clamp(maskY, 0, Math.Max(0, h - 1));
        var mw = Math.Clamp(maskWidth, 1, w - mx);
        var mh = Math.Clamp(maskHeight, 1, h - my);
        var mr = new Rect(mx, my, mw, mh);
        var inpaintType = algorithm switch {
            OpenCvInpaintAlgorithm.NavierStokes => InpaintTypes.NS,
            var _ => InpaintTypes.Telea
        };

        try {
            using var mask = new Mat(h, w, MatType.CV_8UC1, Scalar.All(0));
            Cv2.Rectangle(mask, mr, Scalar.All(255), -1);
            using var dst = new Mat();
            Cv2.Inpaint(src, mask, dst, radius, inpaintType);
            Cv2.ImEncode(".png", dst, out var outPng);
            return Result<byte[]>.Success(outPng);
        }
        catch (Exception ex) {
            return Result<byte[]>.Failure(ex, "OpenCvInpaint.InpaintError");
        }
    }
}