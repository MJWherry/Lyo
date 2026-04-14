namespace Lyo.QRCode;

/// <summary>Error codes used by QR code services.</summary>
public static class QRCodeErrorCodes
{
    /// <summary>Failed to generate QR code.</summary>
    public const string GenerateFailed = "QRCODE_GENERATE_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "QRCODE_OPERATION_CANCELLED";

    /// <summary>Invalid QR code data.</summary>
    public const string InvalidData = "QRCODE_INVALID_DATA";

    /// <summary>Invalid QR code size.</summary>
    public const string InvalidSize = "QRCODE_INVALID_SIZE";

    /// <summary>File operation failed.</summary>
    public const string FileOperationFailed = "QRCODE_FILE_OPERATION_FAILED";

    /// <summary>Stream operation failed.</summary>
    public const string StreamOperationFailed = "QRCODE_STREAM_OPERATION_FAILED";

    /// <summary>Could not decode a QR code from the image.</summary>
    public const string ReadFailed = "QRCODE_READ_FAILED";

    /// <summary>Image bytes are missing, corrupt, or not a supported format.</summary>
    public const string InvalidImage = "QRCODE_INVALID_IMAGE";
}