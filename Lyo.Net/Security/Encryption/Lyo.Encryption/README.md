# Lyo.Encryption

Authenticated encryption for .NET: symmetric AEAD (AES-GCM, ChaCha20-Poly1305, XChaCha20-Poly1305, AES-CCM, AES-SIV), RSA and AES-GCM + RSA hybrids, plus envelope / two-key flows via `ITwoKeyEncryptionService`.

Primary contracts: `IEncryptionService` (single key), `ITwoKeyEncryptionService` (per-operation DEK wrapped by a KEK), and `EncryptionServiceBase` (streaming, string, and file helpers). Keys can be inline or resolved from `Lyo.KeyStore` by `keyId`.

For architecture, threat model, and operational checklists, see the [Security/Encryption README](../README.md). This file covers this assembly's types and methods.

## Features

### Algorithms

Confidentiality + integrity (authenticated tags). Tampering surfaces as `DecryptionFailedException`.

- Symmetric AEAD: AES-GCM, ChaCha20-Poly1305, XChaCha20-Poly1305, AES-CCM, AES-SIV
- RSA encrypt/decrypt and AES-GCM + RSA hybrid
- Envelope / two-key via `ITwoKeyEncryptionService`

### Keying

- Inline `byte[] key` / `byte[] kek`, or `IKeyStore` lookup by `keyId`
- Versioned decrypt / rotation on two-key paths

### I/O

- **Streaming.** `EncryptToStreamAsync` / `DecryptToStreamAsync` for large payloads (framed wire format)
- **Files.** `EncryptToFileAsync`, `DecryptFromFileAsync`, and stream-to-file variants
- **Strings.** `EncryptString` / `DecryptString` with per-direction encoding (UTF-8 by default)

### Integration

- DI helpers for RSA / AES-GCM+RSA, keyed `ITwoKeyEncryptionService` + `IKeyStore`
- Algorithm discovery via `EncryptionAlgorithm` / `EncryptionAlgorithmDiscovery`
- Non-throwing `EncryptionResult` / `DecryptionResult` ([`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md))
- `SecurityUtilities` for buffer zeroing and constant-time compare. Not KDFs. See [`Lyo.KeyStore`](../Lyo.KeyStore/README.md).

## Examples

### Keyed two-key (recommended)

```csharp
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.KeyStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

const string keyName = "primary";

// Configure key store via lambda (read secrets from IConfiguration inside configure)
services.AddKeyedLocalKeyStore(keyName, store =>
{
    store.UpdateKeyFromString("default-key", "replace-in-production");
});

// Or inline factory with IConfiguration (custom IKeyStore types)
services.AddEncryptionServiceKeyed<MyKeyStore>(
    keyName,
    sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var ks = new MyKeyStore(sp);
        ks.UpdateKeyFromString("default-key", config["Encryption:KekSecret"]!);
        return ks;
    },
    aesGcmKeySize: AesGcmKeySizeBits.Bits256);

// AES-GCM DEK + KEK (built into this package)
services.AddEncryptionServiceKeyed(keyName, keyStoreName: keyName);

// Mixed algorithms (examples)
services.AddEncryptionServiceKeyed<XChaCha20Poly1305EncryptionService, AesGcmEncryptionService>(keyName, keyName);

// Resolve
var envelope = serviceProvider.GetRequiredKeyedService<ITwoKeyEncryptionService>(keyName);
```

### Unkeyed (single algorithm / cache helper)

```csharp
services.AddLocalKeyStore(ks => ks.UpdateKeyFromString("k", "dev-secret"));
// Built-in types use core keyed helpers; addons:
// services.AddAesCcmEncryption(); // from Lyo.Encryption.AesCcm
services.AddDefaultEncryptionService<AesGcmEncryptionService>(); // after registering that concrete
```

### RSA / hybrid

```csharp
services.AddRsaEncryption(publicPemPath: "keys/public.pem", privatePemPath: "keys/private.pem");
services.AddAesGcmRsaEncryption(publicPemPath: "keys/public.pem", privatePemPath: "keys/private.pem");
```

## Benchmarks

AES-GCM encrypts 10 MB in ~5 ms with gigabyte-class throughput.

- Portfolio suite: `encryption`
- [AES-GCM encrypt](/benchmarks/encryption)
- [Benchmark summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)

## Service matrix

| Type | Role |
| ------------------------------------- | ----------------------------------------------------------------------------- |
| `AesGcmEncryptionService` | AES-GCM. Key size via `AesGcmKeySizeBits`. |
| `ChaCha20Poly1305EncryptionService` | ChaCha20-Poly1305 (IETF nonce) |
| `XChaCha20Poly1305EncryptionService` | XChaCha20-Poly1305 (extended nonce) |
| `AesCcmEncryptionService` | AES-CCM |
| `AesSivEncryptionService` | AES-SIV (misuse-resistant synthetic IV) |
| `RsaEncryptor` / `RsaDecryptor` | RSA encrypt (public key) / decrypt (private key), chunked for large plaintext |
| `AesGcmRsaEncryptionService` | Hybrid: RSA wraps AES key, AES-GCM protects payload |
| `TwoKeyEncryptionService<TKek, TDek>` | Envelope: random DEK per operation, KEK encrypts DEK |

Concrete types live under `AesGcm/`, `ChaCha20Poly1305/`, `Symmetric/Aes/*`, `Symmetric/ChaCha/*`, `Rsa/`, `AesGcmRsa/`, and `TwoKey/`.

## IEncryptionService (single-key path)

- `Encrypt` / `Decrypt` on `byte[]`, `ReadOnlySpan<byte>`, or slice overloads
- `EncryptString` / `DecryptString`
- `EncryptToStreamAsync` / `DecryptToStreamAsync`. Output begins with a small header (format version, algorithm id, reserved bytes) followed by length-prefixed encrypted chunks (default plaintext chunk size 1 MiB, configurable)
- `EncryptToFileAsync` / `DecryptFromFileAsync`

## ITwoKeyEncryptionService (envelope)

- `Encrypt` returns `TwoKeyEncryptionResult`: ciphertext + encrypted DEK + `KeyId` / `KeyVersion` (+ optional salt metadata)
- `Decrypt` takes ciphertext and encrypted DEK separately
- `EncryptStreamAsync` / `DecryptToStreamAsync`. Combined stream layout: encrypted DEK first, then chunked ciphertext. See XML on `TwoKeyEncryptionService` for format notes.
- `ReEncryptDek` / `ReEncryptDekAsync`. Rotate or migrate KEK without re-encrypting bulk data.

## Thread safety

`EncryptionServiceBase` documents that multiple threads may call the same instance concurrently. Each invocation uses its own cryptographic context. If `IKeyStore` or other dependencies are not thread-safe, synchronize or scope lifetimes accordingly.

## Dependency injection (this assembly)

Register `Microsoft.Extensions.DependencyInjection.Abstractions` (already referenced by this package on netstandard2.0 and net10.0). Algorithm addons (`Lyo.Encryption.AesCcm`, `.AesSiv`, `.XChaCha20Poly1305`) add their own `Add*Encryption` helpers. Keys come from [`Lyo.KeyStore`](../Lyo.KeyStore/README.md).

## Registration overview

| Call | Registers |
| --------------------------------------------- | ------------------------------------------------------------------------- |
| `AddLocalKeyStore(configure)` | `LocalKeyStore` + unkeyed `IKeyStore` |
| `AddKeyedLocalKeyStore(key, configure)` | Per-key `LocalKeyStore` + `IKeyStore` |
| `AddEncryptionServiceKeyed(...)` | Keyed DEK/KEK concretes, `IEncryptionService`, `ITwoKeyEncryptionService` |
| `AddAesCcmEncryption()` (addon) | Unkeyed `AesCcmEncryptionService` only |
| `AddDefaultEncryptionService<T>()` | Unkeyed `IEncryptionService` → `T` |
| `AddDefaultTwoKeyEncryptionService<T>()` | Unkeyed `ITwoKeyEncryptionService` → `T` (rare) |
| `AddRsaEncryption` / `AddAesGcmRsaEncryption` | Scoped RSA / hybrid services (paths or PFX) |

Unkeyed addon methods do not register `IEncryptionService` until you call `AddDefaultEncryptionService<TConcrete>()`. File storage and envelope encryption should use keyed registration, which includes `ITwoKeyEncryptionService`.

## Keyed two-key (recommended)

`AddEncryptionServiceKeyed` overloads accept an existing keyed key-store name, or register the store via `Func<IServiceProvider, TKeyStore>`. Generic overloads support different DEK vs KEK types when both implement `IEncryptionService` and are built from `IKeyStore`. See source for the built-in type matrix.

## Configuration notes

- **Service options.** `EncryptionServiceOptions` (`MaxInputSize`, `FileExtension`, `AesGcmKeySize`, and others) are set on concrete service constructors today. Use algorithm parameters on `AddEncryptionServiceKeyed` (`aesGcmKeySize`) or construct services manually for advanced cases.
- **Secrets and key material.** Use `AddLocalKeyStore` / `AddKeyedLocalKeyStore` with `configure => { ... }` and read `IConfiguration` inside that callback, the same pattern as other Lyo apps. There is no `AddEncryptionServiceFromConfiguration` on this package. Bind appsettings in the key-store configure delegate or in a custom `IKeyStore` factory.

## Options

`EncryptionServiceOptions` (per concrete service):

| Property | Typical use |
| --------------------------------- | -------------------------------------------------------------------- |
| `FileExtension` | Suffix for encrypted artifacts (required non-empty on base ctor) |
| `MinInputSize` / `MaxInputSize` | Enforced on encrypt paths |
| `CurrentFormatVersion` | Stream/header version. Defaults align with `StreamFormatVersion.V1`. |
| `AesGcmKeySize` / `AesSivKeySize` | Algorithm-specific key material where applicable |

## Result and error types

- `Lyo.Encryption.Models.EncryptionResult` / `DecryptionResult.` `Result<byte[]>` with key metadata for APIs that avoid exceptions.
- `Lyo.Encryption.EncryptionErrorCodes.` Stable error-code constants paired with `EncryptionResult` / `DecryptionResult`, for example `KEY_NOT_FOUND`, `DECRYPTION_FAILED`, `INVALID_HEADER`. Use these instead of string-matching exception messages.
- `DecryptionFailedException`, `EncryptionException`, `InvalidDataException`, `ArgumentOutsideRangeException.` See `IEncryptionService` XML for which throws apply.

## Helpers and validation

- `Lyo.Encryption.TwoKey.TwoKeyDekValidation.` Validates `DekAlgorithm` + DEK key-material byte length for all supported symmetric algorithms. Used on decrypt to reject mismatched envelopes before any cryptographic call.
- `Lyo.Encryption.RsaKeyLoader.` PEM/PFX RSA key loading helper. Uses BouncyCastle on `netstandard2.0` and `RSA.ImportFromPem` / `X509Certificate2` on `net10.0`. Invoked transitively by `RsaEncryptor` / `RsaDecryptor` / `AesGcmRsaEncryptionService` constructors but exposed for callers that want to share a loaded key across services.
- `Lyo.Encryption.ISymmetricKeyMaterialSize.` Implemented by every symmetric `IEncryptionService` to advertise its accepted key-material sizes in bytes, e.g. AES-GCM = `{16, 24, 32}`, XChaCha20-Poly1305 = `{32}`. `TwoKeyDekValidation` and key-store validators rely on this.
- `Encrypt` / `Decrypt` `ReadOnlySpan<byte>` overloads on `IEncryptionService`. Zero-copy entry points for callers that already hold a contiguous buffer. The legacy `byte[]` overloads remain.
- `TwoKeyEncryptionResult` fields. Beyond ciphertext, the record carries `EncryptedDek`, `DekKeyMaterialBytes`, `KeyEncryptionKeySalt`, `KeyId`, `KeyVersion`, and `TotalSize`. The legacy `Lyo.Encryption.Models.TwoKeyEncryptionResult` is preserved for callers that still consume the result-builder shape.

## Streaming two-key

- `EncryptStreamAsync(Stream input, Stream output, ...)` / `DecryptStreamAsync(Stream input, Stream output, TwoKeyEncryptionResult metadata, ...)`. Operates on an existing `TwoKeyEncryptionResult` (carries the wrapped DEK, key id/version, salt).
- `EncryptToStreamAsync(...)` / `DecryptToStreamAsync(...)`. Writes the combined wire format (encrypted-DEK header + chunked ciphertext) to a single output stream and reads it back without external metadata.

## Upgrade checklist (short)

- Confirm nonce / IV uniqueness policy for each algorithm when integrating custom stores. See parent `README.md`.
- After dependency bumps (BouncyCastle, Dorssel AES extras), run `Lyo.Encryption.Benchmarks` in Release with algorithm-specific filters.
- Validate FIPS / regional requirements externally. This library follows general best practices but does not certify every jurisdiction.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (direct, third-party, netstandard2.0)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft, net10.0, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (direct, microsoft, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)