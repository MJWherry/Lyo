namespace Lyo.Encryption;

/// <summary>Represents the encryption algorithm used for data encryption.</summary>
public enum EncryptionAlgorithm
{
    /// <summary>AES-GCM (Advanced Encryption Standard - Galois/Counter Mode) Authenticated encryption algorithm providing confidentiality and integrity.</summary>
    AesGcm,

    /// <summary>ChaCha20-Poly1305 Modern authenticated encryption algorithm with high performance.</summary>
    ChaCha20Poly1305,

    /// <summary>AES-GCM with RSA key exchange Hybrid encryption using RSA for key exchange and AES-GCM for data encryption.</summary>
    AesGcmRsa,

    /// <summary>RSA (Rivest-Shamir-Adleman) Asymmetric encryption algorithm.</summary>
    Rsa,

    /// <summary>AES-CCM authenticated encryption (nonce + tag layout compatible with other symmetric envelope formats).</summary>
    AesCcm,

    /// <summary>AES-SIV synthetic-IV authenticated encryption (RFC 5297).</summary>
    AesSiv,

    /// <summary>XChaCha20-Poly1305 (extended 24-byte nonce).</summary>
    XChaCha20Poly1305
}