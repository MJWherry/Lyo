#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
using System.Security.Cryptography;
#endif
using Lyo.Hashing;
using Microsoft.Extensions.Logging;
using SysCryptoHashAlgorithm = System.Security.Cryptography.HashAlgorithm;

namespace Lyo.FileStorage;

/// <summary>
/// Wraps a source stream and hashes data as it is read. On dispose, checks the computed hash against the expected value, but only when the stream was fully drained to EOF.
/// Partial reads (early dispose) skip that check so a truncated consume does not look like a mismatch. Owns and disposes extra inner streams passed to
/// <see cref="WithAdditionalDisposable" />.
/// </summary>
internal sealed class HashVerifyingReadStream : Stream
{
    private readonly List<IDisposable> _additionalDisposables = new();
    private readonly byte[]? _expectedHash;
    private readonly Guid _fileId;
    private readonly HashingStream _hashingStream;
    private readonly Stream _inner;
    private readonly ILogger _logger;
    private readonly bool _throwOnMismatch;
    private bool _disposed;
    private bool _reachedEof;

    public override bool CanRead => _hashingStream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _hashingStream.CanSeek ? _hashingStream.Length : throw new NotSupportedException();

    public override long Position {
        get => _hashingStream.CanSeek ? _hashingStream.Position : throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public HashVerifyingReadStream(Stream inner, SysCryptoHashAlgorithm hashAlgorithm, byte[]? expectedHash, bool throwOnMismatch, ILogger logger, Guid fileId)
    {
        _inner = inner;
        _hashingStream = new(inner, hashAlgorithm);
        _expectedHash = expectedHash;
        _throwOnMismatch = throwOnMismatch;
        _logger = logger;
        _fileId = fileId;
    }

    /// <summary>Registers another disposable (an outer wrapper stream chain, for example) that this stream should dispose.</summary>
    public HashVerifyingReadStream WithAdditionalDisposable(IDisposable disposable)
    {
        _additionalDisposables.Add(disposable);
        return this;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _hashingStream.Read(buffer, offset, count);
        if (n == 0)
            _reachedEof = true;

        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var n = await _hashingStream.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (n == 0)
            _reachedEof = true;

        return n;
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        var n = await _hashingStream.ReadAsync(buffer, ct).ConfigureAwait(false);
        if (n == 0)
            _reachedEof = true;

        return n;
    }
#endif

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush() { }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            _disposed = true;
            try {
                VerifyHash();
            }
            finally {
                _hashingStream.Dispose();
                _inner.Dispose();
                foreach (var d in _additionalDisposables) {
                    try {
                        d.Dispose();
                    }
                    catch {
                        // best effort: one inner dispose should not block the rest
                    }
                }
            }
        }

        base.Dispose(disposing);
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed) {
            _disposed = true;
            try {
                VerifyHash();
            }
            finally {
                await _hashingStream.DisposeAsync().ConfigureAwait(false);
                await _inner.DisposeAsync().ConfigureAwait(false);
                foreach (var d in _additionalDisposables) {
                    try {
                        switch (d) {
                            case IAsyncDisposable asyncDisposable:
                                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                                break;
                            default:
                                d.Dispose();
                                break;
                        }
                    }
                    catch {
                        // best-effort dispose
                    }
                }
            }
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    private void VerifyHash()
    {
        if (_expectedHash == null || _expectedHash.Length == 0)
            return;

        // Only check when the caller drained the stream. A partial read makes the inline hash meaningless.
        if (!_reachedEof)
            return;

        var computedHash = _hashingStream.GetHash();
        if (FixedTimeEqualsHashes(computedHash, _expectedHash))
            return;

        if (_throwOnMismatch) {
            _logger.LogError("Hash mismatch for file {FileId}. File may be corrupted.", _fileId);
            throw new InvalidDataException($"Hash mismatch for file {_fileId}. File may be corrupted.");
        }

        _logger.LogWarning("Hash mismatch for file {FileId}. File may be corrupted.", _fileId);
    }

    private static bool FixedTimeEqualsHashes(byte[]? a, byte[]? b)
    {
        if (a == null || b == null)
            return a == b;

        if (a.Length != b.Length)
            return false;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(a, b);
#else
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];

        return diff == 0;
#endif
    }
}