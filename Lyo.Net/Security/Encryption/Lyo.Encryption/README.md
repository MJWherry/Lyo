# Lyo.Encryption

Production-oriented **authenticated encryption** for .NET: symmetric AEAD (**AES-GCM**, **ChaCha20-Poly1305**, **XChaCha20-Poly1305**, **AES-CCM**, **AES-SIV**), **RSA** and *
*AES-GCM +
RSA** hybrids, and **envelope / two-key** flows (**`ITwoKeyEncryptionService`**) composed from **`IEncryptionService`** implementations. Keys can be supplied inline or resolved
from
**`Lyo.Keystore`** via **`keyId`**.

The primary contracts are **`IEncryptionService`** (single key per operation), **`ITwoKeyEncryptionService`** (per-operation DEK wrapped by a KEK), and **`EncryptionServiceBase`**
(shared streaming, string, and file helpers). With XML doc generation enabled in the repo, IntelliSense surfaces the same summaries as this README for documented members.

For architecture, threat model, **`IKeyStore`** expectations, benchmarks, and operational checklists, see **[Security/Encryption `README.md`](../README.md)** — treat that document
as
the umbrella guide; this file focuses on **this assembly’s API surface**.

## Features

- **AEAD** – Confidentiality + integrity (authenticated tags); tampering surfaces as **`DecryptionFailedException`**
- **Key sources** – Optional **`byte[] key`** / **`byte[] kek`** or **`IKeyStore`** lookup by **`keyId`** (and version for two-key decrypt / rotation)
- **Streaming** – **`EncryptToStreamAsync`** / **`DecryptToStreamAsync`** chunk large payloads without materializing the whole ciphertext in memory (framed format on the wire)
- **Files** – **`EncryptToFileAsync`**, **`DecryptFromFileAsync`**, and stream-to-file variants
- **Strings** – **`EncryptString`** / **`DecryptString`** using the per-direction encoding (**`GetEncryptionEncoding`** / **`GetDecryptionEncoding`**, UTF-8 by default; set via * *`SetEncryptionEncoding`** / **`SetDecryptionEncoding`**)
- **DI helpers** – **`EncryptionServiceExtensions`**: RSA / AES-GCM+RSA registration, keyed **`ITwoKeyEncryptionService`** + keyed **`IKeyStore`**
- **Discovery** – **`EncryptionAlgorithm`**, **`EncryptionAlgorithmDiscovery`**, algorithm metadata on **`EncryptionServiceBase.AlgorithmKind`**
- **Non-throwing workflows** – **`EncryptionResult`** / **`DecryptionResult`** in **`Lyo.Encryption.Models`** ([`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md))
- **Utilities** – **`SecurityUtilities`** (buffer zeroing, constant-time compare) — **not** KDFs (see [`Lyo.Keystore`](../Lyo.Keystore/README.md))

## Examples

### Keyed two-key (recommended)

```csharp
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.Keystore;
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
| ----------------------------------------- | ----------------------------------------------------------------------------- |
| **`AesGcmEncryptionService`** | AES-GCM; key size via **`AesGcmKeySizeBits`** |
| **`ChaCha20Poly1305EncryptionService`** | ChaCha20-Poly1305 (IETF nonce) |
| **`XChaCha20Poly1305EncryptionService`** | XChaCha20-Poly1305 (extended nonce) |
| **`AesCcmEncryptionService`** | AES-CCM |
| **`AesSivEncryptionService`** | AES-SIV (misuse-resistant synthetic IV) |
| **`RsaEncryptor`** / **`RsaDecryptor`** | RSA encrypt (public key) / decrypt (private key), chunked for large plaintext |
| **`AesGcmRsaEncryptionService`** | Hybrid: RSA wraps AES key, AES-GCM protects payload |
| **`TwoKeyEncryptionService<TKek, TDek>`** | Envelope: random DEK per operation, KEK encrypts DEK |

Concrete types live under **`AesGcm/`**, **`ChaCha20Poly1305/`**, **`Symmetric/Aes/*`**, **`Symmetric/ChaCha/*`**, **`Rsa/`**, **`AesGcmRsa/`**, and **`TwoKey/`**.

## **`IEncryptionService`** (single-key path)

- **`Encrypt`** / **`Decrypt`** on **`byte[]`**, **`ReadOnlySpan<byte>`**, or slice overloads
- **`EncryptString`** / **`DecryptString`**
- **`EncryptToStreamAsync`** / **`DecryptToStreamAsync`** — output begins with a small header (format version, algorithm id, reserved bytes) followed by length-prefixed encrypted chunks (default plaintext chunk size **1 MiB**; configurable)
- **`EncryptToFileAsync`** / **`DecryptFromFileAsync`**

## **`ITwoKeyEncryptionService`** (envelope)

- **`Encrypt`** returns **`TwoKeyEncryptionResult`**: ciphertext + **encrypted DEK** + **`KeyId`** / **`KeyVersion`** (+ optional salt metadata)
- **`Decrypt`** takes ciphertext and **encrypted DEK** separately
- **`EncryptStreamAsync`** / **`DecryptToStreamAsync`** — combined stream layout: encrypted DEK first, then chunked ciphertext (see XML on **`TwoKeyEncryptionService`** for format notes)
- **`ReEncryptDek`** / **`ReEncryptDekAsync`** — rotate or migrate KEK without re-encrypting bulk data

## Thread safety

**`EncryptionServiceBase`** documents that **multiple threads may call the same instance concurrently**; each invocation uses its own cryptographic context. If **`IKeyStore`** (or other dependencies) are not thread-safe, synchronize or scope lifetimes accordingly.

## Dependency injection (this assembly)

Register **`Microsoft.Extensions.DependencyInjection.Abstractions`** (already referenced by this package on **netstandard2.0** and **net10.0**). Algorithm addons ( `Lyo.Encryption.AesCcm`, `.AesSiv`, `.XChaCha20Poly1305`) add their own `Add*Encryption` helpers; keys come from [`Lyo.Keystore`](../Lyo.Keystore/README.md).

## Registration overview

| Call | Registers |
| --------------------------------------------- | ----------------------------------------------------------------------------- |
| `AddLocalKeyStore(configure)` | `LocalKeyStore` + unkeyed `IKeyStore` |
| `AddKeyedLocalKeyStore(key, configure)` | Per-key `LocalKeyStore` + `IKeyStore` |
| `AddEncryptionServiceKeyed(...)` | Keyed DEK/KEK concretes, `IEncryptionService`, **`ITwoKeyEncryptionService`** |
| `AddAesCcmEncryption()` (addon) | Unkeyed **`AesCcmEncryptionService`** only |
| `AddDefaultEncryptionService<T>()` | Unkeyed **`IEncryptionService`** → `T` |
| `AddDefaultTwoKeyEncryptionService<T>()` | Unkeyed **`ITwoKeyEncryptionService`** → `T` (rare) |
| `AddRsaEncryption` / `AddAesGcmRsaEncryption` | Scoped RSA / hybrid services (paths or PFX) |

Unkeyed addon methods do **not** register `IEncryptionService` until you call `AddDefaultEncryptionService<TConcrete>()`. **File storage and envelope encryption** should use *
*keyed** registration (includes `ITwoKeyEncryptionService`).

## Keyed two-key (recommended)

`AddEncryptionServiceKeyed` overloads accept an existing keyed key-store name, or register the store via `Func<IServiceProvider, TKeyStore>`. Generic overloads support different DEK vs KEK types when both implement **`IEncryptionService`** and are built from **`IKeyStore`** (see source for the built-in type matrix).

## Configuration notes

- **Service options** (`EncryptionServiceOptions`: `MaxInputSize`, `FileExtension`, `AesGcmKeySize`, etc.) are set on concrete service constructors today — use algorithm parameters on `AddEncryptionServiceKeyed` (`aesGcmKeySize`) or construct services manually for advanced cases.
- **Secrets and key material** — use `AddLocalKeyStore` / `AddKeyedLocalKeyStore` with `configure => { ... }` and read **`IConfiguration`** inside that callback (same pattern as other Lyo apps). There is no `AddEncryptionServiceFromConfiguration` on this package; bind appsettings in the key-store configure delegate or in a custom `IKeyStore` factory.

## Options

**`EncryptionServiceOptions`** (per concrete service):

| Property | Typical use |
| ----------------------------------------- | ----------------------------------------------------------------------- |
| **`FileExtension`** | Suffix for encrypted artifacts (required non-empty on base ctor) |
| **`MinInputSize`** / **`MaxInputSize`** | Enforced on encrypt paths |
| **`CurrentFormatVersion`** | Stream/header version; defaults align with **`StreamFormatVersion.V1`** |
| **`AesGcmKeySize`** / **`AesSivKeySize`** | Algorithm-specific key material where applicable |

## Result and error types

- **`Lyo.Encryption.Models.EncryptionResult`** / **`DecryptionResult`** – **`Result<byte[]>`** with key metadata for APIs that avoid exceptions
- **`Lyo.Encryption.EncryptionErrorCodes`** – stable error-code constants paired with `EncryptionResult` / `DecryptionResult` (for example `KEY_NOT_FOUND`, `DECRYPTION_FAILED`, `INVALID_HEADER`); use these instead of string-matching exception messages
- **`DecryptionFailedException`**, **`EncryptionException`**, **`InvalidDataException`**, **`ArgumentOutsideRangeException`** – see **`IEncryptionService`** XML for which throws apply

## Helpers and validation

- **`Lyo.Encryption.TwoKey.TwoKeyDekValidation`** – validates `DekAlgorithm` + DEK key-material byte length for all supported symmetric algorithms; used on decrypt to reject mismatched envelopes before any cryptographic call.
- **`Lyo.Encryption.RsaKeyLoader`** – PEM/PFX RSA key loading helper. Uses BouncyCastle on `netstandard2.0` and `RSA.ImportFromPem` / `X509Certificate2` on `net10.0`. Invoked transitively by `RsaEncryptor` / `RsaDecryptor` / `AesGcmRsaEncryptionService` constructors but exposed for callers that want to share a loaded key across services.
- **`Lyo.Encryption.ISymmetricKeyMaterialSize`** – implemented by every symmetric `IEncryptionService` to advertise its accepted key-material sizes in bytes (e.g. AES-GCM = `{16, 24, 32}`, XChaCha20-Poly1305 = `{32}`). `TwoKeyDekValidation` and key-store validators rely on this.
- **`Encrypt` / `Decrypt` `ReadOnlySpan<byte>` overloads** on `IEncryptionService` — zero-copy entry points for callers that already hold a contiguous buffer; the legacy `byte[]` overloads remain.
- **`TwoKeyEncryptionResult`** fields — beyond the obvious ciphertext, the record carries `EncryptedDek`, `DekKeyMaterialBytes`, `KeyEncryptionKeySalt`, `KeyId`, `KeyVersion`, and `TotalSize`. The legacy `Lyo.Encryption.Models.TwoKeyEncryptionResult` is preserved for callers that still consume the result-builder shape.

## Helpers and validation — Streaming two-key

- `EncryptStreamAsync(Stream input, Stream output, ...)` / `DecryptStreamAsync(Stream input, Stream output, TwoKeyEncryptionResult metadata, ...)` — operates on an existing `TwoKeyEncryptionResult` (carries the wrapped DEK, key id/version, salt).
- `EncryptToStreamAsync(...)` / `DecryptToStreamAsync(...)` — writes the combined wire format (encrypted-DEK header + chunked ciphertext) to a single output stream and reads it back without external metadata.

## Upgrade checklist (short)

- Confirm **nonce / IV uniqueness** policy for each algorithm when integrating custom stores (see parent **`README.md`**).
- After dependency bumps (**BouncyCastle**, **Dorssel** AES extras), run **`Lyo.Encryption.Benchmarks`** in **Release** with algorithm-specific filters.
- Validate **FIPS / regional** requirements externally — this library follows general best practices but does not certify every jurisdiction.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Lyo.Keystore` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Lyo.Streams` — (direct, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (direct, third-party, netstandard2.0)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft, net10.0, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (direct, microsoft, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)