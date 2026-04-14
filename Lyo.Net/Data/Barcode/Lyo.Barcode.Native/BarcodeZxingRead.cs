using Lyo.Barcode.Models;
using Lyo.Codes.ZXing;
using Lyo.Common;
using Lyo.Exceptions;

namespace Lyo.Barcode.Native;

/// <summary>Decode barcode images with ZXing (shared by <see cref="NativeBarcodeService" />).</summary>
public static class BarcodeZxingRead
{
    /// <summary>Decode the first supported linear or 2D barcode in an image.</summary>
    public static Result<BarcodeImageReadResult> Decode(byte[] imageBytes)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes, nameof(imageBytes));
        return BarcodeZxingReadMapper.Map(ZxingCodeImageDecoder.DecodeBarcode(imageBytes));
    }
}