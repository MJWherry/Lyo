using System.Text;

namespace Lyo.Encryption;

/// <summary>
/// Represents the encryption header for two-key encrypted stream files. Format version <see cref="StreamFormatVersion.V1" />:
/// [FormatVersion:1][DEKAlgorithmId:1][KEKAlgorithmId:1][DekKeyMaterialBytes:1][DekEncoding:1][KeyIdLength:4][KeyId][KeyVersionLength:4][KeyVersion][EncryptedDEKLength:4][EncryptedDEK].
/// </summary>
public sealed record EncryptionHeader
{
    /// <summary><see cref="DekEncoding" /> value: the DEK is encrypted with the KEK encryption service's regular single-shot format (used when no raw AES-sized KEK is available, e.g. RSA KEKs).</summary>
    public const byte DekEncodingEnvelope = 0;

    /// <summary><see cref="DekEncoding" /> value: the DEK is wrapped with AES Key Wrap (RFC 3394) — deterministic, integrity-checked, and always exactly <c>dekLength + 8</c> bytes.</summary>
    public const byte DekEncodingAesKeyWrap = 1;

    /// <summary>AES Key Wrap (RFC 3394) output is always exactly this many bytes longer than the plaintext key it wraps.</summary>
    public const int AesKeyWrapOverhead = 8;

    /// <summary>First byte of the header; use <see cref="StreamFormatVersion.V1" />.</summary>
    public byte FormatVersion { get; init; } = (byte)StreamFormatVersion.V1;

    public byte DekAlgorithmId { get; init; }

    public byte KekAlgorithmId { get; init; }

    /// <summary>Length in bytes of the plaintext DEK after KEK unwrap.</summary>
    public byte DekKeyMaterialBytes { get; init; } = 32;

    /// <summary>How the DEK is protected: 0 = encrypted with the KEK service's single-shot envelope, 1 = wrapped with AES Key Wrap (RFC 3394).</summary>
    public byte DekEncoding { get; init; }

    public string KeyId { get; init; } = string.Empty;

    public string KeyVersion { get; init; } = string.Empty;

    public byte[] EncryptedDataEncryptionKey { get; init; } = [];

    /// <summary>Reads the encryption header from a BinaryReader.</summary>
    public static EncryptionHeader Read(BinaryReader reader)
    {
        var formatVersion = reader.ReadByte();
        if (formatVersion != (byte)StreamFormatVersion.V1)
            throw new InvalidDataException($"Unsupported encryption header format version: {formatVersion}.");

        var dekAlgorithmId = reader.ReadByte();
        var kekAlgorithmId = reader.ReadByte();
        var dekKeyMaterialBytes = reader.ReadByte();
        var dekEncoding = reader.ReadByte();
        TwoKeyDekValidation.ValidateHeader(dekAlgorithmId, dekKeyMaterialBytes);
        var keyIdLen = reader.ReadInt32();
        if (keyIdLen is < 0 or > 1024)
            throw new InvalidDataException($"Invalid key ID length: {keyIdLen}. Maximum allowed: 1024 bytes.");

        var keyIdBytes = keyIdLen > 0 ? reader.ReadBytes(keyIdLen) : [];
        var keyId = keyIdLen > 0 ? Encoding.UTF8.GetString(keyIdBytes) : string.Empty;
        var keyVersionLen = reader.ReadInt32();
        if (keyVersionLen is < 0 or > 1024)
            throw new InvalidDataException($"Invalid key version length: {keyVersionLen}. Maximum allowed: 1024 bytes.");

        var keyVersionBytes = keyVersionLen > 0 ? reader.ReadBytes(keyVersionLen) : [];
        var keyVersion = keyVersionLen > 0 ? Encoding.UTF8.GetString(keyVersionBytes) : string.Empty;
        var encryptedDekLen = reader.ReadInt32();
        if (encryptedDekLen < 0)
            throw new InvalidDataException($"Invalid encrypted DEK length: {encryptedDekLen}.");

        var encryptedDek = reader.ReadBytes(encryptedDekLen);
        return new() {
            FormatVersion = formatVersion,
            DekAlgorithmId = dekAlgorithmId,
            KekAlgorithmId = kekAlgorithmId,
            DekKeyMaterialBytes = dekKeyMaterialBytes,
            DekEncoding = dekEncoding,
            KeyId = keyId,
            KeyVersion = keyVersion,
            EncryptedDataEncryptionKey = encryptedDek
        };
    }

    /// <summary>Reads the encryption header from a stream.</summary>
    public static EncryptionHeader Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        return Read(reader);
    }

    /// <summary>Reads the encryption header from a byte array.</summary>
    public static EncryptionHeader Read(byte[] data)
    {
        using var stream = new MemoryStream(data, false);
        return Read(stream);
    }

    /// <summary>Writes the encryption header to a BinaryWriter (format version <see cref="StreamFormatVersion.V1" />).</summary>
    public void Write(BinaryWriter writer)
    {
        var keyIdBytes = string.IsNullOrEmpty(KeyId) ? [] : Encoding.UTF8.GetBytes(KeyId);
        var keyVersionBytes = string.IsNullOrEmpty(KeyVersion) ? [] : Encoding.UTF8.GetBytes(KeyVersion);
        writer.Write((byte)StreamFormatVersion.V1);
        writer.Write(DekAlgorithmId);
        writer.Write(KekAlgorithmId);
        writer.Write(DekKeyMaterialBytes);
        writer.Write(DekEncoding);
        writer.Write(keyIdBytes.Length);
        if (keyIdBytes.Length > 0)
            writer.Write(keyIdBytes);

        writer.Write(keyVersionBytes.Length);
        if (keyVersionBytes.Length > 0)
            writer.Write(keyVersionBytes);

        writer.Write(EncryptedDataEncryptionKey.Length);
        writer.Write(EncryptedDataEncryptionKey);
    }

    /// <summary>Writes the encryption header to a stream.</summary>
    public void Write(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        Write(writer);
    }

    /// <summary>Writes the encryption header to a List&lt;byte&gt; (format version <see cref="StreamFormatVersion.V1" />).</summary>
    public void Write(List<byte> buffer)
    {
        var keyIdBytes = string.IsNullOrEmpty(KeyId) ? [] : Encoding.UTF8.GetBytes(KeyId);
        var keyVersionBytes = string.IsNullOrEmpty(KeyVersion) ? [] : Encoding.UTF8.GetBytes(KeyVersion);
        buffer.Add((byte)StreamFormatVersion.V1);
        buffer.Add(DekAlgorithmId);
        buffer.Add(KekAlgorithmId);
        buffer.Add(DekKeyMaterialBytes);
        buffer.Add(DekEncoding);
        buffer.AddRange(BitConverter.GetBytes(keyIdBytes.Length));
        if (keyIdBytes.Length > 0)
            buffer.AddRange(keyIdBytes);

        buffer.AddRange(BitConverter.GetBytes(keyVersionBytes.Length));
        if (keyVersionBytes.Length > 0)
            buffer.AddRange(keyVersionBytes);

        buffer.AddRange(BitConverter.GetBytes(EncryptedDataEncryptionKey.Length));
        buffer.AddRange(EncryptedDataEncryptionKey);
    }

    /// <summary>Gets the total size of the header in bytes.</summary>
    public int GetHeaderSize()
    {
        var keyIdBytes = string.IsNullOrEmpty(KeyId) ? [] : Encoding.UTF8.GetBytes(KeyId);
        var keyVersionBytes = string.IsNullOrEmpty(KeyVersion) ? [] : Encoding.UTF8.GetBytes(KeyVersion);
        return 1 + // Format Version
            1 + // DEK Algorithm ID
            1 + // KEK Algorithm ID
            1 + // DekKeyMaterialBytes
            1 + // DekEncoding
            4 + // KeyId length
            keyIdBytes.Length + // KeyId
            4 + // KeyVersion length
            keyVersionBytes.Length + // KeyVersion
            4 + // Encrypted DEK length
            EncryptedDataEncryptionKey.Length; // Encrypted DEK
    }

    /// <summary>
    /// Infers how an encrypted DEK blob is encoded from its length: AES Key Wrap output is always exactly
    /// <paramref name="dekKeyMaterialBytes" /> + <see cref="AesKeyWrapOverhead" /> bytes, while KEK-service envelopes (version + nonce + tag + ciphertext) are always larger.
    /// </summary>
    public static byte InferDekEncoding(int encryptedDekLength, byte dekKeyMaterialBytes)
        => encryptedDekLength == dekKeyMaterialBytes + AesKeyWrapOverhead ? DekEncodingAesKeyWrap : DekEncodingEnvelope;

    /// <summary>
    /// Creates a copy of this header with updated values. When <paramref name="encryptedDataEncryptionKey" /> is supplied (e.g. during DEK migration to a different KEK),
    /// <see cref="DekEncoding" /> is recomputed from the new blob via <see cref="InferDekEncoding" /> — the old value may no longer match when the source and target KEKs
    /// differ in AES Key Wrap eligibility, and a stale byte would make the file undecryptable.
    /// </summary>
    public EncryptionHeader With(
        string? keyId = null,
        string? keyVersion = null,
        byte[]? encryptedDataEncryptionKey = null,
        byte? formatVersion = null,
        byte? dekAlgorithmId = null,
        byte? kekAlgorithmId = null,
        byte? dekKeyMaterialBytes = null)
    {
        var newDekKeyMaterialBytes = dekKeyMaterialBytes ?? DekKeyMaterialBytes;
        return this with {
            FormatVersion = formatVersion ?? FormatVersion,
            DekAlgorithmId = dekAlgorithmId ?? DekAlgorithmId,
            KekAlgorithmId = kekAlgorithmId ?? KekAlgorithmId,
            DekKeyMaterialBytes = newDekKeyMaterialBytes,
            DekEncoding = encryptedDataEncryptionKey == null ? DekEncoding : InferDekEncoding(encryptedDataEncryptionKey.Length, newDekKeyMaterialBytes),
            KeyId = keyId ?? KeyId,
            KeyVersion = keyVersion ?? KeyVersion,
            EncryptedDataEncryptionKey = encryptedDataEncryptionKey ?? EncryptedDataEncryptionKey
        };
    }
}