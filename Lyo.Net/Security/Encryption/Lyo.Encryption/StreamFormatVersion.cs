namespace Lyo.Encryption;

/// <summary>Represents the version of the stream format used for encryption/decryption.</summary>
public enum StreamFormatVersion : byte
{
    /// <summary>Unknown or unsupported version.</summary>
    Unknown = 0,

    /// <summary>
    /// Version 1 of the stream format (initial two-key layout): after the format byte, DEK and KEK algorithm IDs, includes <c>DekKeyMaterialBytes</c> before key id / encrypted DEK
    /// fields.
    /// </summary>
    V1 = 1
}