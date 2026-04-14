using Lyo.Streams;
using Microsoft.Extensions.Logging;
using SysCryptoHashAlgorithm = System.Security.Cryptography.HashAlgorithm;

namespace Lyo.FileStorage;

/// <summary>
/// Wraps a source stream and computes a hash inline as data is read.
/// On dispose, verifies the computed hash against the expected hash.
/// This enables true streaming reads with deferred hash verification — no buffering required.
/// </summary>
internal sealed class HashVerifyingReadStream : Stream
{
    private readonly HashingStream _hashingStream;
    private readonly byte[]? _expectedHash;
    private readonly bool _throwOnMismatch;
    private readonly ILogger _logger;
    private readonly Guid _fileId;
    private readonly SysCryptoHashAlgorithm _hashAlgorithm;
    private bool _disposed;

    public HashVerifyingReadStream(
        Stream inner,
        SysCryptoHashAlgorithm hashAlgorithm,
        byte[]? expectedHash,
        bool throwOnMismatch,
        ILogger logger,
        Guid fileId)
    {
        _hashAlgorithm = hashAlgorithm;
        _hashingStream = new HashingStream(inner, hashAlgorithm);
        _expectedHash = expectedHash;
        _throwOnMismatch = throwOnMismatch;
        _logger = logger;
        _fileId = fileId;
    }

    public override bool CanRead => _hashingStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _hashingStream.CanSeek ? _hashingStream.Length : throw new NotSupportedException();

    public override long Position
    {
        get => _hashingStream.CanSeek ? _hashingStream.Position : throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => _hashingStream.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => _hashingStream.ReadAsync(buffer, offset, count, ct);

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        => _hashingStream.ReadAsync(buffer, ct);
#endif

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() { }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            _disposed = true;
            VerifyHash();
            _hashingStream.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed) {
            _disposed = true;
            VerifyHash();
            await _hashingStream.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    private void VerifyHash()
    {
        if (_expectedHash == null || _expectedHash.Length == 0)
            return;

        var computedHash = _hashingStream.GetHash();
        if (ByteArraysEqual(computedHash, _expectedHash))
            return;

        if (_throwOnMismatch)
            _logger.LogError("Hash mismatch for file {FileId}. File may be corrupted.", _fileId);
        else
            _logger.LogWarning("Hash mismatch for file {FileId}. File may be corrupted.", _fileId);
    }

    private static bool ByteArraysEqual(byte[]? a, byte[]? b)
    {
        if (a == null || b == null) return a == b;
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++) {
            if (a[i] != b[i]) return false;
        }

        return true;
    }
}
