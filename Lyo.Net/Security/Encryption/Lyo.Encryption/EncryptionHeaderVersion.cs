namespace Lyo.Encryption;

/// <summary>Version tag for the encryption header layout.</summary>
public enum EncryptionHeaderVersion : byte
{
    /// <summary>Unrecognized or unsupported version.</summary>
    Unknown = 0,

    /// <summary>First version of the encryption header layout.</summary>
    V1 = 1
}