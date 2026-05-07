using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Pdf.Models;
using Microsoft.Extensions.Logging;
using PigDoc = UglyToad.PdfPig.PdfDocument;

namespace Lyo.Pdf.Documents;

/// <summary>Caller-owned PdfPig-backed reader plus <see cref="ITextExtractor" />; not thread-safe.</summary>
public sealed class PdfReader : IPdfReader
{
    private readonly string? _filePath;
    private readonly string? _url;
    private byte[] _buffer;
    private int _disposed;
    private PigDoc _pig;

    private ITextExtractor? _text;

    internal PdfServiceOptions Options { get; }

    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>Underlying PdfPig document; disposed when this reader is disposed.</summary>
    public PigDoc PdfPig {
        get {
            EnsureNotDisposed();
            return _pig;
        }
    }

    internal PigDoc Pig => PdfPig;

    internal PdfReader(ILoggerFactory loggerFactory, PdfServiceOptions options, IMetrics metrics, byte[] pdfBytes, PigDoc pig, string? filePath = null, string? url = null)
    {
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _buffer = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        _pig = pig ?? throw new ArgumentNullException(nameof(pig));
        _filePath = filePath;
        _url = url;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> SourceBytes {
        get {
            EnsureNotDisposed();
            return _buffer;
        }
    }

    /// <inheritdoc />
    public IMetrics Metrics { get; }

    /// <inheritdoc />
    public ITextExtractor Text => Volatile.Read(ref _disposed) != 0 ? throw new ObjectDisposedException(nameof(PdfReader)) : _text ??= new PdfTextExtractor(this);

    /// <inheritdoc />
    public PdfInfo GetInfo()
    {
        EnsureNotDisposed();
        var document = _pig;
        var info = document.Information;
        DateTime? creationDate = null;
        DateTime? modifiedDate = null;
        if (!string.IsNullOrWhiteSpace(info.CreationDate) && DateTime.TryParse(info.CreationDate, out var cd))
            creationDate = cd;

        if (!string.IsNullOrWhiteSpace(info.ModifiedDate) && DateTime.TryParse(info.ModifiedDate, out var md))
            modifiedDate = md;

        return new(document.NumberOfPages, info.Title, info.Author, info.Subject, info.Creator, info.Producer, _filePath, _url, creationDate, modifiedDate);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
#if NETSTANDARD2_0
        return default;
#else
        return ValueTask.CompletedTask;
#endif
    }

    public void Dispose() => DisposeCore();

    internal T WithPdf<T>(Func<PigDoc, T> reader)
    {
        ArgumentHelpers.ThrowIfNull(reader);
        EnsureNotDisposed();
        return reader(_pig);
    }

    internal static PdfReader OpenTransient(ILoggerFactory loggerFactory, PdfServiceOptions options, IMetrics metrics, byte[] pdfBytes)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes);
        ArgumentHelpers.ThrowIfGreaterThan(
            pdfBytes.Length, MaxAllowedBytes(options), nameof(pdfBytes), $"PDF size ({pdfBytes.Length} bytes) exceeds max allowed size ({MaxAllowedBytes(options)} bytes).");

        var pig = PigDoc.Open(pdfBytes);
        return new(loggerFactory, options, metrics, pdfBytes, pig);
    }

    internal static long MaxAllowedBytes(PdfServiceOptions options)
        => options.MaxPdfSizeBytes.GetValueOrDefault() > 0 ? options.MaxPdfSizeBytes!.Value : PdfServiceOptions.SuggestedMaxPdfSizeBytes;

    private void EnsureNotDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(PdfReader));
    }

    private void DisposeCore()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try {
            _pig.Dispose();
        }
        finally {
            _pig = null!;
            _buffer = [];
        }
    }
}