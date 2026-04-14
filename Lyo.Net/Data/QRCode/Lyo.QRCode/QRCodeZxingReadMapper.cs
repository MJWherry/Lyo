using Lyo.Codes.ZXing;
using Lyo.Common;
using Lyo.QRCode.Models;
using static Lyo.QRCode.QRCodeErrorCodes;

namespace Lyo.QRCode;

internal static class QRCodeZxingReadMapper
{
    internal static Result<QRCodeImageReadResult> Map(Result<CodeReadPayload> r)
    {
        if (r.IsSuccess && r.Data != null)
            return Result<QRCodeImageReadResult>.Success(new() { Text = r.Data.Text, FormatName = r.Data.FormatName });

        var e = r.Errors?[0];
        var code = e?.Code switch {
            ZxingDecodeErrorCodes.ImageEmpty => InvalidImage,
            ZxingDecodeErrorCodes.ImageLoad => InvalidImage,
            var _ => ReadFailed
        };

        return Result<QRCodeImageReadResult>.Failure(new Error(e?.Message ?? "QR decode failed", code));
    }
}