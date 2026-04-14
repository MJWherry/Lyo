namespace Lyo.Encryption;

/// <summary>Represents the version of the encryption header format.</summary>
public enum EncryptionHeaderVersion : byte
{
    /// <summary>Unknown or unsupported version.</summary>
    Unknown = 0,

    /// <summary>Version 1 of the encryption header format.</summary>
    V1 = 1
}