namespace Lyo.Barcode;

/// <summary>Error codes used by barcode services.</summary>
public static class BarcodeErrorCodes
{
    public const string GenerateFailed = "BARCODE_GENERATE_FAILED";

    public const string OperationCancelled = "BARCODE_OPERATION_CANCELLED";

    public const string InvalidData = "BARCODE_INVALID_DATA";

    public const string InvalidDimensions = "BARCODE_INVALID_DIMENSIONS";

    public const string FileOperationFailed = "BARCODE_FILE_OPERATION_FAILED";

    public const string StreamOperationFailed = "BARCODE_STREAM_OPERATION_FAILED";

    public const string UnsupportedSymbology = "BARCODE_UNSUPPORTED_SYMBOLOGY";

    public const string ReadFailed = "BARCODE_READ_FAILED";

    public const string InvalidImage = "BARCODE_INVALID_IMAGE";
}