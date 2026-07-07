namespace Lyo.Encryption;

/// <summary>
/// Core interface for encryption services. Composes <see cref="IEncryptor" /> and <see cref="IDecryptor" /> into the full bidirectional contract that all encryption services
/// must implement. For additional helper methods (string, file operations), see EncryptionServiceBase.
/// </summary>
/// <remarks>
/// <para>
/// Pass <c>keyId</c> to resolve key material from <see cref="Lyo.Keystore.IKeyStore" /> when the implementation is constructed with a store; pass <c>key</c> for inline
/// symmetric keys. Streaming helpers write a small versioned header then length-prefixed ciphertext chunks — see <see cref="EncryptionServiceBase" /> source for the exact frame
/// layout.
/// </para>
/// <para>
/// Keys used with 96-bit-nonce AEADs (AES-GCM, ChaCha20-Poly1305, AES-CCM) should be rotated well before ~2^32 encrypt operations (single-shot calls or streams) to keep the
/// random-nonce collision probability negligible; use XChaCha20-Poly1305 or AES-SIV for very high-volume keys. See <see cref="EncryptionServiceBase" /> remarks for details.
/// </para>
/// </remarks>
public interface IEncryptionService : IEncryptor, IDecryptor;