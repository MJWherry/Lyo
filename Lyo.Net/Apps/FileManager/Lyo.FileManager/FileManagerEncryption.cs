using System.Text;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.FileManager;

/// <summary>Picks AES-SIV, AES-GCM, or ChaCha20-Poly1305 for one local store. An upload can override the algorithm for that save.</summary>
internal sealed class FileManagerEncryption : ITwoKeyEncryptionService
{
    private readonly ITwoKeyEncryptionService _aesGcm;
    private readonly ITwoKeyEncryptionService _aesSiv;
    private readonly ITwoKeyEncryptionService _chaCha;

    private FileManagerEncryption(IKeyStore store)
    {
        _aesSiv = Wrap(new AesSivEncryptionService(store), store);
        _aesGcm = Wrap(new AesGcmEncryptionService(store), store);
        _chaCha = Wrap(new ChaCha20Poly1305EncryptionService(store), store);
    }

    /// <inheritdoc />
    public string FileExtension => Current().FileExtension;

    /// <inheritdoc />
    public EncryptionAlgorithm? DekAlgorithm => Current().DekAlgorithm;

    /// <inheritdoc />
    public EncryptionAlgorithm? KekAlgorithm => Current().KekAlgorithm;

    /// <inheritdoc />
    public Encoding GetEncryptionEncoding() => Current().GetEncryptionEncoding();

    /// <inheritdoc />
    public void SetEncryptionEncoding(Encoding encoding) => Current().SetEncryptionEncoding(encoding);

    /// <inheritdoc />
    public Encoding GetDecryptionEncoding() => Current().GetDecryptionEncoding();

    /// <inheritdoc />
    public void SetDecryptionEncoding(Encoding encoding) => Current().SetDecryptionEncoding(encoding);

    /// <inheritdoc />
    public string? GetKeyVersion(string keyId) => _aesSiv.GetKeyVersion(keyId);

    /// <inheritdoc />
    public byte[]? GetSaltForVersion(string keyId, string version) => _aesSiv.GetSaltForVersion(keyId, version);

    /// <inheritdoc />
    public TwoKeyEncryptionResult Encrypt(byte[] bytes, string? keyId = null, byte[]? kek = null) => Current().Encrypt(bytes, keyId, kek);

    /// <inheritdoc />
    public byte[] Decrypt(byte[] encryptedData, byte[] encryptedDataEncryptionKey, string? keyId = null, byte[]? kek = null, string? keyVersion = null, byte[]? salt = null)
        => Current().Decrypt(encryptedData, encryptedDataEncryptionKey, keyId, kek, keyVersion, salt);

    /// <inheritdoc />
    public TwoKeyEncryptionResult EncryptString(string text, string? keyId = null, byte[]? kek = null, Encoding? encoding = null)
        => Current().EncryptString(text, keyId, kek, encoding);

    /// <inheritdoc />
    public string DecryptString(
        byte[] encryptedData,
        byte[] encryptedDataEncryptionKey,
        string? keyId = null,
        Encoding? encoding = null,
        byte[]? kek = null,
        string? keyVersion = null,
        byte[]? salt = null)
        => Current().DecryptString(encryptedData, encryptedDataEncryptionKey, keyId, encoding, kek, keyVersion, salt);

    /// <inheritdoc />
    public Task<TwoKeyEncryptionResult> EncryptStreamAsync(Stream input, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024)
        => Current().EncryptStreamAsync(input, keyId, kek, chunkSize);

    /// <inheritdoc />
    public Task DecryptStreamAsync(TwoKeyEncryptionResult result, Stream output, string? keyId = null, byte[]? kek = null)
        => _aesSiv.DecryptStreamAsync(result, output, keyId, kek);

    /// <inheritdoc />
    public Task EncryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
        => Current().EncryptToStreamAsync(input, output, keyId, kek, chunkSize, ct);

    /// <inheritdoc />
    public async Task<EncryptedStreamLayout> ReadEncryptedStreamLayoutAsync(Stream prefix, CancellationToken ct = default)
    {
        var head = new byte[2];
        var read = 0;
        while (read < head.Length) {
            var n = await prefix.ReadAsync(head.AsMemory(read, head.Length - read), ct).ConfigureAwait(false);
            if (n == 0)
                break;

            read += n;
        }

        var service = read >= 2 ? ServiceFor((EncryptionAlgorithm)head[1]) : _aesSiv;
        return await service.ReadEncryptedStreamLayoutAsync(new PrefixStream(head, read, prefix), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DecryptFromChunkAsync(
        ReadOnlyMemory<byte> header,
        Stream ciphertextAtChunk,
        Stream output,
        long chunkIndex,
        int skipPlaintextBytes,
        long maxPlaintextBytes,
        byte[] unwrappedDek,
        CancellationToken ct = default)
        => ServiceFor(header.Length > 1 ? (EncryptionAlgorithm)header.Span[1] : null)
            .DecryptFromChunkAsync(header, ciphertextAtChunk, output, chunkIndex, skipPlaintextBytes, maxPlaintextBytes, unwrappedDek, ct);

    /// <inheritdoc />
    public async Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        var head = new byte[2];
        var read = 0;
        while (read < head.Length) {
            var n = await input.ReadAsync(head.AsMemory(read, head.Length - read), ct).ConfigureAwait(false);
            if (n == 0)
                break;

            read += n;
        }

        var service = read >= 2 ? ServiceFor((EncryptionAlgorithm)head[1]) : _aesSiv;
        await service.DecryptToStreamAsync(new PrefixStream(head, read, input), output, keyId, kek, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
        => Current().EncryptToFileAsync(data, outputPath, keyId, kek, ct);

    /// <inheritdoc />
    public Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
        => Current().EncryptToFileAsync(input, outputPath, keyId, kek, chunkSize, ct);

    /// <inheritdoc />
    public Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
        => _aesSiv.DecryptFromFileAsync(inputPath, keyId, kek, ct);

    /// <inheritdoc />
    public byte[] ReEncryptDek(byte[] encryptedDek, string sourceKeyId, string sourceKeyVersion, string? targetKeyId = null, string? targetKeyVersion = null)
        => _aesSiv.ReEncryptDek(encryptedDek, sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion);

    /// <inheritdoc />
    public Task<byte[]> ReEncryptDekAsync(
        byte[] encryptedDek,
        string sourceKeyId,
        string sourceKeyVersion,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        CancellationToken ct = default)
        => _aesSiv.ReEncryptDekAsync(encryptedDek, sourceKeyId, sourceKeyVersion, targetKeyId, targetKeyVersion, ct);

    /// <summary>Builds a switcher over <paramref name="store" />.</summary>
    public static FileManagerEncryption Create(IKeyStore store)
    {
        ArgumentHelpers.ThrowIfNull(store);
        return new(store);
    }

    private ITwoKeyEncryptionService Current()
    {
        var name = FileManagerUploadScope.EncryptionAlgorithm;
        return string.IsNullOrWhiteSpace(name) ? _aesSiv : ServiceFor(Parse(name));
    }

    private ITwoKeyEncryptionService ServiceFor(EncryptionAlgorithm? algorithm)
        => algorithm switch {
            EncryptionAlgorithm.AesGcm => _aesGcm,
            EncryptionAlgorithm.ChaCha20Poly1305 => _chaCha,
            var _ => _aesSiv
        };

    private static EncryptionAlgorithm? Parse(string name) => Enum.TryParse<EncryptionAlgorithm>(name, true, out var algorithm) ? algorithm : null;

    private static ITwoKeyEncryptionService Wrap(IEncryptionService service, IKeyStore store)
        => service switch {
            AesSivEncryptionService aes => new TwoKeyEncryptionService<AesSivEncryptionService, AesSivEncryptionService>(aes, store),
            AesGcmEncryptionService gcm => new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(gcm, store),
            ChaCha20Poly1305EncryptionService chacha => new TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, ChaCha20Poly1305EncryptionService>(chacha, store),
            var _ => throw new InvalidOperationException("Unsupported encryption service.")
        };

    private sealed class PrefixStream(byte[] prefix, int count, Stream rest) : Stream
    {
        private int _pos;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var copied = CopyPrefix(buffer.AsSpan(offset, count));
            if (copied == count)
                return copied;

            return copied + rest.Read(buffer, offset + copied, count - copied);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var copied = CopyPrefix(buffer.Span);
            if (copied == buffer.Length)
                return copied;

            return copied + await rest.ReadAsync(buffer[copied..], cancellationToken).ConfigureAwait(false);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int CopyPrefix(Span<byte> buffer)
        {
            var n = Math.Min(buffer.Length, count - _pos);
            if (n <= 0)
                return 0;

            prefix.AsSpan(_pos, n).CopyTo(buffer);
            _pos += n;
            return n;
        }
    }
}