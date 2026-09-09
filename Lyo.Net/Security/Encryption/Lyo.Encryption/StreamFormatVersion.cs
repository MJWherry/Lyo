namespace Lyo.Encryption;

/// <summary>Stream-format version used when encrypting and decrypting.</summary>
public enum StreamFormatVersion : byte
{
    /// <summary>Unrecognized or unsupported version.</summary>
    Unknown = 0,

    /// <summary>
    /// Stream format V1. A random per-stream nonce prefix sits in the (authenticated) header; per-chunk nonces come from a local counter, so chunks
    /// cannot be reordered, replayed, or dropped. Frames are <c>[lengthAndFinalFlag:4][ciphertext][tag]</c>; the high bit of the little-endian length marks the last chunk
    /// (truncation detection). Each chunk authenticates the whole stream header (plus any caller AAD) as AAD.
    /// </summary>
    V1 = 1
}