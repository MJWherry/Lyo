namespace Lyo.Billing;

/// <summary>Encrypts invoice bill-to tax snapshots and biller remittance at rest.</summary>
public interface IBillingFieldEncryptionService
{
    /// <summary>True when an encryption service is registered on this host.</summary>
    bool IsEncryptionEnabled { get; }

    /// <summary>Encrypts <paramref name="plaintext" /> into <paramref name="encrypted" /> and clears plaintext when encryption is enabled.</summary>
    /// <param name="plaintext">In-memory value. Cleared after a successful encrypt.</param>
    /// <param name="encrypted">Ciphertext to persist.</param>
    void Encrypt(ref string? plaintext, ref byte[]? encrypted);

    /// <summary>Decrypts ciphertext. Returns null when there is nothing to decrypt or decryption fails.</summary>
    string? Decrypt(byte[]? encrypted);
}