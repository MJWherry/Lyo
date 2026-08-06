# Lyo.Encryption.XChaCha20Poly1305

XChaCha20-Poly1305 (24-byte nonce, 32-byte key) authenticated-encryption addon for `Lyo.Encryption`. Uses an HChaCha20 subkey derivation step then BouncyCastle's IETF ChaCha20-Poly1305 implementation for the inner AEAD.

Install this addon only when you actually use XChaCha20-Poly1305 — the core `Lyo.Encryption` package no longer pulls BouncyCastle on `net10`.

## Examples

### Unkeyed

```csharp
using Lyo.Encryption;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using Lyo.Encryption.XChaCha20Poly1305;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

services.AddLocalKeyStore(ks => ks.UpdateKeyFromString("k", "secret"));
services.AddXChaCha20Poly1305Encryption();
services.AddDefaultEncryptionService<XChaCha20Poly1305EncryptionService>();
```

### Keyed two-key

```csharp
services.AddKeyedLocalKeyStore("ks", ks => ks.UpdateKeyFromString("k", "secret"));
services.AddXChaCha20Poly1305EncryptionServiceKeyed("primary", "ks");
```

### Mixed DEK / KEK (core package)

```csharp
services.AddKeyedLocalKeyStore("primary", ks => { /* ... */ });
services.AddEncryptionServiceKeyed<XChaCha20Poly1305EncryptionService, AesGcmEncryptionService>("primary", "primary");
```

## Benchmarks

- [Benchmark summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

Requires **`IKeyStore`**. Use `configure =>` on key-store registration to bind secrets from **`IConfiguration`** ([`Lyo.Keystore`](../Lyo.Keystore/README.md)).

## Mixed DEK / KEK (core package)

See [`Lyo.Encryption`](../Lyo.Encryption/README.md) for RSA helpers and `AddDefaultTwoKeyEncryptionService<T>()`.

## Performance

BenchmarkDotNet on Intel Core Ultra 7 155U (.NET 10.0.9, June 2026): **2.54 ms encrypt / 2.34 ms decrypt @ 1 MB** (~3.8× AES-GCM; HChaCha20 + BouncyCastle). Benchmarks use explicit key material. Full tables: [`BENCHMARK_SUMMARY.md`](../Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Encryption` — (direct, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)