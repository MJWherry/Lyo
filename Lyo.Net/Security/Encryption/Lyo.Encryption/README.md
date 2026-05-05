# Lyo.Encryption

Production-oriented **authenticated encryption** for .NET: symmetric AEAD (**AES-GCM**, **ChaCha20-Poly1305**, **XChaCha20-Poly1305**, **AES-CCM**, **AES-SIV**), **RSA** and **AES-GCM +
RSA** hybrids, and **envelope / two-key** flows (**`ITwoKeyEncryptionService`**) composed from **`IEncryptionService`** implementations. Keys can be supplied inline or resolved from
**`Lyo.Keystore`** via **`keyId`**.

The primary contracts are **`IEncryptionService`** (single key per operation), **`ITwoKeyEncryptionService`** (per-operation DEK wrapped by a KEK), and **`EncryptionServiceBase`**
(shared streaming, string, and file helpers). With XML doc generation enabled in the repo, IntelliSense surfaces the same summaries as this README for documented members.

For architecture, threat model, **`IKeyStore`** expectations, benchmarks, and operational checklists, see **[Security/Encryption `README.md`](../README.md)** — treat that document as
the umbrella guide; this file focuses on **this assembly’s API surface**.

## Features

- **AEAD** – Confidentiality + integrity (authenticated tags); tampering surfaces as **`DecryptionFailedException`**
- **Key sources** – Optional **`byte[] key`** / **`byte[] kek`** or **`IKeyStore`** lookup by **`keyId`** (and version for two-key decrypt / rotation)
- **Streaming** – **`EncryptToStreamAsync`** / **`DecryptToStreamAsync`** chunk large payloads without materializing the whole ciphertext in memory (framed format on the wire)
- **Files** – **`EncryptToFileAsync`**, **`DecryptFromFileAsync`**, and stream-to-file variants
- **Strings** – **`EncryptString`** / **`DecryptString`** using **`DefaultEncoding`** (UTF-8 by default on **`EncryptionServiceBase`**)
- **DI helpers** – **`EncryptionServiceExtensions`**: RSA / AES-GCM+RSA registration, keyed **`ITwoKeyEncryptionService`** + keyed **`IKeyStore`**
- **Discovery** – **`EncryptionAlgorithm`**, **`EncryptionAlgorithmDiscovery`**, algorithm metadata on **`EncryptionServiceBase.AlgorithmKind`**
- **Non-throwing workflows** – **`EncryptionResult`** / **`DecryptionResult`** in **`Lyo.Encryption.Models`** ([`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md))
- **Utilities** – **`SecurityUtilities`** (buffer zeroing, constant-time compare) — **not** KDFs (see [`Lyo.Keystore`](../Lyo.Keystore/README.md))

## Service matrix

| Type | Role |
|------|------|
| **`AesGcmEncryptionService`** | AES-GCM; key size via **`AesGcmKeySizeBits`** |
| **`ChaCha20Poly1305EncryptionService`** | ChaCha20-Poly1305 (IETF nonce) |
| **`XChaCha20Poly1305EncryptionService`** | XChaCha20-Poly1305 (extended nonce) |
| **`AesCcmEncryptionService`** | AES-CCM |
| **`AesSivEncryptionService`** | AES-SIV (misuse-resistant synthetic IV) |
| **`RsaEncryptionService`** | RSA encrypt/decrypt (chunked for large plaintext) |
| **`AesGcmRsaEncryptionService`** | Hybrid: RSA wraps AES key, AES-GCM protects payload |
| **`TwoKeyEncryptionService<TKek, TDek>`** | Envelope: random DEK per operation, KEK encrypts DEK |

Concrete types live under **`AesGcm/`**, **`ChaCha20Poly1305/`**, **`Symmetric/Aes/*`**, **`Symmetric/ChaCha/*`**, **`Rsa/`**, **`AesGcmRsa/`**, and **`TwoKey/`**.

## **`IEncryptionService`** (single-key path)

- **`Encrypt`** / **`Decrypt`** on **`byte[]`**, **`ReadOnlySpan<byte>`**, or slice overloads
- **`EncryptString`** / **`DecryptString`**
- **`EncryptToStreamAsync`** / **`DecryptToStreamAsync`** — output begins with a small header (format version, algorithm id, reserved bytes) followed by length-prefixed encrypted
  chunks (default plaintext chunk size **1 MiB**; configurable)
- **`EncryptToFileAsync`** / **`DecryptFromFileAsync`**

Pass **`keyId`** to use **`IKeyStore`**; pass **`key`** (or **`kek`** on two-key) for explicit material. If both are omitted and the implementation requires a store, operations throw
**`InvalidOperationException`**.

## **`ITwoKeyEncryptionService`** (envelope)

- **`Encrypt`** returns **`TwoKeyEncryptionResult`**: ciphertext + **encrypted DEK** + **`KeyId`** / **`KeyVersion`** (+ optional salt metadata)
- **`Decrypt`** takes ciphertext and **encrypted DEK** separately
- **`EncryptStreamAsync`** / **`DecryptToStreamAsync`** — combined stream layout: encrypted DEK first, then chunked ciphertext (see XML on **`TwoKeyEncryptionService`** for format
  notes)
- **`ReEncryptDek`** / **`ReEncryptDekAsync`** — rotate or migrate KEK without re-encrypting bulk data

## Thread safety

**`EncryptionServiceBase`** documents that **multiple threads may call the same instance concurrently**; each invocation uses its own cryptographic context. If **`IKeyStore`** (or
other dependencies) are not thread-safe, synchronize or scope lifetimes accordingly.

## Dependency injection (this assembly)

Register **`Microsoft.Extensions.DependencyInjection.Abstractions`** (already referenced by this package on **netstandard2.0** and **net10.0**).

```csharp
using Lyo.Encryption.Extensions;
using Microsoft.Extensions.DependencyInjection;

// RSA or hybrid (paths / PFX as appropriate for your deployment)
services.AddRsaEncryption(publicPemPath: "keys/public.pem", privatePemPath: "keys/private.pem");
services.AddAesGcmRsaEncryption(publicPemPath: "keys/public.pem", privatePemPath: "keys/private.pem");

// Keyed two-key stack: registers keyed IKeyStore, keyed DEK/KEK IEncryptionService, and keyed ITwoKeyEncryptionService
services.AddEncryptionServiceKeyed<MyKeyStore>(
    keyName: "tenant-1",
    sp => new MyKeyStore(sp, /* ... */),
    aesGcmKeySize: AesGcmKeySizeBits.Bits256);

// Resolve
var twoKey = serviceProvider.GetRequiredKeyedService<ITwoKeyEncryptionService>("tenant-1");
```

**`AddEncryptionServiceKeyed`** has overloads that accept an existing keyed key-store name or inline key-store factory; generic overloads support different DEK vs KEK service types
when both derive from **`IEncryptionService`** and are constructible from **`IKeyStore`** (see source for supported type matrix).

**`EncryptionServiceExtensions.DetermineAlgorithm`**, **`DetermineDekAlgorithm`**, and **`DetermineKekAlgorithm`** introspect live instances for logging or diagnostics.

## Options

**`EncryptionServiceOptions`** (per concrete service):

| Property | Typical use |
|----------|-------------|
| **`FileExtension`** | Suffix for encrypted artifacts (required non-empty on base ctor) |
| **`MinInputSize`** / **`MaxInputSize`** | Enforced on encrypt paths |
| **`CurrentFormatVersion`** | Stream/header version; defaults align with **`StreamFormatVersion.V1`** |
| **`AesGcmKeySize`** / **`AesSivKeySize`** | Algorithm-specific key material where applicable |

## Result and error types

- **`Lyo.Encryption.Models.EncryptionResult`** / **`DecryptionResult`** – **`Result<byte[]>`** with key metadata for APIs that avoid exceptions
- **`DecryptionFailedException`**, **`EncryptionException`**, **`InvalidDataException`**, **`ArgumentOutsideRangeException`** – see **`IEncryptionService`** XML for which throws apply

## Upgrade checklist (short)

1. Confirm **nonce / IV uniqueness** policy for each algorithm when integrating custom stores (see parent **`README.md`**).
2. After dependency bumps (**BouncyCastle**, **Dorssel** AES extras), run **`Lyo.Encryption.Benchmarks`** in **Release** with algorithm-specific filters.
3. Validate **FIPS / regional** requirements externally — this library follows general best practices but does not certify every jurisdiction.


## Dependencies

*(Synchronized from `Lyo.Encryption.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                      | Version  | Notes |
|----------------------------------------------|----------|--------|
| `BouncyCastle.Cryptography`                  | `2.6.2`  | |
| `Dorssel.Security.Cryptography.AesExtra`     | `2.0.0`  | |
| `Microsoft.Bcl.AsyncInterfaces`            | `10.0.0` | *netstandard2.0 only* |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` | *netstandard2.0 and net10.0* |
| `System.Threading.Tasks.Extensions`        | `4.6.3`  | *netstandard2.0 only* |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
- [`Lyo.Streams`](../../../Core/Streams/Lyo.Streams/README.md)
- [`Lyo.Hashing`](../../Hashing/Lyo.Hashing/README.md)
- [`Lyo.Keystore`](../Lyo.Keystore/README.md)