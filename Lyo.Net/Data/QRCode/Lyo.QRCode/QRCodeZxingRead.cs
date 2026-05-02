using Lyo.Codes.ZXing;
using Lyo.Exceptions;
using Lyo.QRCode.Models;
using Lyo.Result;

namespace Lyo.QRCode;

/// <summary>Decode QR images with ZXing (shared by <see cref="BuiltInQRCodeService" /> and QRCoder-backed services).</summary>
public static class QRCodeZxingRead
{
    /// <summary>Decode the first QR code in an image (PNG, JPEG, BMP, etc.).</summary>
    public static Result<QRCodeImageReadResult> Decode(byte[] imageBytes)
    {
        ArgumentHelpers.ThrowIfNull(imageBytes);
        return QRCodeZxingReadMapper.Map(ZxingCodeImageDecoder.DecodeQrCode(imageBytes));
    }
}