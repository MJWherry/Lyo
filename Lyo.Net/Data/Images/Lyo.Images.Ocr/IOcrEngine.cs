using Lyo.Images.Ocr.Models;
using Lyo.Result;

namespace Lyo.Images.Ocr;

/// <summary>Pluggable OCR engine that reads encoded raster streams (PNG, JPEG, etc.).</summary>
public interface IOcrEngine
{
    /// <summary>Runs OCR on an image stream (must be rewindable if the implementation reads it more than once).</summary>
    Task<Result<OcrPageResult>> ReadAsync(Stream imageStream, OcrReadRequest? request = null, CancellationToken cancellationToken = default);
}