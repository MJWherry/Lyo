using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>
/// A read-only stream that generates a deterministic byte sequence of a fixed <see cref="Length" /> from a seed. Bytes are produced on demand so the full payload is never
/// allocated. Suitable for large streaming benchmarks and tests.
/// </summary>
/// <remarks>
/// Seeking to an arbitrary position resets the PRNG and skips forward; prefer sequential reads and <c>Seek(0)</c> for best performance. The same length and seed always yield the
/// same sequence.
/// </remarks>
public sealed class DeterministicPayloadStream : Stream
{
    private readonly long _length;
    private readonly int _seed;
    private bool _disposed;
    private long _position;
    private Random _rng;

    /// <summary>Creates a stream that yields <paramref name="length" /> bytes from <paramref name="seed" />.</summary>
    /// <param name="length">Exact number of bytes available to read; must be non-negative.</param>
    /// <param name="seed">PRNG seed; identical seeds produce identical sequences for the same length.</param>
    public DeterministicPayloadStream(long length, int seed)
    {
        ArgumentHelpers.ThrowIfNegative(length);
        _length = length;
        _seed = seed;
        _rng = new Random(seed);
    }

    /// <inheritdoc />
    public override bool CanRead => !_disposed;

    /// <inheritdoc />
    public override bool CanSeek => !_disposed;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length {
        get {
            ThrowIfDisposed();
            return _length;
        }
    }

    /// <inheritdoc />
    public override long Position {
        get {
            ThrowIfDisposed();
            return _position;
        }
        set => Seek(value, SeekOrigin.Begin);
    }

    /// <inheritdoc />
    public override void Flush() => ThrowIfDisposed();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        if (count == 0 || _position >= _length)
            return 0;

        var toRead = (int)Math.Min(count, _length - _position);
        var fill = new byte[toRead];
        _rng.NextBytes(fill);
        Buffer.BlockCopy(fill, 0, buffer, offset, toRead);
        _position += toRead;
        return toRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        if (buffer.IsEmpty || _position >= _length)
            return 0;

        var toRead = (int)Math.Min(buffer.Length, _length - _position);
        _rng.NextBytes(buffer[..toRead]);
        _position += toRead;
        return toRead;
    }
#endif

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        var target = origin switch {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (target < 0 || target > _length)
            throw new IOException("Attempted to seek outside the stream bounds.");

        if (target < _position) {
            _rng = new Random(_seed);
            Skip(target);
        }
        else if (target > _position)
            Skip(target - _position);

        _position = target;
        return _position;
    }

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException($"{nameof(DeterministicPayloadStream)} is fixed-length.");

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException($"{nameof(DeterministicPayloadStream)} is read-only.");

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        base.Dispose(disposing);
    }

    private void Skip(long count)
    {
        if (count <= 0)
            return;

        var buffer = new byte[(int)Math.Min(count, 64 * 1024)];
        var remaining = count;
        while (remaining > 0) {
            var n = (int)Math.Min(remaining, buffer.Length);
            if (n == buffer.Length)
                _rng.NextBytes(buffer);
            else {
                var slice = new byte[n];
                _rng.NextBytes(slice);
            }

            remaining -= n;
        }
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(DeterministicPayloadStream));
}
