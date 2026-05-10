using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Lyo.Common.Records;
using Lyo.Images.Ocr.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tesseract;
using static Lyo.Images.Ocr.OcrErrorCodes;

namespace Lyo.Images.Ocr.Tesseract;

/// <summary>Tesseract implementation of <see cref="IOcrEngine"/> (not thread-safe internally; serializes calls).</summary>
public sealed class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly OcrEngineOptions _sharedOptions;
    private readonly TesseractOcrEngineOptions _tesseractOptions;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, Lazy<TesseractEngine>> _engines = new(StringComparer.Ordinal);
    private bool _disposed;

    public TesseractOcrEngine(
        OcrEngineOptions sharedOptions,
        TesseractOcrEngineOptions tesseractOptions,
        ILogger<TesseractOcrEngine>? logger = null,
        IMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(sharedOptions);
        ArgumentNullException.ThrowIfNull(tesseractOptions);
        _sharedOptions = sharedOptions;
        _tesseractOptions = tesseractOptions;
        _logger = logger ?? NullLogger<TesseractOcrEngine>.Instance;
        _metrics = sharedOptions.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    /// <inheritdoc />
    public async Task<Result<OcrPageResult>> ReadAsync(Stream imageStream, OcrReadRequest? request = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        if (string.IsNullOrWhiteSpace(_tesseractOptions.TessdataDirectory))
            return Result<OcrPageResult>.Failure(new Error("TessdataDirectory is not configured.", EngineNotConfigured));

        await using var mem = new MemoryStream();
        await imageStream.CopyToAsync(mem, cancellationToken).ConfigureAwait(false);
        var bytes = mem.ToArray();
        if (bytes.Length == 0)
            return Result<OcrPageResult>.Failure(new Error("Image stream is empty.", ImageEmpty));

        var languages = string.IsNullOrWhiteSpace(request?.Languages) ? _sharedOptions.DefaultLanguages : request!.Languages!;
        var psm = request?.PageSegmentationMode ?? _sharedOptions.DefaultPageSegmentationMode;
        var minConf = request?.MinimumConfidencePercent;

        var sw = Stopwatch.StartNew();
        try {
            var result = await Task.Run(() => ReadSync(bytes, languages, psm, minConf, cancellationToken), cancellationToken).ConfigureAwait(false);
            sw.Stop();
            if (!_sharedOptions.EnableMetrics)
                return result;

            _metrics.RecordHistogram(OcrMetrics.ReadDurationMs, sw.Elapsed.TotalMilliseconds);
            if (result.IsSuccess)
                _metrics.IncrementCounter(OcrMetrics.ReadSuccess);
            else
                _metrics.IncrementCounter(OcrMetrics.ReadFailure);

            return result;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            if (_sharedOptions.EnableMetrics)
                _metrics.IncrementCounter(OcrMetrics.ReadFailure);

            return Result<OcrPageResult>.Failure(ex, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "OCR read failed.");
            if (_sharedOptions.EnableMetrics)
                _metrics.IncrementCounter(OcrMetrics.ReadFailure);

            return Result<OcrPageResult>.Failure(ex, RecognitionFailed);
        }
    }

    private Result<OcrPageResult> ReadSync(byte[] bytes, string languages, OcrPageSegmentationMode psm, int? minConf, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TesseractEngine engine;
        try {
            engine = GetOrCreateEngine(languages);
        }
        catch (Exception ex) when (TryUnwrapDllNotFound(ex, out var dllEx)) {
            _logger.LogError(dllEx, "Native Leptonica/Tesseract library missing for languages {Languages}.", languages);
            return Result<OcrPageResult>.Failure(new Error(
                $"{dllEx!.Message} See Lyo.Images.Ocr.Tesseract README (Linux Leptonica symlink).",
                NativeLibraryNotFound,
                exception: dllEx));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to create Tesseract engine for languages {Languages}.", languages);
            return Result<OcrPageResult>.Failure(ex, EngineNotConfigured);
        }

        _gate.Wait(cancellationToken);
        try {
            using var pix = Pix.LoadFromMemory(bytes);
            var imageWidth = pix.Width;
            var imageHeight = pix.Height;
            using var page = engine.Process(pix, TesseractPageSegModeMapper.ToPageSegMode(psm));
            var fullText = page.GetText() ?? "";

            var words = new List<OcrWord>();
            using var iter = page.GetIterator();
            iter.Begin();
            do {
                if (!iter.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                    continue;

                var text = iter.GetText(PageIteratorLevel.Word)?.Trim() ?? "";
                if (string.IsNullOrEmpty(text))
                    continue;

                var conf = iter.GetConfidence(PageIteratorLevel.Word);
                var confPercent = float.IsNaN(conf) ? (float?)null : conf;
                if (minConf.HasValue && confPercent.HasValue && confPercent.Value < minConf.Value)
                    continue;

                var box = OcrCoordinateTransforms.FromTopLeftDownwardRect(rect.X1, rect.Y1, rect.Width, rect.Height, imageHeight);
                words.Add(new OcrWord(text, box, confPercent));
            } while (iter.Next(PageIteratorLevel.Word));

            var tolerance = ComputeLineTolerance(words);
            var lines = OcrLineGrouper.GroupIntoLines(words, tolerance);
            return Result<OcrPageResult>.Success(new OcrPageResult(fullText.TrimEnd(), words, lines, imageWidth, imageHeight));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Tesseract processing failed.");
            return Result<OcrPageResult>.Failure(ex, ImageLoadFailed);
        }
        finally {
            _gate.Release();
        }
    }

    private TesseractEngine GetOrCreateEngine(string languages)
    {
        var dataPath = Path.GetFullPath(_tesseractOptions.TessdataDirectory);
        if (!Directory.Exists(dataPath))
            throw new DirectoryNotFoundException($"Tessdata directory not found: {dataPath}");

        var lazy = _engines.GetOrAdd(
            languages,
            lang => new(() => new TesseractEngine(dataPath, lang, EngineMode.Default), LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    private static bool TryUnwrapDllNotFound(Exception ex, out DllNotFoundException? dllEx)
    {
        dllEx = ex as DllNotFoundException ?? (ex as TargetInvocationException)?.InnerException as DllNotFoundException;
        return dllEx != null;
    }

    private static double ComputeLineTolerance(IReadOnlyList<OcrWord> words)
    {
        if (words.Count == 0)
            return 10;

        var maxH = words.Max(w => Math.Max(1, w.BoundingBoxPixels.Height));
        return Math.Max(8, maxH * 0.35);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _gate.Dispose();
        foreach (var lazy in _engines.Values) {
            if (lazy.IsValueCreated)
                lazy.Value.Dispose();
        }

        _engines.Clear();
    }
}
