namespace Lyo.QRCode.Encoding.Iso;

/// <summary>Raised when the payload is larger than the QR standard allows.</summary>
internal sealed class DataTooLongException : Exception
{
    /// <summary>Builds a <see cref="DataTooLongException" /> with a specified error message.</summary>
    /// <param name="eccLevel">Error-correction level of the QR code.</param>
    /// <param name="encodingMode">Encoding mode of the QR code.</param>
    /// <param name="maxSizeByte">Maximum size allowed for these parameters, in bytes.</param>
    public DataTooLongException(string eccLevel, string encodingMode, int maxSizeByte)
        : base(
            $"The given payload exceeds the maximum size of the QR code standard. The maximum size allowed for the chosen parameters (ECC level={eccLevel}, EncodingMode={encodingMode}) is {maxSizeByte} bytes.") { }

    /// <summary>Builds a <see cref="DataTooLongException" /> that includes a fixed version in the message.</summary>
    /// <param name="eccLevel">Error-correction level of the QR code.</param>
    /// <param name="encodingMode">Encoding mode of the QR code.</param>
    /// <param name="version">Fixed QR version.</param>
    /// <param name="maxSizeByte">Maximum size allowed for these parameters, in bytes.</param>
    public DataTooLongException(string eccLevel, string encodingMode, int version, int maxSizeByte)
        : base(
            $"The given payload exceeds the maximum size of the QR code standard. The maximum size allowed for the chosen parameters (ECC level={eccLevel}, EncodingMode={encodingMode}, FixedVersion={version}) is {maxSizeByte} bytes.") { }
}