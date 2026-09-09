using Lyo.Barcode.Models;
using Lyo.Result;

namespace Lyo.Barcode;

/// <summary>Contract for encoding payloads into barcode images and reading symbols back from rasters.</summary>
public interface IBarcodeService
{
    /// <summary>Format applied when a request does not set <see cref="BarcodeOptions.Format" />.</summary>
    BarcodeFormat DefaultFormat { get; }

    /// <summary>Encodes <paramref name="data" /> with <paramref name="symbology" /> and returns the rendered image.</summary>
    /// <param name="data">Text to encode. Allowed characters depend on the symbology.</param>
    /// <param name="symbology">Which barcode family to draw.</param>
    /// <param name="options">Raster or SVG settings. Omit to use the service defaults.</param>
    /// <param name="ct">Token that cancels the render.</param>
    /// <returns>Outcome of the render. A successful path typically yields a <see cref="BarcodeResult" /> holding image bytes.</returns>
    Task<Result<BarcodeRequest>> GenerateAsync(string data, BarcodeSymbology symbology, BarcodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Renders a barcode from a filled-in <see cref="BarcodeBuilder" />.</summary>
    /// <param name="builder">Builder already configured with payload and options.</param>
    /// <param name="ct">Token that cancels the render.</param>
    Task<Result<BarcodeRequest>> GenerateAsync(BarcodeBuilder builder, CancellationToken ct = default);

    /// <summary>Renders a barcode and copies the bytes into <paramref name="outputStream" />.</summary>
    Task<Result<bool>> GenerateToStreamAsync(string data, BarcodeSymbology symbology, Stream outputStream, BarcodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Renders a barcode and writes the bytes to <paramref name="filePath" />.</summary>
    Task<Result<bool>> GenerateToFileAsync(string data, BarcodeSymbology symbology, string filePath, BarcodeOptions? options = null, CancellationToken ct = default);

    /// <summary>Renders a set of barcodes in a single call.</summary>
    /// <param name="requests">Each item supplies payload, symbology, and optional per-item options.</param>
    /// <param name="ct">Token that cancels the batch.</param>
    Task<BulkResult<BarcodeRequest, BarcodeResult>> GenerateBatchAsync(IEnumerable<BarcodeRequest> requests, CancellationToken ct = default);

    /// <summary>Reads a barcode out of raster bytes such as PNG, JPEG, or BMP.</summary>
    /// <param name="imageBytes">Image that contains a scannable symbol.</param>
    /// <param name="ct">Token that cancels the decode.</param>
    /// <returns>On success, the decoded text and the decoder's format name.</returns>
    Task<Result<BarcodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default);
}