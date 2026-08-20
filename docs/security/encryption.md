# Encryption design notes

Design-level notes on how Lyo frames and streams ciphertext. This complements the package-level [`Lyo.Encryption` README](../../Lyo.Net/Security/Encryption/README.md) (API, examples, algorithm matrix) and the security overview ([security/README.md](README.md)). The on-disk formats here are **Lyo-specific**. The algorithms are standard, but how bytes are framed is defined by this library.

## Algorithms

`Lyo.Encryption` multi-targets `net10.0` and `netstandard2.0` with the same supported algorithms and key sizes on both, so blobs encrypted on one target decrypt on the other when keys and formats match. On `net10.0` BCL primitives are used where available. On `netstandard2.0` BouncyCastle provides the same sizes and on-the-wire layout.

| Algorithm          | Key / nonce / tag                             |
|--------------------|-----------------------------------------------|
| AES-GCM            | 16/24/32-byte key; 12-byte nonce; 16-byte tag |
| ChaCha20-Poly1305  | 32-byte key; 12-byte nonce; 16-byte tag       |
| AES-CCM            | 16/24/32-byte key; 12-byte nonce; 16-byte tag |
| AES-SIV (RFC 5297) | 32/48/64-byte key material; deterministic     |
| XChaCha20-Poly1305 | 32-byte key; 24-byte nonce; 16-byte tag       |
| RSA (OAEP-SHA256)  | >= 2048-bit modulus (3072+ recommended)       |

`EncryptionAlgorithm` IDs are stable and embedded in the stream header so a blob self-describes its algorithm:

| ID | Algorithm           | Default extension |
|---:|---------------------|-------------------|
|  0 | `AesGcm`            | `.ag`             |
|  1 | `ChaCha20Poly1305`  | `.chacha`         |
|  2 | `AesGcmRsa`         | `.agr`            |
|  3 | `Rsa`               | `.rsa`            |
|  4 | `AesCcm`            |                   |
|  5 | `AesSiv`            |                   |
|  6 | `XChaCha20Poly1305` |                   |

## Streaming format

Large data is processed with single-pass streaming and no temporary files. A streamed blob is a header followed by a sequence of self-framed chunks.

### Stream header (single-key services)

```
[FormatVersion: 1][AlgorithmId: 1][KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion]
```

`FormatVersion` is the `StreamFormatVersion` enum (currently `V1 = 1`). The header parsing used by the tests is the authoritative reference for field order. See `ExtractChunkNonces` in [`StreamingChunkFormatTests`](../../Lyo.Net/Security/Encryption/Lyo.Encryption.Tests/StreamingChunkFormatTests.cs).

### Chunk frame

Each chunk is encoded by `AeadChunkCodec` in [`AeadStreamCryptor.cs`](../../Lyo.Net/Security/Encryption/Lyo.Encryption/Streaming/AeadStreamCryptor.cs) as a compact, contiguous frame:

```
[ciphertextLen: int32 LE][nonce: NonceSize][ciphertext][tag: TagSize]
```

- The 4-byte length prefix is the **plaintext** length of the chunk.
- The nonce is written per chunk. It is a per-stream random prefix combined with a per-chunk counter, so every chunk gets a unique nonce (verified by `Stream_PerChunkNonces_AreUnique`).
- The tag trails the ciphertext so `ciphertext||tag` is contiguous, which lets the decryptor pass a single slice to the AEAD primitive.
- Per-chunk work uses pooled buffers (`ArrayPool<byte>`). No per-chunk heap allocation. The `IAeadStreamCryptor` is built once per stream (key schedule built once) and driven sequentially. Instances are **not** thread-safe.

### Two-key (envelope) stream header

Envelope streams carry the wrapped DEK and key metadata in an extended header:

```
[FormatVersion: 1][DEKAlgorithmId: 1][KEKAlgorithmId: 1]
[KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion]
[KEKSaltLength: 4][KEKSalt][EncryptedDEKLength: 4][EncryptedDEK]
[DekKeyMaterialBytes: 1][Chunks...]
```

- A unique DEK is generated per operation and wrapped with the KEK resolved from the key store. Only the wrapped DEK is written.
- `KeyVersion` is an arbitrary string (not necessarily an integer), supporting key stores like KMS/Key Vault whose versions are opaque.
- `DekKeyMaterialBytes` records the DEK key size and is validated on decrypt (`TwoKeyDekValidation`).
- Default two-key file extensions append the suffix `2k` to the inner DEK service's extension (AES-GCM DEK -> `.ag2k`).

You can inspect a header without decrypting the body using `EncryptionHeader.Read(stream)`.

## Integrity and tamper behavior

Because every algorithm is AEAD, any modification to ciphertext, nonce, length framing, or tag fails authentication on decrypt. The test suite asserts:

- round-trips across many sizes and chunk boundaries (0, 1, 7, 16, 32, 48, 50, 1000 bytes at a 16-byte chunk size) for every symmetric algorithm;
- a flipped tag byte throws `DecryptionFailedException` (single-key and two-key);
- decrypting with a mismatched algorithm throws `InvalidDataException`;
- AES-SIV streaming is deterministic for the same key, plaintext, and chunk size (counter-only nonce, no random prefix).

## Byte-array (non-stream) format

For small payloads the non-streaming AES-GCM/ChaCha format is:

```
[FormatVersion: 1][KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion][nonceLength: 4][nonce][tag][ciphertext]
```

`KeyIdLength` is 0 when a direct key (not a `keyId`) was used.

## Operational guidance

- Use a managed `IKeyStore` in production. `LocalKeyStore` is in-memory/dev only.
- Prefer AES-GCM for bulk data on AES-NI hardware. Prefer ChaCha20-Poly1305 where AES acceleration is absent. Use hybrid (AES-GCM-RSA) to encrypt large blobs to a public key. Reserve pure RSA for small secrets / key wrapping.
- Rotate KEKs periodically and retain old versions for decryption.

See the [`Lyo.Encryption` README](../../Lyo.Net/Security/Encryption/README.md) for DI registration, key management, and the full example set, and the [benchmark summary](../../Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md) for measured throughput.
