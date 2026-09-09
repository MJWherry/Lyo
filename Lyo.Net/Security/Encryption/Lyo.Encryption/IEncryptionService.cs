namespace Lyo.Encryption;

/// <summary>
/// Bidirectional encryption contract: <see cref="IEncryptor" /> plus <see cref="IDecryptor" />. Every encryption service
/// implements this. String and file helpers live on EncryptionServiceBase.
/// </summary>
/// <remarks>
/// <para>
/// Supply <c>keyId</c> to load material from <see cref="Lyo.KeyStore.IKeyStore" /> when the service was built with a store; supply <c>key</c> for inline
/// symmetric keys. Streaming helpers emit a short versioned header then length-prefixed ciphertext chunks — see <see cref="EncryptionServiceBase" /> for the exact frame
/// layout.
/// </para>
/// <para>
/// Rotate keys used with 96-bit-nonce AEADs (AES-GCM, ChaCha20-Poly1305, AES-CCM) well before ~2^32 encrypts (single-shot or stream) so the
/// random-nonce collision chance stays negligible; prefer XChaCha20-Poly1305 or AES-SIV for very high-volume keys. Details in <see cref="EncryptionServiceBase" /> remarks.
/// </para>
/// </remarks>
public interface IEncryptionService : IEncryptor, IDecryptor;