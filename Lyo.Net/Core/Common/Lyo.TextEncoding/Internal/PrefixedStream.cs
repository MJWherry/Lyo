using Lyo.Exceptions;

namespace Lyo.TextEncoding.Internal;

/// <summary>Reads <see cref="Prefix" /> first, then the remainder of <see cref="Inner" />.</summary>
internal sealed class PrefixedStream : Stream
{
    private readonly Stream _inner;
    private readonly bool _leaveOpen;
    private byte[]? _prefix;
    private int _prefixOffset;
    private bool _disposed;

    public PrefixedStream(byte[] prefix, Stream inner, bool leaveOpen = true)
    {
        ArgumentHelpers.ThrowIfNull(prefix);
        ArgumentHelpers.ThrowIfNull(inner);
        _prefix = prefix;
        _inner = inner;
        _leaveOpen = leaveOpen;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentHelpers.ThrowIfNull(buffer);
        var total = 0;
        if (_prefix is { } prefix && _prefixOffset < prefix.Length) {
            var fromPrefix = Math.Min(count, prefix.Length - _prefixOffset);
            Buffer.BlockCopy(prefix, _prefixOffset, buffer, offset, fromPrefix);
            _prefixOffset += fromPrefix;
            total += fromPrefix;
            offset += fromPrefix;
            count -= fromPrefix;
            if (_prefixOffset >= prefix.Length)
                _prefix = null;
        }

        if (count > 0)
            total += _inner.Read(buffer, offset, count);

        return total;
    }

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var total = 0;
        if (_prefix is { } prefix && _prefixOffset < prefix.Length) {
            var fromPrefix = Math.Min(buffer.Length, prefix.Length - _prefixOffset);
            prefix.AsSpan(_prefixOffset, fromPrefix).CopyTo(buffer.Span);
            _prefixOffset += fromPrefix;
            total += fromPrefix;
            buffer = buffer[fromPrefix..];
            if (_prefixOffset >= prefix.Length)
                _prefix = null;
        }

        if (!buffer.IsEmpty)
            total += await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

        return total;
    }
#endif

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing && !_leaveOpen)
            _inner.Dispose();

        _disposed = true;
        base.Dispose(disposing);
    }
}