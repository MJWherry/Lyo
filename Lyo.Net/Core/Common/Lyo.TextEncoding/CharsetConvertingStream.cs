using System.Buffers;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.TextEncoding;

/// <summary>
/// Write-through stream that decodes written bytes with <see cref="From" /> and encodes to <see cref="To" /> on the inner stream. Read is not supported (v1). Suitable for
/// nesting ahead of compression or other write sinks.
/// </summary>
public sealed class CharsetConvertingStream : Stream
{
    private const int ChunkSize = 4096;
    private readonly Stream _inner;
    private readonly bool _leaveOpen;
    private readonly Decoder _decoder;
    private readonly Encoder _encoder;
    private readonly char[] _charBuf;
    private readonly byte[] _byteOut;
    private bool _disposed;

    /// <summary>Create a converting write stream.</summary>
    public CharsetConvertingStream(Stream inner, Encoding from, Encoding to, bool leaveOpen = true, CharsetEncodingOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(inner);
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        options ??= CharsetEncodingOptions.Default;
        from = CharsetEncoding.ApplyFallbacks(from, options);
        to = CharsetEncoding.ApplyFallbacks(to, options);
        _inner = inner;
        _leaveOpen = leaveOpen;
        From = from;
        To = to;
        _decoder = from.GetDecoder();
        _encoder = to.GetEncoder();
        _charBuf = ArrayPool<char>.Shared.Rent(from.GetMaxCharCount(ChunkSize));
        _byteOut = ArrayPool<byte>.Shared.Rent(to.GetMaxByteCount(_charBuf.Length));
    }

    /// <summary>Source encoding for written bytes.</summary>
    public Encoding From { get; }

    /// <summary>Destination encoding written to the inner stream.</summary>
    public Encoding To { get; }

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => _inner.CanWrite;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentHelpers.ThrowIfNull(buffer);
        WriteCore(buffer.AsSpan(offset, count));
    }

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer) => WriteCore(buffer);

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var offset = 0;
        while (offset < buffer.Length) {
            var take = Math.Min(ChunkSize, buffer.Length - offset);
            var chunk = buffer.Slice(offset, take);
            var chars = _decoder.GetChars(chunk.Span, _charBuf, flush: false);
            var outLen = _encoder.GetBytes(_charBuf.AsSpan(0, chars), _byteOut, flush: false);
            if (outLen > 0)
                await _inner.WriteAsync(_byteOut.AsMemory(0, outLen), cancellationToken).ConfigureAwait(false);
            offset += take;
        }
    }
#endif

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            try {
                FlushEncoder();
            }
            finally {
                ArrayPool<char>.Shared.Return(_charBuf);
                ArrayPool<byte>.Shared.Return(_byteOut);
                if (!_leaveOpen)
                    _inner.Dispose();
            }
        }

        _disposed = true;
        base.Dispose(disposing);
    }

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed) {
            try {
                var chars = _decoder.GetChars(ReadOnlySpan<byte>.Empty, _charBuf, flush: true);
                var outLen = _encoder.GetBytes(_charBuf.AsSpan(0, chars), _byteOut, flush: true);
                if (outLen > 0)
                    await _inner.WriteAsync(_byteOut.AsMemory(0, outLen)).ConfigureAwait(false);
            }
            finally {
                ArrayPool<char>.Shared.Return(_charBuf);
                ArrayPool<byte>.Shared.Return(_byteOut);
                if (!_leaveOpen)
                    await _inner.DisposeAsync().ConfigureAwait(false);
            }

            _disposed = true;
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    private void WriteCore(ReadOnlySpan<byte> buffer)
    {
        var offset = 0;
        while (offset < buffer.Length) {
            var take = Math.Min(ChunkSize, buffer.Length - offset);
            var chunk = buffer.Slice(offset, take);
#if NET5_0_OR_GREATER
            var chars = _decoder.GetChars(chunk, _charBuf, flush: false);
            var outLen = _encoder.GetBytes(_charBuf.AsSpan(0, chars), _byteOut, flush: false);
#else
            var tmp = chunk.ToArray();
            var chars = _decoder.GetChars(tmp, 0, tmp.Length, _charBuf, 0, false);
            var outLen = _encoder.GetBytes(_charBuf, 0, chars, _byteOut, 0, false);
#endif
            if (outLen > 0)
                _inner.Write(_byteOut, 0, outLen);

            offset += take;
        }
    }

    private void FlushEncoder()
    {
#if NET5_0_OR_GREATER
        var chars = _decoder.GetChars(ReadOnlySpan<byte>.Empty, _charBuf, flush: true);
        var outLen = _encoder.GetBytes(_charBuf.AsSpan(0, chars), _byteOut, flush: true);
#else
        var chars = _decoder.GetChars([], 0, 0, _charBuf, 0, true);
        var outLen = _encoder.GetBytes(_charBuf, 0, chars, _byteOut, 0, true);
#endif
        if (outLen > 0)
            _inner.Write(_byteOut, 0, outLen);
    }
}