using System.Buffers.Binary;
using System.Text;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;

namespace Lyo.Encryption;

/// <summary>
/// Shared framing for every Lyo single-shot AEAD ciphertext: format version, optional key id, key version, and nonce-length, then the
/// algorithm's nonce, authenticator, and payload. Services only invoke the AEAD primitive; the layout lives here so algorithms stay wire-compatible.
/// </summary>
/// <remarks>
/// <para>
/// Layout: <c>[formatVersion:1][keyIdLength:4 LE][keyId:n][keyVersion:BinaryWriter string][nonceLength:4 LE][nonce:n][authenticator:n][payload:n]</c>. Algorithms with a synthetic
/// IV instead of a nonce and tag (AES-SIV) store the IV in the nonce field with a zero-length authenticator and write into <see cref="EnvelopeBuffer.Body" />, which covers the
/// IV and payload as one span.
/// </para>
/// </remarks>
public static class SingleShotEnvelope
{
    /// <summary>Max key id taken from an untrusted header, so a hostile length cannot force a huge allocation.</summary>
    public const int MaxKeyIdBytes = 1024;

    /// <summary>Newly allocated envelope: backing array plus the regions the caller still needs to fill.</summary>
    public readonly struct EnvelopeBuffer
    {
        internal EnvelopeBuffer(byte[] buffer, int nonceOffset, int nonceLength, int authenticatorLength, int payloadLength)
        {
            Buffer = buffer;
            NonceOffset = nonceOffset;
            NonceLength = nonceLength;
            AuthenticatorLength = authenticatorLength;
            PayloadLength = payloadLength;
        }

        /// <summary>Finished envelope with header already written. Return this from <c>Encrypt</c> after the AEAD call fills the regions below.</summary>
        public byte[] Buffer { get; }

        /// <summary>Start of the nonce (or synthetic IV) region.</summary>
        public int NonceOffset { get; }

        /// <summary>Width of the nonce (or synthetic IV) region.</summary>
        public int NonceLength { get; }

        /// <summary>Width of the tag region; <c>0</c> when the algorithm has no separate tag.</summary>
        public int AuthenticatorLength { get; }

        /// <summary>Width of the ciphertext payload region.</summary>
        public int PayloadLength { get; }

        /// <summary>Nonce region, filled with cryptographically strong random bytes unless the caller opted out.</summary>
        public Span<byte> Nonce => Buffer.AsSpan(NonceOffset, NonceLength);

        /// <summary>Tag region the AEAD primitive writes.</summary>
        public Span<byte> Authenticator => Buffer.AsSpan(NonceOffset + NonceLength, AuthenticatorLength);

        /// <summary>Ciphertext region the AEAD primitive writes.</summary>
        public Span<byte> Payload => Buffer.AsSpan(NonceOffset + NonceLength + AuthenticatorLength, PayloadLength);

        /// <summary>Nonce, authenticator, and payload as one span, for primitives that emit a combined output (AES-SIV).</summary>
        public Span<byte> Body => Buffer.AsSpan(NonceOffset);
    }

    /// <summary>Parsed header plus offsets of the regions that follow it.</summary>
    public readonly struct EnvelopeHeader
    {
        internal EnvelopeHeader(string? keyId, string? keyVersion, int nonceOffset, int nonceLength, int authenticatorLength)
        {
            KeyId = keyId;
            KeyVersion = keyVersion;
            NonceOffset = nonceOffset;
            NonceLength = nonceLength;
            AuthenticatorLength = authenticatorLength;
        }

        /// <summary>Key id stored at encrypt time, or <see langword="null" /> when the envelope has none.</summary>
        public string? KeyId { get; }

        /// <summary>Key version stored at encrypt time, or <see langword="null" /> when the envelope has none.</summary>
        public string? KeyVersion { get; }

        /// <summary>Start of the nonce (or synthetic IV) region inside the envelope.</summary>
        public int NonceOffset { get; }

        /// <summary>Width of the nonce (or synthetic IV) region, already checked against the algorithm's expected width.</summary>
        public int NonceLength { get; }

        /// <summary>Width of the tag region; <c>0</c> when the algorithm has no separate tag.</summary>
        public int AuthenticatorLength { get; }

        /// <summary>Start of the ciphertext payload.</summary>
        public int PayloadOffset => NonceOffset + NonceLength + AuthenticatorLength;

        /// <summary>Reads the nonce (or synthetic IV) from <paramref name="envelope" />.</summary>
        /// <param name="envelope">Envelope this header was parsed from.</param>
        public ReadOnlySpan<byte> Nonce(ReadOnlySpan<byte> envelope) => envelope.Slice(NonceOffset, NonceLength);

        /// <summary>Reads the authentication tag from <paramref name="envelope" />.</summary>
        /// <param name="envelope">Envelope this header was parsed from.</param>
        public ReadOnlySpan<byte> Authenticator(ReadOnlySpan<byte> envelope) => envelope.Slice(NonceOffset + NonceLength, AuthenticatorLength);

        /// <summary>Reads the ciphertext payload from <paramref name="envelope" />.</summary>
        /// <param name="envelope">Envelope this header was parsed from.</param>
        public ReadOnlySpan<byte> Payload(ReadOnlySpan<byte> envelope) => envelope[PayloadOffset..];

        /// <summary>Reads nonce, authenticator, and payload as one span, for primitives that take a combined input (AES-SIV).</summary>
        /// <param name="envelope">Envelope this header was parsed from.</param>
        public ReadOnlySpan<byte> Body(ReadOnlySpan<byte> envelope) => envelope[NonceOffset..];
    }

    /// <summary>Header byte count before the nonce, given the key id and version.</summary>
    /// <param name="keyIdByteCount">UTF-8 length of the key id, or <c>0</c> when absent.</param>
    /// <param name="keyVersion">Key version; empty when absent.</param>
    public static int GetHeaderSize(int keyIdByteCount, string keyVersion)
        => 1 + 4 + keyIdByteCount + BinaryWriterString.GetByteCount(keyVersion) + 4;

    /// <summary>
    /// Allocates an envelope and writes the header, leaving nonce, authenticator, and payload for the AEAD primitive. The nonce is filled with strong random bytes
    /// unless <paramref name="randomizeNonce" /> is <see langword="false" />.
    /// </summary>
    /// <param name="formatVersion">Format version byte written into the envelope.</param>
    /// <param name="keyId">Key id to record; omitted when <paramref name="keyVersion" /> is null or whitespace.</param>
    /// <param name="keyVersion">Key version to record; written as empty when null.</param>
    /// <param name="nonceLength">Nonce width, or synthetic-IV width for algorithms that use one.</param>
    /// <param name="authenticatorLength">Tag width, or <c>0</c> when the algorithm has none.</param>
    /// <param name="payloadLength">Plaintext length (equals ciphertext length for every algorithm here).</param>
    /// <param name="randomizeNonce"><see langword="false" /> when the algorithm fills the nonce region deterministically.</param>
    public static EnvelopeBuffer Allocate(
        byte formatVersion,
        string? keyId,
        string? keyVersion,
        int nonceLength,
        int authenticatorLength,
        int payloadLength,
        bool randomizeNonce = true)
    {
        var versionString = keyVersion ?? "";
        var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
        var headerSize = GetHeaderSize(keyIdBytes.Length, versionString);
        var buffer = new byte[headerSize + nonceLength + authenticatorLength + payloadLength];
        var o = 0;
        buffer[o++] = formatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(o, 4), keyIdBytes.Length);
        o += 4;
        if (keyIdBytes.Length > 0) {
            keyIdBytes.CopyTo(buffer.AsSpan(o));
            o += keyIdBytes.Length;
        }

        o += BinaryWriterString.Write(buffer.AsSpan(o), versionString);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(o, 4), nonceLength);
        o += 4;
        var envelope = new EnvelopeBuffer(buffer, o, nonceLength, authenticatorLength, payloadLength);
        if (randomizeNonce && nonceLength > 0)
            CryptographicRandom.Fill(envelope.Nonce);

        return envelope;
    }

    /// <summary>
    /// Parses the header of <paramref name="envelope" />, checking format version, key-id length, and nonce width before any key material is used. Every failure is an
    /// <see cref="InvalidDataException" /> naming the field that was short or wrong.
    /// </summary>
    /// <param name="envelope">Ciphertext to parse.</param>
    /// <param name="expectedFormatVersion">Format version the service accepts.</param>
    /// <param name="expectedNonceLength">Required nonce (or synthetic IV) width; the header value must match exactly.</param>
    /// <param name="authenticatorLength">Tag width, or <c>0</c> when the algorithm has none.</param>
    /// <param name="nonceFieldName">Name used in errors, for example <c>nonce</c> or <c>synthetic IV</c>.</param>
    /// <exception cref="InvalidDataException">Envelope is truncated, or format version or nonce width is unexpected.</exception>
    public static EnvelopeHeader Read(
        ReadOnlySpan<byte> envelope,
        byte expectedFormatVersion,
        int expectedNonceLength,
        int authenticatorLength,
        string nonceFieldName = "nonce")
    {
        var o = 0;
        if (envelope.Length < 1)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for format version.");

        var firstByte = envelope[o++];
        if (firstByte != expectedFormatVersion)
            throw new InvalidDataException($"Invalid encrypted data format: expected format version {expectedFormatVersion}, got {firstByte}.");

        if (envelope.Length - o < 4)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyId length.");

        var keyIdLength = BinaryPrimitives.ReadInt32LittleEndian(envelope.Slice(o, 4));
        o += 4;
        if (keyIdLength < 0 || keyIdLength > MaxKeyIdBytes)
            throw new InvalidDataException($"Invalid key ID length: {keyIdLength}. Maximum allowed: {MaxKeyIdBytes} bytes.");

        string? keyId = null;
        if (keyIdLength > 0) {
            if (envelope.Length - o < keyIdLength)
                throw new InvalidDataException("Invalid encrypted data format: keyId length exceeds remaining data.");

            keyId = Encoding.UTF8.GetString(envelope.Slice(o, keyIdLength).ToArray());
            o += keyIdLength;
        }

        if (o >= envelope.Length)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyVersion.");

        var keyVersion = BinaryWriterString.Read(envelope[o..], out var versionBytes);
        o += versionBytes;
        if (string.IsNullOrWhiteSpace(keyVersion))
            keyVersion = null;

        if (envelope.Length - o < 4)
            throw new InvalidDataException($"Invalid encrypted data format: insufficient data for {nonceFieldName} length.");

        var nonceLength = BinaryPrimitives.ReadInt32LittleEndian(envelope.Slice(o, 4));
        o += 4;
        ArgumentHelpers.ThrowIfNotInRange(
            nonceLength, expectedNonceLength, expectedNonceLength, nameof(envelope),
            $"Invalid {nonceFieldName} length: {nonceLength}. Expected {expectedNonceLength} bytes.");

        if (envelope.Length - o < nonceLength + authenticatorLength)
            throw new InvalidDataException($"Invalid encrypted data format: truncated {nonceFieldName} or ciphertext.");

        return new(keyId, keyVersion, o, nonceLength, authenticatorLength);
    }
}
