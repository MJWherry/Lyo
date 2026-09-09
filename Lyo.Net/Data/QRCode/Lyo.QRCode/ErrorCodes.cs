namespace Lyo.QRCode;

/// <summary>Error codes used by QR services.</summary>
public static class QRCodeErrorCodes
{
    /// <summary>QR generation failed.</summary>
    public const string GenerateFailed = "QRCODE_GENERATE_FAILED";

    /// <summary>The operation was cancelled.</summary>
    public const string OperationCancelled = "QRCODE_OPERATION_CANCELLED";

    /// <summary>QR payload data is invalid.</summary>
    public const string InvalidData = "QRCODE_INVALID_DATA";

    /// <summary>QR size is invalid.</summary>
    public const string InvalidSize = "QRCODE_INVALID_SIZE";

    /// <summary>A file operation failed.</summary>
    public const string FileOperationFailed = "QRCODE_FILE_OPERATION_FAILED";

    /// <summary>A stream operation failed.</summary>
    public const string StreamOperationFailed = "QRCODE_STREAM_OPERATION_FAILED";

    /// <summary>Could not read a QR code from the image.</summary>
    public const string ReadFailed = "QRCODE_READ_FAILED";

    /// <summary>Image bytes are missing, corrupt, or in an unsupported format.</summary>
    public const string InvalidImage = "QRCODE_INVALID_IMAGE";
}