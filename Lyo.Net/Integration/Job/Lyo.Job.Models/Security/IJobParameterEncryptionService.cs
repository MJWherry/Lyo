namespace Lyo.Job.Models.Security;

/// <summary>Encrypts and decrypts sensitive job parameter values at rest.</summary>
public interface IJobParameterEncryptionService
{
    /// <summary>Whether a keyed <see cref="Lyo.Encryption.IEncryptionService" /> was registered.</summary>
    bool IsEncryptionEnabled { get; }

    /// <summary>Returns true when the parameter stores values in the encrypted column.</summary>
    bool UsesEncryptedStorage(byte[]? encryptedValueMarker);

    /// <summary>Encrypts plaintext into <paramref name="encryptedValue" /> and clears <paramref name="value" /> when encryption is enabled.</summary>
    void EncryptParameterValue(ref string? value, ref byte[]? encryptedValue);

    /// <summary>Decrypts an encrypted parameter value to plaintext.</summary>
    string? DecryptValue(byte[]? encryptedValue);

    /// <summary>Returns a masked placeholder suitable for API responses.</summary>
    string? MaskValue(string? value, byte[]? encryptedValueMarker);
}