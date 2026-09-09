namespace Lyo.Parameters;

/// <summary>Projection helpers shared by feature mappers that turn parameter entities into responses.</summary>
public static class LyoParameterMapper
{
    /// <summary>Placeholder returned in place of a secret parameter's value.</summary>
    public const string MaskedPlaceholder = "***";

    /// <summary>Returns <paramref name="value" /> as-is for plaintext parameters, or <see cref="MaskedPlaceholder" /> when the parameter is encrypted.</summary>
    /// <param name="value">Plaintext value as stored.</param>
    /// <param name="encryptedValue">Ciphertext, when the parameter is a secret.</param>
    public static string? MaskValue(string? value, byte[]? encryptedValue) => encryptedValue is not null ? MaskedPlaceholder : value;

    /// <summary>Always returns null: ciphertext is decrypted server-side and never travels on a response.</summary>
    /// <param name="encryptedValue">Ciphertext as stored; accepted so call sites read as a projection of the entity field.</param>
    public static byte[]? MaskEncryptedValue(byte[]? encryptedValue) => null;
}
