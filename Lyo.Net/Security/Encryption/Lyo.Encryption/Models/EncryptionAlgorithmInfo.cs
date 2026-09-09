using Lyo.Common.Metadata.Records;

namespace Lyo.Encryption.Models;

/// <summary>
/// Per-algorithm AEAD wire facts: nonce and tag widths, default key length, and the smallest byte count that can be a legal single-shot envelope.
/// Services read this table instead of repeating constants, so a nonce-width change lives in one place.
/// </summary>
/// <remarks>
/// <para>
/// Kept in <c>Lyo.Encryption</c> rather than <c>Lyo.Common.Core</c>: <see cref="FileTypeInfo" /> owns file/MIME identity; nonce widths and tag sizes are
/// crypto wire format Core should not know. <see cref="FileType" /> points back at the Core catalog row for the matching ciphertext extension.
/// </para>
/// <para>
/// Rows exist for AEADs that write the shared single-shot envelope. <see cref="EncryptionAlgorithm.Rsa" /> and <see cref="EncryptionAlgorithm.AesGcmRsa" />
/// are omitted: they wrap keys asymmetrically and envelope size follows the RSA modulus, not a fixed nonce and tag.
/// </para>
/// </remarks>
public sealed record EncryptionAlgorithmInfo
{
    /// <summary>
    /// Envelope bytes before the nonce in the smallest case: format version (1), key-id length (4), empty key id (0), a
    /// <see cref="System.IO.BinaryWriter" />-style empty version string (1), and the nonce-length field (4).
    /// </summary>
    public const int MinEnvelopeHeaderBytes = 10;

    private EncryptionAlgorithmInfo(
        EncryptionAlgorithm kind,
        string name,
        int defaultKeyBytes,
        int nonceSize,
        int tagSize,
        int minSingleShotSize,
        bool supportsStreaming,
        bool nonceMisuseResistant,
        FileTypeInfo fileType)
    {
        Kind = kind;
        Name = name;
        DefaultKeyBytes = defaultKeyBytes;
        NonceSize = nonceSize;
        TagSize = tagSize;
        MinSingleShotSize = minSingleShotSize;
        SupportsStreaming = supportsStreaming;
        NonceMisuseResistant = nonceMisuseResistant;
        FileType = fileType;
    }

    /// <summary>Algorithm this row describes.</summary>
    public EncryptionAlgorithm Kind { get; }

    /// <summary>Standards-facing algorithm name for logs and diagnostics.</summary>
    public string Name { get; }

    /// <summary>Default key length in bytes when the caller does not choose one.</summary>
    public int DefaultKeyBytes { get; }

    /// <summary>Nonce width written into the envelope, or <c>0</c> when the algorithm uses a synthetic IV instead of a nonce.</summary>
    public int NonceSize { get; }

    /// <summary>Tag width in bytes, or <c>0</c> when the authenticator is the synthetic IV rather than a separate tag.</summary>
    public int TagSize { get; }

    /// <summary>
    /// Smallest byte count that can be a valid single-shot ciphertext; used as the lower bound on untrusted input. Anything shorter cannot hold framing plus
    /// authenticator, so it is rejected before key material is touched.
    /// </summary>
    public int MinSingleShotSize { get; }

    /// <summary>True when a chunked stream cryptor exists in addition to the single-shot path.</summary>
    public bool SupportsStreaming { get; }

    /// <summary>True when repeating a nonce under the same key is safe. Only AES-SIV qualifies; for the others a repeat is catastrophic.</summary>
    public bool NonceMisuseResistant { get; }

    /// <summary>Core catalog row for this algorithm's ciphertext extension and MIME type.</summary>
    public FileTypeInfo FileType { get; }

    /// <summary>Default ciphertext file extension for this algorithm, for example <c>.ag</c>.</summary>
    public string DefaultExtension => FileType.DefaultExtension;

    /// <summary>Per-message envelope bytes beyond the framing header: nonce plus authenticator.</summary>
    public int AuthenticatorOverheadBytes => NonceSize + TagSize;

    /// <summary>AES-GCM with a 96-bit nonce and 128-bit tag. Default symmetric algorithm.</summary>
    public static readonly EncryptionAlgorithmInfo AesGcm = new(
        EncryptionAlgorithm.AesGcm, "AES-GCM", 32, 12, 16, MinEnvelopeHeaderBytes + 12 + 16, true, false, FileTypeInfo.LyoAesGcm);

    /// <summary>ChaCha20-Poly1305 with a 96-bit nonce and 128-bit tag.</summary>
    public static readonly EncryptionAlgorithmInfo ChaCha20Poly1305 = new(
        EncryptionAlgorithm.ChaCha20Poly1305, "ChaCha20-Poly1305", 32, 12, 16, MinEnvelopeHeaderBytes + 12 + 16, true, false, FileTypeInfo.LyoChaCha20Poly1305);

    /// <summary>AES-CCM with a 96-bit nonce and 128-bit tag. Plaintext is capped well below the other AEADs because of its length-prefixed counter.</summary>
    public static readonly EncryptionAlgorithmInfo AesCcm = new(
        EncryptionAlgorithm.AesCcm, "AES-CCM", 32, 12, 16, MinEnvelopeHeaderBytes + 12 + 16, true, false, FileTypeInfo.LyoAesCcm);

    /// <summary>XChaCha20-Poly1305 with a 192-bit nonce, wide enough that random nonces do not realistically collide.</summary>
    public static readonly EncryptionAlgorithmInfo XChaCha20Poly1305 = new(
        EncryptionAlgorithm.XChaCha20Poly1305, "XChaCha20-Poly1305", 32, 24, 16, MinEnvelopeHeaderBytes + 24 + 16, true, false, FileTypeInfo.LyoXChaCha20Poly1305);

    /// <summary>
    /// AES-SIV (RFC 5297). Uses a 16-byte synthetic IV instead of a nonce and tag, so it needs no randomness and tolerates repeated plaintexts under one key. Minimum
    /// envelope size keeps the historical 27-byte guard rather than the 26 bytes framing alone would imply.
    /// </summary>
    public static readonly EncryptionAlgorithmInfo AesSiv = new(
        EncryptionAlgorithm.AesSiv, "AES-SIV", 64, 0, 16, 27, true, true, FileTypeInfo.LyoAesSiv);

    private static readonly EncryptionAlgorithmInfo[] AllEntries = [AesGcm, ChaCha20Poly1305, AesCcm, XChaCha20Poly1305, AesSiv];

    /// <summary>All AEAD algorithms that write the shared single-shot envelope.</summary>
    public static IReadOnlyList<EncryptionAlgorithmInfo> All => AllEntries;

    /// <summary>Row for <paramref name="kind" />, or <see langword="null" /> when the algorithm has no fixed-width envelope (RSA and AES-GCM+RSA).</summary>
    /// <param name="kind">Algorithm to look up.</param>
    public static EncryptionAlgorithmInfo? TryFromKind(EncryptionAlgorithm kind) => AllEntries.FirstOrDefault(entry => entry.Kind == kind);

    /// <summary>Row for <paramref name="kind" />.</summary>
    /// <param name="kind">Algorithm to look up.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind" /> has no fixed-width envelope (RSA and AES-GCM+RSA).</exception>
    public static EncryptionAlgorithmInfo FromKind(EncryptionAlgorithm kind)
        => TryFromKind(kind) ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Algorithm has no fixed-width AEAD envelope.");

    /// <summary>Row whose ciphertext extension matches <paramref name="extension" /> (leading dot optional), or <see langword="null" /> if none match.</summary>
    /// <param name="extension">Ciphertext extension, for example <c>.chacha</c>.</param>
    public static EncryptionAlgorithmInfo? TryFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        var normalized = extension!.Trim();
        if (!normalized.StartsWith(".", StringComparison.Ordinal))
            normalized = "." + normalized;

        return AllEntries.FirstOrDefault(entry => string.Equals(entry.DefaultExtension, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
