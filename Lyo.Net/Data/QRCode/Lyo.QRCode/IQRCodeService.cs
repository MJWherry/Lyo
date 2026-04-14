using Lyo.Common;
using Lyo.QRCode.Models;

namespace Lyo.QRCode;

/// <summary>Service interface for QR code generation operations.</summary>
public interface IQRCodeService
{
    /// <summary>Gets the default QR code format.</summary>
    QRCodeFormat DefaultFormat { get; }

    /// <summary>Generates a QR code from text/data.</summary>
    /// <param name="data">The data to encode in the QR code.</param>
    /// <param name="options">Optional QR code generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The QR code result containing the generated image.</returns>
    Task<Result<QRCodeRequest>> GenerateAsync(string data, QRCodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Generates a QR code using a builder.</summary>
    /// <param name="builder">The QR code builder containing QR code details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The QR code result containing the generated image.</returns>
    Task<Result<QRCodeRequest>> GenerateAsync(QRCodeBuilder builder, CancellationToken ct = default);

    /// <summary>Generates a QR code and writes it to a stream.</summary>
    /// <param name="data">The data to encode in the QR code.</param>
    /// <param name="outputStream">The output stream to write the QR code image to.</param>
    /// <param name="options">Optional QR code generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> GenerateToStreamAsync(string data, Stream outputStream, QRCodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Generates a QR code and saves it to a file.</summary>
    /// <param name="data">The data to encode in the QR code.</param>
    /// <param name="filePath">The file path where the QR code image will be saved.</param>
    /// <param name="options">Optional QR code generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> GenerateToFileAsync(string data, string filePath, QRCodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Generates multiple QR codes in batch.</summary>
    /// <param name="requests">Collection of QR code generation requests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of QR code results.</returns>
    Task<BulkResult<QRCodeRequest, QRCodeResult>> GenerateBatchAsync(IEnumerable<QRCodeRequest> requests, CancellationToken ct = default);

    /// <summary>Decodes a QR code from image bytes (PNG, JPEG, BMP, etc.).</summary>
    Task<Result<QRCodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default);
}