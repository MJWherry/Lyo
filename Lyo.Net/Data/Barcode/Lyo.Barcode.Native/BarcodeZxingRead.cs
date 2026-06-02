using Lyo.Barcode.Models;
using Lyo.Exceptions;
using Lyo.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing.ImageSharp;
using static Lyo.Barcode.BarcodeErrorCodes;
using ZxFormat = ZXing.BarcodeFormat;

namespace Lyo.Barcode.Native;

/// <summary>Decode barcode images with ZXing (shared by <see cref="NativeBarcodeService" />).</summary>
public static class BarcodeZxingRead
{
    /// <summary>Decode the first supported linear or 2D barcode in an image.</summary>
    public static Result<BarcodeImageReadResult> Decode(byte[] imageBytes)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            return Result<BarcodeImageReadResult>.Failure(new Error("Image bytes are empty.", InvalidImage));

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
                return Result<BarcodeImageReadResult>.Failure(new Error("No barcode found in image.", ReadFailed));

            return Result<BarcodeImageReadResult>.Success(new() { Text = r.Text ?? "", FormatName = r.BarcodeFormat.ToString() });
        }
        catch (Exception ex) {
            return Result<BarcodeImageReadResult>.Failure(Error.FromException(ex, InvalidImage));
        }
    }
}
