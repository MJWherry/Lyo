using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Hashing.Internal;

namespace Lyo.Hashing;

/// <summary>A stream wrapper that updates a non-cryptographic <see cref="ChecksumAlgorithm" /> as data flows through it (parallels <see cref="HashingStream" />).</summary>
public sealed class ChecksumStream : Stream
{
    private readonly Stream _baseStream;

    private readonly ChecksumCalculator _calculator;

    private bool _disposed;

    /// <summary>Wraps <paramref name="baseStream" />, accumulating <paramref name="algorithm" /> over every byte read or written.</summary>
    public ChecksumStream(Stream baseStream, ChecksumAlgorithm algorithm)
    {
        ArgumentHelpers.ThrowIfNull(baseStream);
        _baseStream = baseStream;
        _calculator = ChecksumCalculator.Create(algorithm);
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
        if (bytesRead > 0)
            _calculator.Append(buffer.AsSpan(offset, bytesRead));

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        var bytesRead = _baseStream.Read(buffer);
        if (bytesRead > 0)
            _calculator.Append(buffer[..bytesRead]);

        return bytesRead;
    }
#endif

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        HashStreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (bytesRead > 0)
            _calculator.Append(buffer.AsSpan(offset, bytesRead));

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var bytesRead = await _baseStream.ReadAsync(buffer, ct).ConfigureAwait(false);
        if (bytesRead > 0)
            _calculator.Append(buffer.Span[..bytesRead]);

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
        if (count > 0)
            _calculator.Append(buffer.AsSpan(offset, count));
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _baseStream.Write(buffer);
        if (buffer.Length > 0)
            _calculator.Append(buffer);
    }
#endif

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        HashStreamValidation.ValidateWriteBuffer(buffer, offset, count);
        await _baseStream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (count > 0)
            _calculator.Append(buffer.AsSpan(offset, count));
    }

#if NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _baseStream.WriteAsync(buffer, ct).ConfigureAwait(false);
        if (buffer.Length > 0)
            _calculator.Append(buffer.Span);
    }
#endif

    /// <summary>Gets the checksum of all data seen so far as big-endian bytes (4 bytes for 32-bit checksums, 8 for CRC-64). May be called repeatedly.</summary>
    public byte[] GetChecksum() => Checksummer.ToBigEndianBytes(_calculator.GetCurrentValue(), _calculator.HashSizeInBytes);

    /// <summary>Gets the checksum of all data seen so far as a raw numeric value (32-bit results occupy the low bits).</summary>
    public ulong GetChecksumValue() => _calculator.GetCurrentValue();

    /// <summary>Hexadecimal of <see cref="GetChecksum" /> with the chosen letter case for A–F.</summary>
    public string GetChecksumHex(TextLetterCase letterCase = TextLetterCase.Upper) => HexEncoding.ToHexString(GetChecksum(), letterCase);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
            _disposed = true;

        base.Dispose(disposing);
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(ChecksumStream));
}