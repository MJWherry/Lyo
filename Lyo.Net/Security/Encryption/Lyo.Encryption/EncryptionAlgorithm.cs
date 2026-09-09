namespace Lyo.Encryption;

/// <summary>Algorithm used to encrypt payload data.</summary>
public enum EncryptionAlgorithm
{
    /// <summary>AES-GCM (Galois/Counter Mode): authenticated encryption (confidentiality plus integrity).</summary>
    AesGcm,

    /// <summary>ChaCha20-Poly1305: modern AEAD tuned for high throughput.</summary>
    ChaCha20Poly1305,

    /// <summary>Hybrid AES-GCM plus RSA key exchange: RSA wraps the session key; AES-GCM seals the payload.</summary>
    AesGcmRsa,

    /// <summary>RSA (Rivest–Shamir–Adleman) public-key encryption.</summary>
    Rsa,

    /// <summary>AES-CCM AEAD (nonce and tag layout matches the other symmetric envelope formats).</summary>
    AesCcm,

    /// <summary>AES-SIV synthetic-IV AEAD (RFC 5297).</summary>
    AesSiv,

    /// <summary>XChaCha20-Poly1305 with a 24-byte nonce.</summary>
    XChaCha20Poly1305
}