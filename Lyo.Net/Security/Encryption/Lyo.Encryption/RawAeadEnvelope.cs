using Lyo.Common.Core.Security;
using Lyo.Exceptions;

namespace Lyo.Encryption;

/// <summary>
/// Headerless single-shot AEAD blob: nonce (or synthetic IV), optional tag, then ciphertext. Use this when a foreign client must decrypt with a stock AES-GCM /
/// ChaCha20-Poly1305 / AES-SIV API. Lyo's default <see cref="SingleShotEnvelope" /> is a different layout and is not interchangeable.
/// </summary>
/// <remarks>
/// <para>
/// Layout: <c>[nonce:n][authenticator:n][payload:n]</c>. Algorithms with a synthetic IV instead of a nonce and tag (AES-SIV) store the IV in the nonce field with a
/// zero-length authenticator and write into <see cref="EnvelopeBuffer.Body" />, which is RFC 5297 <c>SIV || ciphertext</c>.
/// </para>
/// </remarks>
public static class RawAeadEnvelope
{
    /// <summary>Newly allocated raw blob: backing array plus the regions the caller still needs to fill.</summary>
    public readonly struct EnvelopeBuffer
    {
        internal EnvelopeBuffer(byte[] buffer, int nonceLength, int authenticatorLength, int payloadLength)
        {
            Buffer = buffer;
            NonceLength = nonceLength;
            AuthenticatorLength = authenticatorLength;
            PayloadLength = payloadLength;
        }

        /// <summary>Finished blob. Return this from <c>EncryptRaw</c> after the AEAD call fills the regions below.</summary>
        public byte[] Buffer { get; }

        /// <summary>Start of the nonce (or synthetic IV) region; always <c>0</c>.</summary>
        public int NonceOffset => 0;

        /// <summary>Width of the nonce (or synthetic IV) region.</summary>
        public int NonceLength { get; }

        /// <summary>Width of the tag region; <c>0</c> when the algorithm has no separate tag.</summary>
        public int AuthenticatorLength { get; }

        /// <summary>Width of the ciphertext payload region.</summary>
        public int PayloadLength { get; }

        /// <summary>Nonce region, filled with cryptographically strong random bytes unless the caller opted out.</summary>
        public Span<byte> Nonce => Buffer.AsSpan(0, NonceLength);

        /// <summary>Tag region the AEAD primitive writes.</summary>
        public Span<byte> Authenticator => Buffer.AsSpan(NonceLength, AuthenticatorLength);

        /// <summary>Ciphertext region the AEAD primitive writes.</summary>
        public Span<byte> Payload => Buffer.AsSpan(NonceLength + AuthenticatorLength, PayloadLength);

        /// <summary>Nonce, authenticator, and payload as one span, for primitives that emit a combined output (AES-SIV).</summary>
        public Span<byte> Body => Buffer.AsSpan();
    }

    /// <summary>Parsed raw blob plus offsets of the nonce, tag, and payload.</summary>
    public readonly struct EnvelopeHeader
    {
        internal EnvelopeHeader(int nonceLength, int authenticatorLength)
        {
            NonceLength = nonceLength;
            AuthenticatorLength = authenticatorLength;
        }

        /// <summary>Width of the nonce (or synthetic IV) region, already checked against the algorithm's expected width.</summary>
        public int NonceLength { get; }

        /// <summary>Width of the tag region; <c>0</c> when the algorithm has no separate tag.</summary>
        public int AuthenticatorLength { get; }

        /// <summary>Start of the ciphertext payload.</summary>
        public int PayloadOffset => NonceLength + AuthenticatorLength;

        /// <summary>Reads the nonce (or synthetic IV) from <paramref name="envelope" />.</summary>
        /// <param name="envelope">Blob this header was parsed from.</param>
        public ReadOnlySpan<byte> Nonce(ReadOnlySpan<byte> envelope) => envelope.Slice(0, NonceLength);

        /// <summary>Reads the authentication tag from <paramref name="envelope" />.</summary>
        /// <param name="envelope">Blob this header was parsed from.</param>
        public ReadOnlySpan<byte> Authenticator(ReadOnlySpan<byte> envelope) => envelope.Slice(NonceLength, AuthenticatorLength);

        /// <summary>Reads the ciphertext payload from <paramref name="envelope" />.</summary>
        /// <param name="envelope">Blob this header was parsed from.</param>
        public ReadOnlySpan<byte> Payload(ReadOnlySpan<byte> envelope) => envelope[PayloadOffset..];

        /// <summary>Reads nonce, authenticator, and payload as one span, for primitives that take a combined input (AES-SIV).</summary>
        /// <param name="envelope">Blob this header was parsed from.</param>
        public ReadOnlySpan<byte> Body(ReadOnlySpan<byte> envelope) => envelope;
    }

    /// <summary>
    /// Allocates a raw blob, leaving nonce, authenticator, and payload for the AEAD primitive. The nonce is filled with strong random bytes unless
    /// <paramref name="randomizeNonce" /> is <see langword="false" />.
    /// </summary>
    /// <param name="nonceLength">Nonce width, or synthetic-IV width for algorithms that use one.</param>
    /// <param name="authenticatorLength">Tag width, or <c>0</c> when the algorithm has none.</param>
    /// <param name="payloadLength">Plaintext length (equals ciphertext length for every algorithm here).</param>
    /// <param name="randomizeNonce"><see langword="false" /> when the algorithm fills the nonce region deterministically.</param>
    public static EnvelopeBuffer Allocate(int nonceLength, int authenticatorLength, int payloadLength, bool randomizeNonce = true)
    {
        ArgumentHelpers.ThrowIfNegative(nonceLength);
        ArgumentHelpers.ThrowIfNegative(authenticatorLength);
        ArgumentHelpers.ThrowIfNegative(payloadLength);
        var envelope = new EnvelopeBuffer(new byte[nonceLength + authenticatorLength + payloadLength], nonceLength, authenticatorLength, payloadLength);
        if (randomizeNonce && nonceLength > 0)
            CryptographicRandom.Fill(envelope.Nonce);

        return envelope;
    }

    /// <summary>
    /// Parses <paramref name="envelope" /> as a raw AEAD blob, checking that it holds the expected nonce (or synthetic IV) and authenticator before any key
    /// material is used. Every failure is an <see cref="InvalidDataException" />.
    /// </summary>
    /// <param name="envelope">Ciphertext to parse.</param>
    /// <param name="expectedNonceLength">Required nonce (or synthetic IV) width; the blob must be at least this plus <paramref name="authenticatorLength" />.</param>
    /// <param name="authenticatorLength">Tag width, or <c>0</c> when the algorithm has none.</param>
    /// <param name="nonceFieldName">Name used in errors, for example <c>nonce</c> or <c>synthetic IV</c>.</param>
    /// <exception cref="InvalidDataException">Blob is shorter than nonce plus authenticator.</exception>
    public static EnvelopeHeader Read(
        ReadOnlySpan<byte> envelope,
        int expectedNonceLength,
        int authenticatorLength,
        string nonceFieldName = "nonce")
    {
        ArgumentHelpers.ThrowIfNegative(expectedNonceLength);
        ArgumentHelpers.ThrowIfNegative(authenticatorLength);
        var min = expectedNonceLength + authenticatorLength;
        if (envelope.Length < min)
            throw new InvalidDataException($"Invalid raw AEAD data: truncated {nonceFieldName} or ciphertext.");

        return new(expectedNonceLength, authenticatorLength);
    }
}
