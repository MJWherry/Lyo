using Lyo.QRCode.Models;
using Lyo.Result;

namespace Lyo.QRCode;

/// <summary>Generates and decodes QR codes.</summary>
public interface IQRCodeService
{
    /// <summary>Default QR code format.</summary>
    QRCodeFormat DefaultFormat { get; }

    /// <summary>Builds a QR code from text or data.</summary>
    /// <param name="data">Payload to encode in the QR code.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result that holds the generated image.</returns>
    Task<Result<QRCodeRequest>> GenerateAsync(string data, QRCodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Builds a QR code from a builder.</summary>
    /// <param name="builder">Builder that holds the QR details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result that holds the generated image.</returns>
    Task<Result<QRCodeRequest>> GenerateAsync(QRCodeBuilder builder, CancellationToken ct = default);

    /// <summary>Builds a QR code and writes the image to a stream.</summary>
    /// <param name="data">Payload to encode in the QR code.</param>
    /// <param name="outputStream">Stream that receives the QR image.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> GenerateToStreamAsync(string data, Stream outputStream, QRCodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Builds a QR code and writes the image to a file.</summary>
    /// <param name="data">Payload to encode in the QR code.</param>
    /// <param name="filePath">Path where the QR image is written.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> GenerateToFileAsync(string data, string filePath, QRCodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Builds several QR codes in one batch.</summary>
    /// <param name="requests">Generation requests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Results for each request.</returns>
    Task<BulkResult<QRCodeRequest, QRCodeResult>> GenerateBatchAsync(IEnumerable<QRCodeRequest> requests, CancellationToken ct = default);

    /// <summary>Decodes a QR code from image bytes (PNG, JPEG, BMP, and similar).</summary>
    /// <param name="imageBytes">Raster image that contains a readable QR symbol.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Decoded text and format metadata when the read succeeds.</returns>
    Task<Result<QRCodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default);
}