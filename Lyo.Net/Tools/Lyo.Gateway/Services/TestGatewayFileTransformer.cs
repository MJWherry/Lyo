using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using AesSivKeySizeBits = Lyo.Encryption.Symmetric.Aes.AesSiv.AesSivKeySizeBits;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;
using Microsoft.AspNetCore.Components.Forms;

namespace Lyo.Gateway.Services;

public enum TestGatewayFileOperation
{
    Encrypt,
    Decrypt,
    TwoKeyEncrypt,
    TwoKeyDecrypt,
    Compress,
    Decompress
}

public sealed record TestGatewayUploadedFile(string FileName, string ContentType, byte[] Content)
{
    public long Size => Content.LongLength;
}

public sealed record TestGatewayTransformOptions(
    bool Reverse,
    bool ApplyCompression,
    bool ApplyEncryption,
    bool UseTwoKeyEncryption,
    string Secret,
    EncryptionAlgorithm DataEncryptionAlgorithm,
    EncryptionAlgorithm KeyEncryptionAlgorithm,
    CompressionAlgorithm CompressionAlgorithm,
    AesGcmKeySizeBits DataAesGcmKeySize = AesGcmKeySizeBits.Bits256,
    AesGcmKeySizeBits KeyAesGcmKeySize = AesGcmKeySizeBits.Bits256,
    AesSivKeySizeBits DataAesSivKeySize = AesSivKeySizeBits.Bits256,
    AesSivKeySizeBits KeyAesSivKeySize = AesSivKeySizeBits.Bits256);

public sealed record TestGatewayTransformResult(string OutputFileName, string OutputContentType, byte[] OutputBytes, IReadOnlyList<KeyValuePair<string, string>> Details);

public sealed class TestGatewayFileTransformer
{
    public const long MaxUploadBytes = 100L * 1024 * 1024;

    public async Task<TestGatewayTransformResult> TransformAsync(TestGatewayUploadedFile file, TestGatewayTransformOptions options, CancellationToken ct = default)
    {
        if (file.Content.Length == 0)
            throw new InvalidOperationException("Please upload a file first.");

        if (!options.ApplyCompression && !options.ApplyEncryption)
            throw new InvalidOperationException("Select compression, encryption, or both.");

        if (options.ApplyEncryption && string.IsNullOrWhiteSpace(options.Secret))
            throw new InvalidOperationException("A secret is required when encryption is enabled.");

        var currentFileName = file.FileName;
        var currentBytes = file.Content;
        var details = new List<KeyValuePair<string, string>> {
            Detail("Direction", options.Reverse ? "Reverse" : "Forward"),
            Detail("Compression", options.ApplyCompression ? options.CompressionAlgorithm.ToString() : "Off"),
            Detail(
                "Encryption",
                options.ApplyEncryption
                    ? options.UseTwoKeyEncryption
                        ? FormatTwoKeyEncryptionSummary(options)
                        : FormatSingleKeyEncryptionSummary(options)
                    : "Off"),
            Detail("Input Size", ToFileSize(file.Size))
        };

        if (!options.Reverse) {
            if (options.ApplyCompression) {
                var compressed = Compress(currentBytes, currentFileName, options.CompressionAlgorithm);
                currentBytes = compressed.Bytes;
                currentFileName = compressed.FileName;
                details.AddRange(compressed.Details);
            }

            if (options.ApplyEncryption) {
                var encrypted = options.UseTwoKeyEncryption
                    ? await TwoKeyEncryptAsync(currentBytes, currentFileName, options, ct)
                    : Encrypt(currentBytes, currentFileName, options);

                currentBytes = encrypted.Bytes;
                currentFileName = encrypted.FileName;
                details.AddRange(encrypted.Details);
            }
        }
        else {
            if (options.ApplyEncryption) {
                var decrypted = options.UseTwoKeyEncryption
                    ? await TwoKeyDecryptAsync(currentBytes, currentFileName, options, ct)
                    : Decrypt(currentBytes, currentFileName, options);

                currentBytes = decrypted.Bytes;
                currentFileName = decrypted.FileName;
                details.AddRange(decrypted.Details);
            }

            if (options.ApplyCompression) {
                var decompressed = Decompress(currentBytes, currentFileName, options.CompressionAlgorithm);
                currentBytes = decompressed.Bytes;
                currentFileName = decompressed.FileName;
                details.AddRange(decompressed.Details);
            }
        }

        details.Add(Detail("Output Size", ToFileSize(currentBytes.LongLength)));
        return new(currentFileName, FileTypeInfo.Unknown.MimeType, currentBytes, details);
    }

    /// <summary>Reads Lyo encryption envelope fields from the binary layout (and raw magic at offset 0 for unencrypted inputs) without decrypting.</summary>
    public TestGatewayTransformResult ProbeFile(TestGatewayUploadedFile file)
    {
        var details = new List<KeyValuePair<string, string>> {
            Detail("Probe", "Header / format sniff (no decryption)"),
            Detail("Input file", file.FileName),
            Detail("Input size", ToFileSize(file.Size))
        };

        var data = file.Content;
        if (data.Length == 0) {
            details.Add(Detail("Result", "Empty buffer."));
            return new("(analysis)", FileTypeInfo.Txt.MimeType, [], details);
        }

        var outerEncExt = TryGetOuterEncryptionExtension(file.FileName);

        try {
            if (outerEncExt != null && IsTwoKeyFilenameExtension(outerEncExt))
                AppendTwoKeyEnvelopeDetails(data, details);
            else if (outerEncExt != null && IsAesSivFilenameExtension(outerEncExt))
                TryAppendAesSivEnvelopeDetails(data, details);
            else if (outerEncExt != null && IsRsaFamilyFilenameExtension(outerEncExt)) {
                details.Add(
                    Detail(
                        "Envelope",
                        "RSA / AES-GCM+RSA use a different wire format than symmetric V1; this probe does not parse those layouts. Decrypt with the matching service to inspect content."));
            }
            else if (outerEncExt == null || IsSymmetricAuthenticatedFilenameExtension(outerEncExt)) {
                if (outerEncExt == null) {
                    try {
                        TryAppendSymmetricAuthenticatedEnvelopeDetails(data, details);
                    }
                    catch (Exception gcmStyle) {
                        try {
                            TryAppendAesSivEnvelopeDetails(data, details);
                        }
                        catch (Exception siv) {
                            details.Add(
                                Detail(
                                    "Symmetric V1 envelope",
                                    $"Could not parse as nonce+tag layout ({gcmStyle.Message}) or AES-SIV layout ({siv.Message})."));
                        }
                    }
                }
                else
                    TryAppendSymmetricAuthenticatedEnvelopeDetails(data, details);
            }
            else
                details.Add(Detail("Envelope", $"Unrecognized Lyo encryption extension '{outerEncExt}' for structured probe."));
        }
        catch (Exception ex) {
            details.Add(Detail("Envelope", ex.Message));
        }

        AppendRawCompressionMagicHint(outerEncExt, data, details);
        return new("(analysis)", FileTypeInfo.Txt.MimeType, [], details);
    }

    /// <summary>Returns the outermost Lyo encryption suffix on <paramref name="fileName"/> (longest match), or null.</summary>
    private static string? TryGetOuterEncryptionExtension(string fileName)
    {
        foreach (var ext in FileTypeInfo.EncryptionFilenameStripSuffixesLongestFirst) {
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return ext;
        }

        return null;
    }

    private static bool IsTwoKeyFilenameExtension(string ext)
        => ext.EndsWith(FileTypeInfo.TwoKeyEnvelopeSuffix, StringComparison.OrdinalIgnoreCase);

    private static bool IsAesSivFilenameExtension(string ext)
        => ext.Equals(FileTypeInfo.LyoAesSiv.DefaultExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsRsaFamilyFilenameExtension(string ext)
        => ext.Equals(FileTypeInfo.LyoRsa.DefaultExtension, StringComparison.OrdinalIgnoreCase)
            || ext.Equals(FileTypeInfo.LyoAesGcmRsa.DefaultExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsSymmetricAuthenticatedFilenameExtension(string ext)
        => ext.Equals(FileTypeInfo.LyoAesGcm.DefaultExtension, StringComparison.OrdinalIgnoreCase)
            || ext.Equals(FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension, StringComparison.OrdinalIgnoreCase)
            || ext.Equals(FileTypeInfo.LyoAesCcm.DefaultExtension, StringComparison.OrdinalIgnoreCase)
            || ext.Equals(FileTypeInfo.LyoXChaCha20Poly1305.DefaultExtension, StringComparison.OrdinalIgnoreCase);

    private static void AppendTwoKeyEnvelopeDetails(byte[] data, List<KeyValuePair<string, string>> details)
    {
        var header = EncryptionHeader.Read(data);
        details.Add(Detail("Envelope", "Two-key (Lyo stream)"));
        details.Add(Detail("Format version", header.FormatVersion.ToString()));
        details.Add(Detail("DEK algorithm", ((EncryptionAlgorithm)header.DekAlgorithmId).ToString()));
        details.Add(Detail("KEK algorithm", ((EncryptionAlgorithm)header.KekAlgorithmId).ToString()));
        details.Add(Detail("DEK key material (bytes)", header.DekKeyMaterialBytes.ToString()));
        details.Add(Detail("KEK key material (expected)", TwoKeyDekValidation.DescribeValidSymmetricKeyMaterialSizes(header.KekAlgorithmId)));
        details.Add(Detail("Key id", string.IsNullOrEmpty(header.KeyId) ? "(none)" : header.KeyId));
        details.Add(Detail("Key version", string.IsNullOrEmpty(header.KeyVersion) ? "(none)" : header.KeyVersion));
        details.Add(Detail("Encrypted DEK length", ToFileSize(header.EncryptedDataEncryptionKey.LongLength)));
        AppendTwoKeyStreamLayoutFromHeader(data, header, details);
    }

    /// <summary>Reads sizes from the two-key stream layout after <see cref="EncryptionHeader"/> (wrapped DEK + first sealed chunk length).</summary>
    private static void AppendTwoKeyStreamLayoutFromHeader(byte[] data, EncryptionHeader header, List<KeyValuePair<string, string>> details)
    {
        var headerByteLen = header.GetHeaderSize();
        details.Add(Detail("Header size (bytes)", headerByteLen.ToString()));
        details.Add(
            Detail(
                "Inner compression",
                "Not a field in the two-key header. Layout after the header is: wrapped DEK, then repeated [u32 sealed length][ciphertext chunk]. Plaintext inside each chunk is the pre-DEK payload (e.g. compressed bytes); gzip/lzma/… magic is only visible after KEK unwrap → DEK decrypt."));

        if (data.Length < headerByteLen + 4) {
            details.Add(Detail("First sealed chunk", data.Length <= headerByteLen ? "Missing data after header." : "Missing chunk length prefix after header."));
            return;
        }

        var chunkLen = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(headerByteLen, 4));
        details.Add(Detail("First sealed chunk length (from stream, bytes)", chunkLen.ToString()));
        details.Add(Detail("First sealed chunk payload offset", (headerByteLen + 4).ToString()));

        if (chunkLen < 0 || chunkLen > data.Length - headerByteLen - 4)
            details.Add(Detail("First sealed chunk", $"Length {chunkLen} is inconsistent with file size."));
    }

    public static async Task<TestGatewayUploadedFile> ReadBrowserFileAsync(IBrowserFile browserFile, CancellationToken ct = default)
    {
        await using var input = browserFile.OpenReadStream(MaxUploadBytes, ct);
        await using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        return new(browserFile.Name, browserFile.ContentType, buffer.ToArray());
    }

    public static IReadOnlyList<string> ParseLines(string? value)
        => value?.Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

    public static string FormatErrors(IReadOnlyList<Error>? errors)
        => errors == null || errors.Count == 0 ? "Unknown error." : string.Join(Environment.NewLine, errors.Select(i => $"{i.Code}: {i.Message}"));

    /// <summary>Parses symmetric V1 envelopes that store nonce + 16-byte auth tag + ciphertext (AES-GCM, ChaCha, AES-CCM, XChaCha).</summary>
    private static void TryAppendSymmetricAuthenticatedEnvelopeDetails(byte[] data, List<KeyValuePair<string, string>> details)
    {
        using var ms = new MemoryStream(data, false);
        using var br = new BinaryReader(ms, Encoding.UTF8, true);
        if (data.Length < 14)
            throw new InvalidDataException("Buffer too small for a Lyo symmetric V1 envelope.");

        var formatByte = br.ReadByte();
        if (formatByte != (byte)StreamFormatVersion.V1)
            throw new InvalidDataException($"Expected format version {(byte)StreamFormatVersion.V1}, got {formatByte}.");

        var keyIdLength = br.ReadInt32();
        if (keyIdLength is < 0 or > 1024)
            throw new InvalidDataException($"Invalid key id length: {keyIdLength}.");

        string? headerKeyId = null;
        if (keyIdLength > 0) {
            if (ms.Position + keyIdLength > ms.Length)
                throw new InvalidDataException("Key id length exceeds remaining data.");

            headerKeyId = Encoding.UTF8.GetString(br.ReadBytes(keyIdLength));
        }

        var headerKeyVersion = br.ReadString();
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        var nonceLength = br.ReadInt32();
        details.Add(Detail("Envelope", "Single-key (nonce + Poly1305/GCM tag layout: AES-GCM, ChaCha20-Poly1305, AES-CCM, or XChaCha20-Poly1305)"));
        details.Add(Detail("Key id", string.IsNullOrEmpty(headerKeyId) ? "(none)" : headerKeyId));
        details.Add(Detail("Key version", string.IsNullOrWhiteSpace(headerKeyVersion) ? "(none)" : headerKeyVersion));
        details.Add(Detail("Nonce length", nonceLength.ToString()));

        if (nonceLength == AesGcmHelper.NonceSize)
            details.Add(
                Detail(
                    "Symmetric cipher hint",
                    "12-byte nonce: typically AES-GCM, ChaCha20-Poly1305, or AES-CCM — use file extension (.ag, .chacha, .ccm)."));

        const int xChaChaNonceSize = 24;
        if (nonceLength == xChaChaNonceSize)
            details.Add(Detail("Symmetric cipher hint", "24-byte nonce: consistent with XChaCha20-Poly1305 (.xchacha)."));

        if (ms.Position + nonceLength + 16 > ms.Length)
            throw new InvalidDataException("Truncated ciphertext (nonce/tag).");

        _ = br.ReadBytes(nonceLength);
        _ = br.ReadBytes(16);
        var cipherBytes = ms.Length - ms.Position;
        details.Add(Detail("Ciphertext length (after nonce + tag)", ToFileSize(cipherBytes)));
    }

    /// <summary>Parses AES-SIV V1 envelopes (16-byte synthetic IV + CTR payload, no separate tag in the stream).</summary>
    private static void TryAppendAesSivEnvelopeDetails(byte[] data, List<KeyValuePair<string, string>> details)
    {
        const int sivSize = 16;
        const int minEncryptedSize = 27;
        if (data.Length < minEncryptedSize)
            throw new InvalidDataException($"Buffer too small for a Lyo AES-SIV envelope (need at least {minEncryptedSize} bytes).");

        using var ms = new MemoryStream(data, false);
        using var br = new BinaryReader(ms, Encoding.UTF8, true);

        var formatByte = br.ReadByte();
        if (formatByte != (byte)StreamFormatVersion.V1)
            throw new InvalidDataException($"Expected format version {(byte)StreamFormatVersion.V1}, got {formatByte}.");

        var keyIdLength = br.ReadInt32();
        if (keyIdLength is < 0 or > 1024)
            throw new InvalidDataException($"Invalid key id length: {keyIdLength}.");

        string? headerKeyId = null;
        if (keyIdLength > 0) {
            if (ms.Position + keyIdLength > ms.Length)
                throw new InvalidDataException("Key id length exceeds remaining data.");

            headerKeyId = Encoding.UTF8.GetString(br.ReadBytes(keyIdLength));
        }

        var headerKeyVersion = br.ReadString();
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        var syntheticIvLength = br.ReadInt32();
        if (syntheticIvLength != sivSize)
            throw new InvalidDataException($"Invalid synthetic IV length for AES-SIV: {syntheticIvLength} (expected {sivSize}).");

        if (ms.Position + syntheticIvLength > ms.Length)
            throw new InvalidDataException("Truncated AES-SIV envelope (synthetic IV).");

        _ = br.ReadBytes(syntheticIvLength);
        var payloadBytes = ms.Length - ms.Position;

        details.Add(Detail("Envelope", "Single-key (AES-SIV layout)"));
        details.Add(Detail("Key id", string.IsNullOrEmpty(headerKeyId) ? "(none)" : headerKeyId));
        details.Add(Detail("Key version", string.IsNullOrWhiteSpace(headerKeyVersion) ? "(none)" : headerKeyVersion));
        details.Add(Detail("Synthetic IV length", syntheticIvLength.ToString()));
        details.Add(Detail("Payload length (CTR ciphertext)", ToFileSize(payloadBytes)));
    }

    private static void AppendRawCompressionMagicHint(string? outerEncExt, byte[] data, List<KeyValuePair<string, string>> details)
    {
        var magic = DescribeCompressionMagicExtended(data);
        if (!string.IsNullOrEmpty(outerEncExt)) {
            details.Add(
                Detail(
                    "Raw compression (magic)",
                    "Bytes at offset 0 are the Lyo encryption envelope, not raw compressor output — signatures such as gzip or xz appear only after decryption (or infer compression from the filename). "
                        + magic));
        }
        else
            details.Add(Detail("Raw compression (magic)", magic));
    }

    /// <summary>Best-effort magic-byte description for common stream formats (offset 0).</summary>
    private static string DescribeCompressionMagicExtended(ReadOnlySpan<byte> d)
    {
        if (d.Length == 0)
            return "No data.";

        if (d.Length >= 2 && d[0] == 0x1F && d[1] == 0x8B)
            return "Looks like GZIP (0x1F 0x8B).";

        if (d.Length >= 2 && d[0] == 0x78 && d[1] is 0x01 or 0x5E or 0x9C or 0xDA)
            return "Looks like zlib wrapper (0x78 … CMF/FLG).";

        if (d.Length >= 1 && d[0] >= 0x81 && d[0] <= 0x83)
            return "First byte may be Brotli window setting (weak heuristic).";

        if (d.Length >= 6 && d[0] == 0xFD && d[1] == 0x37 && d[2] == 0x7A && d[3] == 0x58 && d[4] == 0x5A && d[5] == 0x00)
            return "Looks like XZ container (FD 37 7A 58 5A 00).";

        if (d.Length >= 4 && d[0] == 0x28 && d[1] == 0xB5 && d[2] == 0x2F && d[3] == 0xFD)
            return "Looks like Zstandard frame (28 B5 2F FD).";

        if (d.Length >= 4 && d[0] == 0x04 && d[1] == 0x22 && d[2] == 0x4D && d[3] == 0x18)
            return "Looks like LZ4 frame (04 22 4D 18).";

        if (d.Length >= 2 && d[0] == 0x42 && d[1] == 0x5A)
            return "Looks like bzip2 (42 5A…).";

        if (d.Length >= 8 && d[0] == 0xFF && d[1] == 0x06 && d[2] == 0x00 && d[3] == 0x00 && d[4] == 0x73 && d[5] == 0x4E && d[6] == 0x61 && d[7] == 0x50)
            return "Looks like Snappy framing stream identifier (stream header sNaP).";

        return "No recognized gzip / zlib / brotli / xz / zstd / lz4 / bzip2 / snappy signature at offset 0.";
    }

    private static TestGatewayStepResult Encrypt(byte[] bytes, string fileName, TestGatewayTransformOptions options)
    {
        var algorithm = options.DataEncryptionAlgorithm;
        var service = CreateEncryptionService(algorithm, CreatePlaceholderKeyStore(), options.DataAesGcmKeySize, options.DataAesSivKeySize);
        var encrypted = service.Encrypt(
            bytes,
            key: DeriveSymmetricKey(options.Secret, algorithm, options.DataAesGcmKeySize, options.DataAesSivKeySize));
        var details = new List<KeyValuePair<string, string>> { Detail("Encrypted With", algorithm.ToString()) };
        AppendSingleKeySizeDetails(details, algorithm, options.DataAesGcmKeySize, options.DataAesSivKeySize);

        return new(BuildEncryptedName(fileName, service.FileExtension), encrypted, details);
    }

    private static TestGatewayStepResult Decrypt(byte[] bytes, string fileName, TestGatewayTransformOptions options)
    {
        var algorithm = options.DataEncryptionAlgorithm;
        var service = CreateEncryptionService(algorithm, CreatePlaceholderKeyStore(), options.DataAesGcmKeySize, options.DataAesSivKeySize);
        var decrypted = service.Decrypt(
            bytes,
            key: DeriveSymmetricKey(options.Secret, algorithm, options.DataAesGcmKeySize, options.DataAesSivKeySize));
        var details = new List<KeyValuePair<string, string>> { Detail("Decrypted With", algorithm.ToString()) };
        AppendSingleKeySizeDetails(details, algorithm, options.DataAesGcmKeySize, options.DataAesSivKeySize);

        return new(BuildDecryptedName(fileName, service.FileExtension), decrypted, details);
    }

    private static async Task<TestGatewayStepResult> TwoKeyEncryptAsync(
        byte[] bytes,
        string fileName,
        TestGatewayTransformOptions options,
        CancellationToken ct)
    {
        var dataAlgorithm = options.DataEncryptionAlgorithm;
        var keyAlgorithm = options.KeyEncryptionAlgorithm;
        var service = CreateTwoKeyEncryptionService(
            dataAlgorithm,
            keyAlgorithm,
            CreatePlaceholderKeyStore(),
            options.DataAesGcmKeySize,
            options.KeyAesGcmKeySize,
            options.DataAesSivKeySize,
            options.KeyAesSivKeySize);
        try {
            await using var input = new MemoryStream(bytes, false);
            await using var output = new MemoryStream();
            await service.EncryptToStreamAsync(
                input,
                output,
                kek: DeriveSymmetricKey(options.Secret, keyAlgorithm, options.KeyAesGcmKeySize, options.KeyAesSivKeySize),
                ct: ct);
            var encrypted = output.ToArray();
            var header = EncryptionHeader.Read(encrypted);
            var details = new List<KeyValuePair<string, string>> {
                Detail("Data Encrypted With", dataAlgorithm.ToString()),
                Detail("Key Encrypted With", keyAlgorithm.ToString()),
                Detail("Encrypted DEK Size", ToFileSize(header.EncryptedDataEncryptionKey.LongLength))
            };
            AppendTwoKeySizeDetails(details, dataAlgorithm, keyAlgorithm, options);

            return new(BuildEncryptedName(fileName, service.FileExtension), encrypted, details);
        }
        finally {
            (service as IDisposable)?.Dispose();
        }
    }

    private static async Task<TestGatewayStepResult> TwoKeyDecryptAsync(
        byte[] bytes,
        string fileName,
        TestGatewayTransformOptions options,
        CancellationToken ct)
    {
        var dataAlgorithm = options.DataEncryptionAlgorithm;
        var keyAlgorithm = options.KeyEncryptionAlgorithm;
        var service = CreateTwoKeyEncryptionService(
            dataAlgorithm,
            keyAlgorithm,
            CreatePlaceholderKeyStore(),
            options.DataAesGcmKeySize,
            options.KeyAesGcmKeySize,
            options.DataAesSivKeySize,
            options.KeyAesSivKeySize);
        try {
            var header = EncryptionHeader.Read(bytes);
            await using var input = new MemoryStream(bytes, false);
            await using var output = new MemoryStream();
            await service.DecryptToStreamAsync(
                input,
                output,
                kek: DeriveSymmetricKey(options.Secret, keyAlgorithm, options.KeyAesGcmKeySize, options.KeyAesSivKeySize),
                ct: ct);
            var details = new List<KeyValuePair<string, string>> {
                Detail("Data Decrypted With", dataAlgorithm.ToString()),
                Detail("Key Decrypted With", keyAlgorithm.ToString()),
                Detail("Encrypted DEK Size", ToFileSize(header.EncryptedDataEncryptionKey.LongLength))
            };
            AppendTwoKeySizeDetails(details, dataAlgorithm, keyAlgorithm, options);

            return new(BuildDecryptedName(fileName, service.FileExtension), output.ToArray(), details);
        }
        finally {
            (service as IDisposable)?.Dispose();
        }
    }

    private static TestGatewayStepResult Compress(byte[] bytes, string fileName, CompressionAlgorithm algorithm)
    {
        var service = new CompressionService(options: new() { DefaultAlgorithm = algorithm });
        var info = service.Compress(bytes, out var compressed);
        return new(
            fileName + service.FileExtension, compressed,
            [
                Detail("Compressed With", algorithm.ToString()), Detail("Compression Ratio", info.CompressionRatio.ToString("P2")),
                Detail("Space Saved", info.SpaceSavedPercent.ToString("F2") + "%")
            ]);
    }

    private static TestGatewayStepResult Decompress(byte[] bytes, string fileName, CompressionAlgorithm algorithm)
    {
        var service = new CompressionService(options: new() { DefaultAlgorithm = algorithm });
        var info = service.Decompress(bytes, out var decompressed);
        return new(
            BuildDecompressedName(fileName, service.FileExtension), decompressed,
            [Detail("Decompressed With", algorithm.ToString()), Detail("Expansion Ratio", info.ExpansionRatio.ToString("P2"))]);
    }

    private static IEncryptionService CreateEncryptionService(
        EncryptionAlgorithm algorithm,
        IKeyStore keyStore,
        AesGcmKeySizeBits aesGcmOrCcmKeySize,
        AesSivKeySizeBits aesSivKeySize)
        => algorithm switch {
            EncryptionAlgorithm.AesGcm => new AesGcmEncryptionService(keyStore, aesGcmOrCcmKeySize),
            EncryptionAlgorithm.ChaCha20Poly1305 => new ChaCha20Poly1305EncryptionService(keyStore),
            EncryptionAlgorithm.AesCcm => new AesCcmEncryptionService(keyStore, aesGcmOrCcmKeySize),
            EncryptionAlgorithm.AesSiv => new AesSivEncryptionService(keyStore, aesSivKeySize),
            EncryptionAlgorithm.XChaCha20Poly1305 => new XChaCha20Poly1305EncryptionService(keyStore),
            var _ => throw new NotSupportedException(
                $"{algorithm} is not supported in the test gateway file workbench. Use AES-GCM, ChaCha20-Poly1305, AES-CCM, AES-SIV, or XChaCha20-Poly1305.")
        };

    private static ITwoKeyEncryptionService CreateTwoKeyEncryptionService(
        EncryptionAlgorithm dataAlgorithm,
        EncryptionAlgorithm keyAlgorithm,
        IKeyStore keyStore,
        AesGcmKeySizeBits dataAesGcmKeySize,
        AesGcmKeySizeBits keyAesGcmKeySize,
        AesSivKeySizeBits dataAesSivKeySize,
        AesSivKeySizeBits keyAesSivKeySize)
    {
        // TwoKeyEncryptionService<TKeyEncryptionService, TDataEncryptionService>(dek, kek, keyStore)
        var dg = new AesGcmEncryptionService(keyStore, dataAesGcmKeySize);
        var kg = new AesGcmEncryptionService(keyStore, keyAesGcmKeySize);
        var dc = new AesCcmEncryptionService(keyStore, dataAesGcmKeySize);
        var kc = new AesCcmEncryptionService(keyStore, keyAesGcmKeySize);
        var dsiv = new AesSivEncryptionService(keyStore, dataAesSivKeySize);
        var ksiv = new AesSivEncryptionService(keyStore, keyAesSivKeySize);
        var dch = new ChaCha20Poly1305EncryptionService(keyStore);
        var kch = new ChaCha20Poly1305EncryptionService(keyStore);
        var dx = new XChaCha20Poly1305EncryptionService(keyStore);
        var kx = new XChaCha20Poly1305EncryptionService(keyStore);

        return (dataAlgorithm, keyAlgorithm) switch {
            (EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesGcm) => new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(dg, kg, keyStore),
            (EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305) => new TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, AesGcmEncryptionService>(dg, kch, keyStore),
            (EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesCcm) => new TwoKeyEncryptionService<AesCcmEncryptionService, AesGcmEncryptionService>(dg, kc, keyStore),
            (EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.AesSiv) => new TwoKeyEncryptionService<AesSivEncryptionService, AesGcmEncryptionService>(dg, ksiv, keyStore),
            (EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.XChaCha20Poly1305) => new TwoKeyEncryptionService<XChaCha20Poly1305EncryptionService, AesGcmEncryptionService>(dg, kx, keyStore),

            (EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesGcm) => new TwoKeyEncryptionService<AesGcmEncryptionService, ChaCha20Poly1305EncryptionService>(dch, kg, keyStore),
            (EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305) => new TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, ChaCha20Poly1305EncryptionService>(dch, kch, keyStore),
            (EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesCcm) => new TwoKeyEncryptionService<AesCcmEncryptionService, ChaCha20Poly1305EncryptionService>(dch, kc, keyStore),
            (EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesSiv) => new TwoKeyEncryptionService<AesSivEncryptionService, ChaCha20Poly1305EncryptionService>(dch, ksiv, keyStore),
            (EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.XChaCha20Poly1305) => new TwoKeyEncryptionService<XChaCha20Poly1305EncryptionService, ChaCha20Poly1305EncryptionService>(dch, kx, keyStore),

            (EncryptionAlgorithm.AesCcm, EncryptionAlgorithm.AesGcm) => new TwoKeyEncryptionService<AesGcmEncryptionService, AesCcmEncryptionService>(dc, kg, keyStore),
            (EncryptionAlgorithm.AesCcm, EncryptionAlgorithm.ChaCha20Poly1305) => new TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, AesCcmEncryptionService>(dc, kch, keyStore),
            (EncryptionAlgorithm.AesCcm, EncryptionAlgorithm.AesCcm) => new TwoKeyEncryptionService<AesCcmEncryptionService, AesCcmEncryptionService>(dc, kc, keyStore),
            (EncryptionAlgorithm.AesCcm, EncryptionAlgorithm.AesSiv) => new TwoKeyEncryptionService<AesSivEncryptionService, AesCcmEncryptionService>(dc, ksiv, keyStore),
            (EncryptionAlgorithm.AesCcm, EncryptionAlgorithm.XChaCha20Poly1305) => new TwoKeyEncryptionService<XChaCha20Poly1305EncryptionService, AesCcmEncryptionService>(dc, kx, keyStore),

            (EncryptionAlgorithm.AesSiv, EncryptionAlgorithm.AesGcm) => new TwoKeyEncryptionService<AesGcmEncryptionService, AesSivEncryptionService>(dsiv, kg, keyStore),
            (EncryptionAlgorithm.AesSiv, EncryptionAlgorithm.ChaCha20Poly1305) => new TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, AesSivEncryptionService>(dsiv, kch, keyStore),
            (EncryptionAlgorithm.AesSiv, EncryptionAlgorithm.AesCcm) => new TwoKeyEncryptionService<AesCcmEncryptionService, AesSivEncryptionService>(dsiv, kc, keyStore),
            (EncryptionAlgorithm.AesSiv, EncryptionAlgorithm.AesSiv) => new TwoKeyEncryptionService<AesSivEncryptionService, AesSivEncryptionService>(dsiv, ksiv, keyStore),
            (EncryptionAlgorithm.AesSiv, EncryptionAlgorithm.XChaCha20Poly1305) => new TwoKeyEncryptionService<XChaCha20Poly1305EncryptionService, AesSivEncryptionService>(dsiv, kx, keyStore),

            (EncryptionAlgorithm.XChaCha20Poly1305, EncryptionAlgorithm.AesGcm) => new TwoKeyEncryptionService<AesGcmEncryptionService, XChaCha20Poly1305EncryptionService>(dx, kg, keyStore),
            (EncryptionAlgorithm.XChaCha20Poly1305, EncryptionAlgorithm.ChaCha20Poly1305) => new TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, XChaCha20Poly1305EncryptionService>(dx, kch, keyStore),
            (EncryptionAlgorithm.XChaCha20Poly1305, EncryptionAlgorithm.AesCcm) => new TwoKeyEncryptionService<AesCcmEncryptionService, XChaCha20Poly1305EncryptionService>(dx, kc, keyStore),
            (EncryptionAlgorithm.XChaCha20Poly1305, EncryptionAlgorithm.AesSiv) => new TwoKeyEncryptionService<AesSivEncryptionService, XChaCha20Poly1305EncryptionService>(dx, ksiv, keyStore),
            (EncryptionAlgorithm.XChaCha20Poly1305, EncryptionAlgorithm.XChaCha20Poly1305) => new TwoKeyEncryptionService<XChaCha20Poly1305EncryptionService, XChaCha20Poly1305EncryptionService>(dx, kx, keyStore),

            var _ => throw new NotSupportedException(
                $"{dataAlgorithm}/{keyAlgorithm} is not supported in the test gateway file workbench (symmetric algorithms only).")
        };
    }

    private static void AppendSingleKeySizeDetails(
        List<KeyValuePair<string, string>> details,
        EncryptionAlgorithm algorithm,
        AesGcmKeySizeBits aesGcmOrCcm,
        AesSivKeySizeBits aesSiv)
    {
        switch (algorithm) {
            case EncryptionAlgorithm.AesGcm:
                details.Add(Detail("AES-GCM key size", $"{(int)aesGcmOrCcm} bits"));
                break;
            case EncryptionAlgorithm.AesCcm:
                details.Add(Detail("AES-CCM key size", $"{(int)aesGcmOrCcm} bits"));
                break;
            case EncryptionAlgorithm.AesSiv:
                details.Add(Detail("AES-SIV key size", $"{(int)aesSiv} bits"));
                break;
        }
    }

    private static void AppendTwoKeySizeDetails(
        List<KeyValuePair<string, string>> details,
        EncryptionAlgorithm dataAlgorithm,
        EncryptionAlgorithm keyAlgorithm,
        TestGatewayTransformOptions options)
    {
        if (dataAlgorithm == EncryptionAlgorithm.AesGcm)
            details.Add(Detail("Data AES-GCM key size", $"{(int)options.DataAesGcmKeySize} bits"));
        if (dataAlgorithm == EncryptionAlgorithm.AesCcm)
            details.Add(Detail("Data AES-CCM key size", $"{(int)options.DataAesGcmKeySize} bits"));
        if (dataAlgorithm == EncryptionAlgorithm.AesSiv)
            details.Add(Detail("Data AES-SIV key size", $"{(int)options.DataAesSivKeySize} bits"));

        if (keyAlgorithm == EncryptionAlgorithm.AesGcm)
            details.Add(Detail("Key AES-GCM key size", $"{(int)options.KeyAesGcmKeySize} bits"));
        if (keyAlgorithm == EncryptionAlgorithm.AesCcm)
            details.Add(Detail("Key AES-CCM key size", $"{(int)options.KeyAesGcmKeySize} bits"));
        if (keyAlgorithm == EncryptionAlgorithm.AesSiv)
            details.Add(Detail("Key AES-SIV key size", $"{(int)options.KeyAesSivKeySize} bits"));
    }

    private static IKeyStore CreatePlaceholderKeyStore() => new LocalKeyStore();

    /// <summary>Derives key material from the secret (SHA-256/512, truncated to the length required by the algorithm).</summary>
    private static byte[] DeriveSymmetricKey(
        string secret,
        EncryptionAlgorithm algorithm,
        AesGcmKeySizeBits aesGcmOrCcmKeySize,
        AesSivKeySizeBits aesSivKeySize)
    {
        var utf8 = Encoding.UTF8.GetBytes(secret);
        return algorithm switch {
            EncryptionAlgorithm.AesGcm => TruncateHash(SHA256.HashData(utf8), aesGcmOrCcmKeySize.GetKeyLengthBytes()),
            EncryptionAlgorithm.ChaCha20Poly1305 => SHA256.HashData(utf8),
            EncryptionAlgorithm.AesCcm => TruncateHash(SHA256.HashData(utf8), aesGcmOrCcmKeySize.GetKeyLengthBytes()),
            EncryptionAlgorithm.AesSiv => TruncateHash(SHA512.HashData(utf8), aesSivKeySize.GetKeyLengthBytes()),
            EncryptionAlgorithm.XChaCha20Poly1305 => SHA256.HashData(utf8),
            var _ => throw new NotSupportedException($"{algorithm} is not supported in the test gateway file workbench.")
        };
    }

    private static byte[] TruncateHash(byte[] hash32, int byteLength)
    {
        if (byteLength > hash32.Length)
            throw new InvalidOperationException($"Key length {byteLength} exceeds SHA-256 output.");

        if (byteLength == hash32.Length)
            return hash32;

        var result = new byte[byteLength];
        hash32.AsSpan(0, byteLength).CopyTo(result);
        return result;
    }

    private static string FormatSingleKeyEncryptionSummary(TestGatewayTransformOptions options)
    {
        var s = options.DataEncryptionAlgorithm.ToString();
        return options.DataEncryptionAlgorithm switch {
            EncryptionAlgorithm.AesGcm => $"{s} ({(int)options.DataAesGcmKeySize}-bit key)",
            EncryptionAlgorithm.AesCcm => $"{s} ({(int)options.DataAesGcmKeySize}-bit key)",
            EncryptionAlgorithm.AesSiv => $"{s} ({(int)options.DataAesSivKeySize}-bit key)",
            _ => s
        };
    }

    private static string FormatTwoKeyEncryptionSummary(TestGatewayTransformOptions options)
    {
        static string FormatAlg(EncryptionAlgorithm a, AesGcmKeySizeBits gcm, AesSivKeySizeBits siv)
            => a switch {
                EncryptionAlgorithm.AesGcm => $"{a} ({(int)gcm}-bit)",
                EncryptionAlgorithm.AesCcm => $"{a} ({(int)gcm}-bit)",
                EncryptionAlgorithm.AesSiv => $"{a} ({(int)siv}-bit)",
                _ => a.ToString()
            };

        var data = FormatAlg(options.DataEncryptionAlgorithm, options.DataAesGcmKeySize, options.DataAesSivKeySize);
        var key = FormatAlg(options.KeyEncryptionAlgorithm, options.KeyAesGcmKeySize, options.KeyAesSivKeySize);
        return $"{data} data / {key} key (two-key)";
    }

    private static string BuildEncryptedName(string fileName, string extension)
        => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? fileName : fileName + extension;

    private static string BuildDecryptedName(string fileName, string extension)
        => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? fileName[..^extension.Length] : fileName + ".decrypted";

    private static string BuildDecompressedName(string fileName, string extension)
        => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? fileName[..^extension.Length] : fileName + ".decompressed";

    private static KeyValuePair<string, string> Detail(string key, string value) => new(key, value);

    private static string ToFileSize(long bytes) => FileSizeUnitInfo.FormatBestFitAbbreviation(bytes, lowercaseAbbreviation: false);

    private sealed record TestGatewayStepResult(string FileName, byte[] Bytes, IReadOnlyList<KeyValuePair<string, string>> Details);
}