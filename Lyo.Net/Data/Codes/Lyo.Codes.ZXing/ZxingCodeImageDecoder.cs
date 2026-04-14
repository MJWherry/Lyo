using Lyo.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing.ImageSharp;
using ZxFormat = ZXing.BarcodeFormat;

namespace Lyo.Codes.ZXing;

/// <summary>Neutral decode result (mapped to QR/barcode-specific models by callers).</summary>
public sealed record CodeReadPayload(string Text, string FormatName);

/// <summary>Decode QR and linear barcodes from image bytes using ZXing.Net + ImageSharp.</summary>
public static class ZxingCodeImageDecoder
{
    /// <summary>Decode a QR code from PNG, JPEG, BMP, GIF, WebP, or other formats ImageSharp can load.</summary>
    public static Result<CodeReadPayload> DecodeQrCode(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            return Result<CodeReadPayload>.Failure(new Error("Image bytes are empty.", ZxingDecodeErrorCodes.ImageEmpty));

        try {
            using var ms = new MemoryStream(imageBytes, false);
            using var image = Image.Load<Rgba32>(ms);
            var reader = new BarcodeReader<Rgba32> {
                AutoRotate = true, Options = new() { PossibleFormats = new List<ZxFormat> { ZxFormat.QR_CODE }, TryHarder = true, PureBarcode = false }
            };

            var r = reader.Decode(image);
            if (r == null)
                return Result<CodeReadPayload>.Failure(new Error("No QR code found in image.", ZxingDecodeErrorCodes.NoQr));

            return Result<CodeReadPayload>.Success(new(r.Text ?? "", r.BarcodeFormat.ToString()));
        }
        catch (Exception ex) {
            return Result<CodeReadPayload>.Failure(Error.FromException(ex, ZxingDecodeErrorCodes.ImageLoad));
        }
    }

    /// <summary>Decode a linear or 2D barcode (Code 128 listed first among candidates).</summary>
    public static Result<CodeReadPayload> DecodeBarcode(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            return Result<CodeReadPayload>.Failure(new Error("Image bytes are empty.", ZxingDecodeErrorCodes.ImageEmpty));

        try {
            using var ms = new MemoryStream(imageBytes, false);
            using var image = Image.Load<Rgba32>(ms);
            var reader = new BarcodeReader<Rgba32> {
                AutoRotate = true,
                Options = new() {
                    PossibleFormats = new List<ZxFormat> {
                        ZxFormat.CODE_128,
                        ZxFormat.CODE_39,
                        ZxFormat.EAN_13,
                        ZxFormat.EAN_8,
                        ZxFormat.UPC_A,
                        ZxFormat.UPC_E,
                        ZxFormat.ITF,
                        ZxFormat.CODABAR,
                        ZxFormat.PDF_417,
                        ZxFormat.DATA_MATRIX
                    },
                    TryHarder = true,
                    PureBarcode = false
                }
            };

            var r = reader.Decode(image);
            if (r == null)
                return Result<CodeReadPayload>.Failure(new Error("No barcode found in image.", ZxingDecodeErrorCodes.NoBarcode));

            return Result<CodeReadPayload>.Success(new(r.Text ?? "", r.BarcodeFormat.ToString()));
        }
        catch (Exception ex) {
            return Result<CodeReadPayload>.Failure(Error.FromException(ex, ZxingDecodeErrorCodes.ImageLoad));
        }
    }
}