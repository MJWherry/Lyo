# Lyo.Encryption.AesSiv

AES-SIV (RFC 5297) deterministic authenticated encryption addon for `Lyo.Encryption`. Provides `AesSivEncryptionService` backed by `Dorssel.Security.Cryptography.AesExtra` and matching DI extensions.

Install this addon only when you actually use AES-SIV — the core `Lyo.Encryption` package no longer pulls `Dorssel.Security.Cryptography.AesExtra`.

## Examples

### Unkeyed

```csharp
using Lyo.Encryption;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

services.AddLocalKeyStore(ks => ks.UpdateKeyFromString("k", "secret"));
services.AddAesSivEncryption(AesSivKeySizeBits.Bits256); // or Bits384 / Bits512
services.AddDefaultEncryptionService<AesSivEncryptionService>();
```

### Keyed two-key

```csharp
services.AddKeyedLocalKeyStore("ks", ks => ks.UpdateKeyFromString("k", "secret"));
services.AddAesSivEncryptionServiceKeyed("primary", "ks", AesSivKeySizeBits.Bits384);
```

## Benchmarks

- [Benchmark summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

Requires **`IKeyStore`**. Configure keys with `AddLocalKeyStore` / `AddKeyedLocalKeyStore` and read **`IConfiguration`** inside the `configure` callback (see [ `Lyo.Keystore`](../Lyo.Keystore/README.md)).

## Keyed two-key

See [`Lyo.Encryption`](../Lyo.Encryption/README.md) for the full registration table and core `AddEncryptionServiceKeyed` overloads.

## Performance

BenchmarkDotNet on Intel Core Ultra 7 155U (.NET 10.0.9, June 2026): **17.0 ms encrypt / 16.4 ms decrypt @ 1 MB** (~25× slower than AES-GCM). Deterministic SIV mode; choose for nonce-misuse resistance, not peak throughput. Full tables: [`BENCHMARK_SUMMARY.md`](../Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Encryption` — (direct, lyo)
- `Dorssel.Security.Cryptography.AesExtra` `2.0.0` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)