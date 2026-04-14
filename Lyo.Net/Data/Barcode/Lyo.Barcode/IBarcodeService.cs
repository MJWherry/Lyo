using Lyo.Barcode.Models;
using Lyo.Common;

namespace Lyo.Barcode;

/// <summary>Service interface for barcode generation.</summary>
public interface IBarcodeService
{
    BarcodeFormat DefaultFormat { get; }

    Task<Result<BarcodeRequest>> GenerateAsync(string data, BarcodeSymbology symbology, BarcodeOptions? options = null, CancellationToken ct = default);

    Task<Result<BarcodeRequest>> GenerateAsync(BarcodeBuilder builder, CancellationToken ct = default);

    Task<Result<bool>> GenerateToStreamAsync(string data, BarcodeSymbology symbology, Stream outputStream, BarcodeOptions? options = null, CancellationToken ct = default);

    Task<Result<bool>> GenerateToFileAsync(string data, BarcodeSymbology symbology, string filePath, BarcodeOptions? options = null, CancellationToken ct = default);

    Task<BulkResult<BarcodeRequest, BarcodeResult>> GenerateBatchAsync(IEnumerable<BarcodeRequest> requests, CancellationToken ct = default);

    /// <summary>Decodes a barcode from image bytes (PNG, JPEG, BMP, etc.).</summary>
    Task<Result<BarcodeImageReadResult>> ReadFromImageAsync(byte[] imageBytes, CancellationToken ct = default);
}