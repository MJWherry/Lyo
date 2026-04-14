namespace Lyo.Codes.ZXing;

/// <summary>Stable error codes from <see cref="ZxingCodeImageDecoder" /> for mapping in callers.</summary>
public static class ZxingDecodeErrorCodes
{
    public const string ImageEmpty = "ZXING_IMAGE_EMPTY";
    public const string ImageLoad = "ZXING_IMAGE_LOAD";
    public const string NoQr = "ZXING_NO_QR";
    public const string NoBarcode = "ZXING_NO_BARCODE";
}