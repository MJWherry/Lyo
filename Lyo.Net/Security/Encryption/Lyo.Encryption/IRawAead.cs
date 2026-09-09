using Lyo.Encryption.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption;

/// <summary>
/// Single-shot AEAD without the Lyo envelope: <c>[nonce][tag][ciphertext]</c> (AES-SIV: RFC 5297 <c>SIV || ciphertext</c>). Stock AES-GCM / ChaCha /
/// AES-SIV clients can decrypt after slicing those regions. Not on <see cref="IEncryptionService" /> — framed <c>Encrypt</c> / <c>Decrypt</c> stay the default, and
/// file storage / config / jobs must not pick this up by accident.
/// </summary>
/// <remarks>
/// <para>
/// Raw blobs do not record key id or version. <c>keyId</c> uses the <em>current</em> KeyStore key; after rotation pass <c>keyVersion</c> on decrypt (or the raw
/// key bytes). Caller AAD is authenticated, not stored.
/// </para>
/// </remarks>
public interface IRawAead
{
    /// <summary>Encrypts a byte array into a headerless AEAD blob.</summary>
    /// <param name="bytes">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the current key.</param>
    /// <param name="associatedData">
    /// Optional AAD authenticated with the ciphertext (not encrypted, not stored). The same value must be passed at decrypt time or authentication fails.
    /// </param>
    /// <returns>Raw blob: nonce, tag, then ciphertext.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when bytes is empty (below MinInputSize), exceeds MaxInputSize, or the key length is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore).</exception>
    byte[] EncryptRaw(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Encrypts a span into a headerless AEAD blob.</summary>
    /// <param name="plaintext">Plaintext. Must not be empty.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the current key.</param>
    /// <param name="associatedData">Optional AAD authenticated with the ciphertext (not encrypted or stored); see <see cref="EncryptRaw(byte[], string, byte[], byte[])" />.</param>
    /// <returns>Raw blob: nonce, tag, then ciphertext.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when plaintext is empty (below MinInputSize), exceeds MaxInputSize, or the key length is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore).</exception>
    byte[] EncryptRaw(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <summary>Decrypts a headerless AEAD blob.</summary>
    /// <param name="encryptedBytes">Raw blob from <see cref="EncryptRaw(byte[], string, byte[], byte[])" />.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="keyVersion">
    /// Optional KeyStore version. Null uses the current key for <paramref name="keyId" />. Required after rotation unless <paramref name="key" /> is supplied —
    /// the blob does not record which version produced it.
    /// </param>
    /// <param name="associatedData">Optional AAD that was authenticated at encrypt time. Must equal the encrypt-time value exactly (null if none was used).</param>
    /// <returns>Plaintext.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is shorter than nonce plus tag, or exceeds MaxInputSize.</exception>
    /// <exception cref="InvalidDataException">Thrown when the blob is truncated below the algorithm's nonce and tag widths.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore).</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, mismatched AAD, or tampering.</exception>
    byte[] DecryptRaw(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, string? keyVersion = null, byte[]? associatedData = null);

    /// <summary>Decrypts a headerless AEAD blob from a span.</summary>
    /// <param name="encrypted">Raw blob from <see cref="EncryptRaw(ReadOnlySpan{byte}, string, byte[], byte[])" />.</param>
    /// <param name="keyId">KeyStore id. Null means use <paramref name="key" /> as-is.</param>
    /// <param name="key">Optional raw key. When null and <paramref name="keyId" /> is set, the KeyStore supplies the key.</param>
    /// <param name="keyVersion">Optional KeyStore version; see <see cref="DecryptRaw(byte[], string, byte[], string, byte[])" />.</param>
    /// <param name="associatedData">Optional AAD authenticated with the ciphertext; see <see cref="DecryptRaw(byte[], string, byte[], string, byte[])" />.</param>
    /// <returns>Plaintext.</returns>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encrypted is shorter than nonce plus tag, or exceeds MaxInputSize.</exception>
    /// <exception cref="InvalidDataException">Thrown when the blob is truncated below the algorithm's nonce and tag widths.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no key is available (neither keyId nor key, or keyId missing from the KeyStore).</exception>
    /// <exception cref="DecryptionFailedException">Thrown on wrong key, damaged data, auth failure, mismatched AAD, or tampering.</exception>
    byte[] DecryptRaw(ReadOnlySpan<byte> encrypted, string? keyId = null, byte[]? key = null, string? keyVersion = null, byte[]? associatedData = null);
}
