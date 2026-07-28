namespace Lyo.Encryption;

/// <summary>Represents the version of the stream format used for encryption/decryption.</summary>
public enum StreamFormatVersion : byte
{
    /// <summary>Unknown or unsupported version.</summary>
    Unknown = 0,

    /// <summary>
    /// Version 1 of the stream format. The per-stream random nonce prefix lives in the (authenticated) header and per-chunk nonces are derived from a local counter, so chunks
    /// cannot be reordered, replayed, or dropped. Chunks are framed as <c>[lengthAndFinalFlag:4][ciphertext][tag]</c> where the top bit of the little-endian length marks the final chunk
    /// (detecting truncation). Every chunk authenticates the full stream header (plus any caller-supplied associated data) as AAD.
    /// </summary>
    V1 = 1
}