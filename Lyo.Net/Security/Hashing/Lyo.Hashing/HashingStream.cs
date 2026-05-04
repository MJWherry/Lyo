using System.Security.Cryptography;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Hashing;

/// <summary>A stream wrapper that computes a hash as data flows through it.</summary>
public sealed class HashingStream : Stream
{
    private readonly Stream _baseStream;

    private readonly HashAlgorithm _hashAlgorithm;

    private bool _disposed;

    private bool _hashFinalized;

    /// <inheritdoc />
    public HashingStream(Stream baseStream, HashAlgorithm hashAlgorithm)
    {
        ArgumentHelpers.ThrowIfNull(baseStream);
        ArgumentHelpers.ThrowIfNull(hashAlgorithm);
        _baseStream = baseStream;
        _hashAlgorithm = hashAlgorithm;
    }

    /// <inheritdoc />
    public override bool CanRead => _baseStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _baseStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _baseStream.CanWrite;

    /// <inheritdoc />
    public override long Length => _baseStream.Length;

    /// <inheritdoc />
    public override long Position {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        ThrowIfDisposed();
        _baseStream.Flush();
    }

    /// <inheritdoc />
    public override async Task FlushAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        await _baseStream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        HashStreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = _baseStream.Read(buffer, offset, count);
        if (bytesRead > 0 && !_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, bytesRead, null, 0);

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        var bytesRead = _baseStream.Read(buffer);
        if (bytesRead > 0 && !_hashFinalized)
            TransformBlockFromSpan(buffer[..bytesRead]);

        return bytesRead;
    }
#endif

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        HashStreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (bytesRead > 0 && !_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, bytesRead, null, 0);

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var bytesRead = await _baseStream.ReadAsync(buffer, ct).ConfigureAwait(false);
        if (bytesRead > 0 && !_hashFinalized)
            TransformBlockFromSpan(buffer.Span[..bytesRead]);

        return bytesRead;
    }
#endif

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _baseStream.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _baseStream.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        HashStreamValidation.ValidateWriteBuffer(buffer, offset, count);
        _baseStream.Write(buffer, offset, count);
        if (!_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, count, null, 0);
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _baseStream.Write(buffer);
        if (!_hashFinalized && buffer.Length > 0)
            TransformBlockFromSpan(buffer);
    }
#endif

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        HashStreamValidation.ValidateWriteBuffer(buffer, offset, count);
        await _baseStream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (!_hashFinalized)
            _hashAlgorithm.TransformBlock(buffer, offset, count, null, 0);
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _baseStream.WriteAsync(buffer, ct).ConfigureAwait(false);
        if (!_hashFinalized && buffer.Length > 0)
            TransformBlockFromSpan(buffer.Span);
    }
#endif

    /// <summary>Gets the computed hash after all bytes have passed through.</summary>
    public byte[] GetHash()
    {
        if (_hashFinalized)
            return _hashAlgorithm.Hash!;

        _hashAlgorithm.TransformFinalBlock([], 0, 0);
        _hashFinalized = true;
        return _hashAlgorithm.Hash!;
    }

    /// <summary>Uppercase hexadecimal of <see cref="GetHash" /> (legacy default for this type).</summary>
    public string GetHashString() => HexEncoding.ToHexString(GetHash());

    /// <summary>Hexadecimal of <see cref="GetHash" /> with chosen letter case for A–F.</summary>
    public string GetHashHex(TextLetterCase letterCase) => HexEncoding.ToHexString(GetHash(), letterCase);

#if NETSTANDARD2_1_OR_GREATER
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

    /// <inheritdoc />
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

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(HashingStream));
}