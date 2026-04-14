using System.Buffers;
using System.Security.Cryptography;
using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>A stream wrapper that computes a hash as data flows through it. This allows computing hashes during streaming operations without buffering entire files in memory.</summary>
public class HashingStream : Stream
{
    private readonly Stream _baseStream;
    private readonly HashAlgorithm _hashAlgorithm;
    private bool _disposed;
    private bool _hashFinalized;

    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => _baseStream.CanWrite;

    public override long Length => _baseStream.Length;

    public override long Position {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public HashingStream(Stream baseStream, HashAlgorithm hashAlgorithm)
    {
        ArgumentHelpers.ThrowIfNull(baseStream, nameof(baseStream));
        ArgumentHelpers.ThrowIfNull(hashAlgorithm, nameof(hashAlgorithm));
        _baseStream = baseStream;
        _hashAlgorithm = hashAlgorithm;
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _baseStream.Flush();
    }

    public override async Task FlushAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        await _baseStream.FlushAsync(ct).ConfigureAwait(false);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = _baseStream.Read(buffer, offset, count);
        if (bytesRead > 0 && !_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, bytesRead, null, 0);

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        var bytesRead = _baseStream.Read(buffer);
        if (bytesRead > 0 && !_hashFinalized)
            TransformBlockFromSpan(buffer[..bytesRead]);

        return bytesRead;
    }
#endif

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (bytesRead > 0 && !_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, bytesRead, null, 0);

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var bytesRead = await _baseStream.ReadAsync(buffer, ct).ConfigureAwait(false);
        if (bytesRead > 0 && !_hashFinalized)
            TransformBlockFromSpan(buffer.Span[..bytesRead]);

        return bytesRead;
    }
#endif

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _baseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        _baseStream.Write(buffer, offset, count);
        if (!_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, count, null, 0);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _baseStream.Write(buffer);
        if (!_hashFinalized && buffer.Length > 0)
            TransformBlockFromSpan(buffer);
    }
#endif

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        await _baseStream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (!_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, count, null, 0);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _baseStream.WriteAsync(buffer, ct).ConfigureAwait(false);
        if (!_hashFinalized && buffer.Length > 0)
            TransformBlockFromSpan(buffer.Span);
    }
#endif

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    private void TransformBlockFromSpan(ReadOnlySpan<byte> span)
    {
        var length = span.Length;
        if (length == 0)
            return;

        var rented = ArrayPool<byte>.Shared.Rent(length);
        try {
            span.CopyTo(rented);
            _hashAlgorithm.TransformBlock(rented, 0, length, null, 0);
        }
        finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
#endif

    /// <summary>Gets the computed hash. Call this after all data has been written/read. This method can be called multiple times and will return the same hash.</summary>
    public byte[] GetHash()
    {
        if (!_hashFinalized) {
            _hashAlgorithm.TransformFinalBlock([], 0, 0);
            _hashFinalized = true;
        }

        return _hashAlgorithm.Hash ?? [];
    }

    /// <summary>Gets the computed hash as a hexadecimal string.</summary>
    public string GetHashString()
    {
        var hash = GetHash();
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            if (!_hashFinalized) {
                try {
                    _hashAlgorithm.TransformFinalBlock([], 0, 0);
                }
                catch {
                    // Ignore errors during finalization on dispose
                }
            }

            _hashAlgorithm.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}