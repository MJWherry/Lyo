# Lyo.Encryption.AesCcm

AES-CCM authenticated encryption addon for `Lyo.Encryption`. Provides `AesCcmEncryptionService` (BouncyCastle-backed on all targets) and matching DI extensions.

Install this addon only when you actually use AES-CCM. The core `Lyo.Encryption` package no longer pulls BouncyCastle on `net10`.

## Examples

### Unkeyed (concrete + optional interface default)

```csharp
using Lyo.Encryption;
using Lyo.Encryption.AesCcm;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.KeyStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Key store — configure via lambda (read IConfiguration inside configure)
services.AddLocalKeyStore(ks =>
{
    ks.UpdateKeyFromString("default-key", "dev-secret");
});

// Algorithm — concrete only; optional AES key size parameter
services.AddAesCcmEncryption(AesGcmKeySizeBits.Bits256);
services.AddDefaultEncryptionService<AesCcmEncryptionService>();

// Example: load secret from configuration
services.AddLocalKeyStore(ks =>
{
    var secret = configuration["Encryption:KekSecret"]!;
    ks.UpdateKeyFromString("default-key", secret);
});
services.AddAesCcmEncryption();
services.AddDefaultEncryptionService<AesCcmEncryptionService>();
```

### Keyed two-key (recommended for file storage)

```csharp
const string keyName = "file-storage";
const string keyStoreName = "file-storage";

services.AddKeyedLocalKeyStore(keyStoreName, ks =>
    ks.UpdateKeyFromString("default-key", configuration["Encryption:KekSecret"]!));

services.AddAesCcmEncryptionServiceKeyed(keyName, keyStoreName, AesGcmKeySizeBits.Bits256);

// Inject: [FromKeyedServices("file-storage")] ITwoKeyEncryptionService
```

## Benchmarks

- [Benchmark summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

Requires a registered `IKeyStore` ([`Lyo.KeyStore`](../Lyo.KeyStore/README.md)).

## Keyed two-key (recommended for file storage)

Registers `AesCcmEncryptionService`, `IEncryptionService`, and `ITwoKeyEncryptionService` under the same key. See [`Lyo.Encryption`](../Lyo.Encryption/README.md) and [Encryption area `README.md`](../README.md) for mixed DEK/KEK algorithms and RSA registration.

## Performance

BenchmarkDotNet on Intel Core Ultra 7 155U (.NET 10.0.9, June 2026): **12.2 ms encrypt / 11.1 ms decrypt @ 1 MB** (~18× slower than AES-GCM). BouncyCastle-backed path. Tables: [`BENCHMARK_SUMMARY.md`](../Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Encryption` (direct, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (direct, third-party)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)