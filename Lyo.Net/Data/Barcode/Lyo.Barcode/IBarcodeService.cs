using Lyo.Barcode.Models;
using Lyo.Result;

namespace Lyo.Barcode;

/// <summary>Service interface for barcode generation.</summary>
public interface IBarcodeService
{
    /// <summary>Default output format when <see cref="BarcodeOptions.Format" /> is not overridden.</summary>
    BarcodeFormat DefaultFormat { get; }

    /// <summary>Generates a barcode image for the given payload and symbology.</summary>
    /// <param name="data">String to encode (symbology-specific charset).</param>
    /// <param name="symbology">Barcode type.</param>
    /// <param name="options">Raster/SVG options; null uses service defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generation outcome; concrete success types include <see cref="BarcodeResult" /> with image bytes.</returns>
    Task<Result<BarcodeRequest>> GenerateAsync(string data, BarcodeSymbology symbology, BarcodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Generates a barcode from a fluent <see cref="BarcodeBuilder" />.</summary>
    /// <param name="builder">Configured builder.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<BarcodeRequest>> GenerateAsync(BarcodeBuilder builder, CancellationToken ct = default);

    /// <summary>Writes generated barcode bytes directly to <paramref name="outputStream" />.</summary>
    Task<Result<bool>> GenerateToStreamAsync(string data, BarcodeSymbology symbology, Stream outputStream, BarcodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Writes generated barcode bytes to <paramref name="filePath" />.</summary>
    Task<Result<bool>> GenerateToFileAsync(string data, BarcodeSymbology symbology, string filePath, BarcodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Generates many barcodes in one call.</summary>
    /// <param name="requests">Per-item data, symbology, and optional options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BulkResult<BarcodeRequest, BarcodeResult>> GenerateBatchAsync(IEnumerable<BarcodeRequest> requests, CancellationToken ct = default);

    /// <summary>Decodes a barcode from image bytes (PNG, JPEG, BMP, etc.).</summary>
    /// <param name="imageBytes">Raster image containing a readable symbol.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Decoded text and format name on success.</returns>
    Task<Result<BarcodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default);
}