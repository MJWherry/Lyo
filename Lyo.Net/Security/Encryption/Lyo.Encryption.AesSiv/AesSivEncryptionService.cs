using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.Keystore;

namespace Lyo.Encryption.AesSiv;

/// <summary>
/// AES-SIV (RFC 5297) via Dorssel.Security.Cryptography.AesExtra. The 16-byte synthetic IV is stored in the header "nonce" field; ciphertext is the CTR payload only (no
/// separate tag). V1 uses empty associated data (<see cref="ReadOnlySpan{T}.Empty" />).
/// </summary>
public class AesSivEncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    private const int SivSize = 16;

    public AesSivKeySizeBits AesSivKeySize { get; }

    public AesSivEncryptionService(IKeyStore keyStore)
        : this(keyStore, AesSivKeySizeBits.Bits256) { }

    public AesSivEncryptionService(IKeyStore keyStore, AesSivKeySizeBits keySize)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoAesSiv.DefaultExtension
            }, keyStore)
        => AesSivKeySize = keySize;

    public AesSivEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore)
        : this(options, keyStore, AesSivKeySizeBits.Bits256) { }

    public AesSivEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore, AesSivKeySizeBits keySize)
        : base(options, keyStore)
        => AesSivKeySize = keySize;

    public int RequiredKeyBytes => AesSivKeySize.GetKeyLengthBytes();

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesSiv;

    /// <summary>Creates a per-stream AES-SIV cipher bound to <paramref name="key" /> for the deterministic streaming chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        ArgumentHelpers.ThrowIf(key.Length != RequiredKeyBytes, $"AES-SIV key must be exactly {RequiredKeyBytes} bytes for the configured key size.", nameof(key));
        return new AesSivStreamCryptor(key);
    }

    /// <inheritdoc cref="IEncryptionService.Encrypt(ReadOnlySpan{byte}, string?, byte[], byte[])" />
    public override byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        if (key != null)
            ValidateKey(key);

        byte[]? actualKey = null;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            ValidateKey(actualKey);
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");

        var total = new byte[SivSize + plaintext.Length];
        using (var siv = new Dorssel.Security.Cryptography.AesSiv(actualKey!))
            siv.Encrypt(plaintext, total.AsSpan(), associatedData is { Length: > 0 } ? associatedData.AsSpan() : ReadOnlySpan<byte>.Empty);

        var sivBlock = new byte[SivSize];
        Buffer.BlockCopy(total, 0, sivBlock, 0, SivSize);
        var body = new byte[plaintext.Length];
        if (plaintext.Length > 0)
            Buffer.BlockCopy(total, SivSize, body, 0, plaintext.Length);

        try {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
            var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
            bw.Write(keyIdBytes.Length);
            if (keyIdBytes.Length > 0)
                bw.Write(keyIdBytes);

            bw.Write(keyVersion ?? "");
            bw.Write(sivBlock.Length);
            bw.Write(sivBlock);
            bw.Write(body);
            return ms.ToArray();
        }
        finally {
            SecurityUtilities.Clear(sivBlock);
            SecurityUtilities.Clear(total);
        }
    }

    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize);
        if (key != null)
            ValidateKey(key);

        byte[]? actualKey = null;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            ValidateKey(actualKey);
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");

        var total = new byte[SivSize + bytes.Length];
        using (var siv = new Dorssel.Security.Cryptography.AesSiv(actualKey!))
            siv.Encrypt(bytes, total.AsSpan(), associatedData is { Length: > 0 } ? associatedData.AsSpan() : ReadOnlySpan<byte>.Empty);

        var sivBlock = new byte[SivSize];
        Buffer.BlockCopy(total, 0, sivBlock, 0, SivSize);
        var body = new byte[bytes.Length];
        if (bytes.Length > 0)
            Buffer.BlockCopy(total, SivSize, body, 0, body.Length);

        try {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
            var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
            bw.Write(keyIdBytes.Length);
            if (keyIdBytes.Length > 0)
                bw.Write(keyIdBytes);

            bw.Write(keyVersion ?? "");
            bw.Write(sivBlock.Length);
            bw.Write(sivBlock);
            bw.Write(body);
            return ms.ToArray();
        }
        finally {
            SecurityUtilities.Clear(sivBlock);
            SecurityUtilities.Clear(total);
        }
    }

    public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 27;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, minEncryptedSize, Options.MaxInputSize);
        using var ms = new MemoryStream(encryptedBytes);
        return DecryptFromStream(ms, keyId, key, associatedData);
    }

    /// <inheritdoc cref="IEncryptionService.Decrypt(byte[], int, int, string?, byte[], byte[])" />
    public override byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => DecryptChunk(buffer, offset, count, keyId, key, associatedData);

    protected override byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 27;
        ArgumentHelpers.ThrowIfNotInRange(count, minEncryptedSize, Options.MaxInputSize);
        using var ms = new MemoryStream(buffer, offset, count, false);
        return DecryptFromStream(ms, keyId, key, associatedData);
    }

    private byte[] DecryptFromStream(MemoryStream ms, string? keyId, byte[]? key, byte[]? associatedData)
    {
        using var br = new BinaryReader(ms);
        var firstByte = br.ReadByte();
        var expectedFormatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        if (firstByte != expectedFormatVersion)
            throw new InvalidDataException($"Invalid encrypted data format: expected format version {expectedFormatVersion}, got {firstByte}.");

        var keyIdLength = br.ReadInt32();
        if (keyIdLength < 0 || keyIdLength > 1024)
            throw new InvalidDataException($"Invalid key ID length: {keyIdLength}. Maximum allowed: 1024 bytes.");

        string? headerKeyId = null;
        if (keyIdLength > 0) {
            if (ms.Position + keyIdLength > ms.Length)
                throw new InvalidDataException("Invalid encrypted data format: keyId length exceeds remaining data.");

            var keyIdBytes = br.ReadBytes(keyIdLength);
            headerKeyId = Encoding.UTF8.GetString(keyIdBytes);
        }

        if (ms.Position >= ms.Length)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyVersion.");

        var headerKeyVersion = br.ReadString();
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        var nonceLength = br.ReadInt32();
        ArgumentHelpers.ThrowIfNotInRange(nonceLength, SivSize, SivSize, nameof(ms), $"Invalid synthetic IV length: {nonceLength}. Expected {SivSize} bytes.");
        var sivBlock = br.ReadBytes(nonceLength);
        var body = br.ReadBytes((int)(ms.Length - ms.Position));
        byte[]? actualKey = null;
        if (key != null)
            actualKey = key;
        else {
            var actualKeyId = headerKeyId ?? keyId;
            var actualKeyVersion = headerKeyVersion;
            if (actualKeyId != null && KeyStore != null) {
                if (!string.IsNullOrWhiteSpace(actualKeyVersion)) {
                    actualKey = KeyStore.GetKey(actualKeyId, actualKeyVersion);
                    OperationHelpers.ThrowIfNull(
                        actualKey, $"No decryption key available for key ID '{actualKeyId}' version {actualKeyVersion}. Ensure the key version exists in KeyStore.");
                }
                else {
                    actualKey = KeyStore.GetCurrentKey(actualKeyId);
                    OperationHelpers.ThrowIfNull(actualKey, $"No decryption key available for key ID '{actualKeyId}'. Ensure a key is configured.");
                }

                ValidateKey(actualKey);
            }
            else
                OperationHelpers.ThrowIf(true, "No decryption key available. Provide either a keyId or a key parameter.");
        }

        if (key != null)
            ValidateKey(key);

        var combined = new byte[SivSize + body.Length];
        Buffer.BlockCopy(sivBlock, 0, combined, 0, SivSize);
        if (body.Length > 0)
            Buffer.BlockCopy(body, 0, combined, SivSize, body.Length);

        var plaintext = new byte[body.Length];
        try {
            using (var siv = new Dorssel.Security.Cryptography.AesSiv(actualKey!))
                siv.Decrypt(combined.AsSpan(), plaintext.AsSpan(), associatedData is { Length: > 0 } ? associatedData.AsSpan() : ReadOnlySpan<byte>.Empty);

            return plaintext;
        }
        catch (CryptographicException ex) {
            throw new DecryptionFailedException("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
        }
        finally {
            SecurityUtilities.Clear(sivBlock);
            SecurityUtilities.Clear(combined);
        }
    }

    private void ValidateKey(byte[] k)
        => ArgumentHelpers.ThrowIf(k.Length != RequiredKeyBytes, $"AES-SIV key must be exactly {RequiredKeyBytes} bytes for the configured key size.", nameof(k));
}