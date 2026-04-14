using Lyo.Barcode.Models;
using Lyo.Codes.ZXing;
using Lyo.Common;
using static Lyo.Barcode.BarcodeErrorCodes;

namespace Lyo.Barcode.Native;

internal static class BarcodeZxingReadMapper
{
    internal static Result<BarcodeImageReadResult> Map(Result<CodeReadPayload> r)
    {
        if (r.IsSuccess && r.Data != null)
            return Result<BarcodeImageReadResult>.Success(new() { Text = r.Data.Text, FormatName = r.Data.FormatName });

        var e = r.Errors?[0];
        var code = e?.Code switch {
            ZxingDecodeErrorCodes.ImageEmpty => InvalidImage,
            ZxingDecodeErrorCodes.ImageLoad => InvalidImage,
            var _ => ReadFailed
        };

        return Result<BarcodeImageReadResult>.Failure(new Error(e?.Message ?? "Barcode decode failed", code));
    }
}