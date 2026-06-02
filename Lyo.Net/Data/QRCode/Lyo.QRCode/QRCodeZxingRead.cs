using Lyo.Exceptions;
using Lyo.QRCode.Models;
using Lyo.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing.ImageSharp;
using static Lyo.QRCode.QRCodeErrorCodes;
using ZxFormat = ZXing.BarcodeFormat;

namespace Lyo.QRCode;

/// <summary>Decode QR images with ZXing (shared by <see cref="BuiltInQRCodeService" /> and QRCoder-backed services).</summary>
public static class QRCodeZxingRead
{
    /// <summary>Decode the first QR code in an image (PNG, JPEG, BMP, etc.).</summary>
    public static Result<QRCodeImageReadResult> Decode(byte[] imageBytes)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            return Result<QRCodeImageReadResult>.Failure(new Error("Image bytes are empty.", InvalidImage));

        try {
            using var ms = new MemoryStream(imageBytes, false);
            using var image = Image.Load<Rgba32>(ms);
            var reader = new BarcodeReader<Rgba32> {
                AutoRotate = true, Options = new() { PossibleFormats = new List<ZxFormat> { ZxFormat.QR_CODE }, TryHarder = true, PureBarcode = false }
            };

            var r = reader.Decode(image);
            if (r == null)
                return Result<QRCodeImageReadResult>.Failure(new Error("No QR code found in image.", ReadFailed));

            return Result<QRCodeImageReadResult>.Success(new() { Text = r.Text ?? "", FormatName = r.BarcodeFormat.ToString() });
        }
        catch (Exception ex) {
            return Result<QRCodeImageReadResult>.Failure(Error.FromException(ex, InvalidImage));
        }
    }
}