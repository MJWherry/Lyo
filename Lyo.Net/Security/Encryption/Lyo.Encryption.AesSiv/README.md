# Lyo.Encryption.AesSiv

AES-SIV (RFC 5297) deterministic authenticated encryption addon for `Lyo.Encryption`. Provides `AesSivEncryptionService` backed by `Dorssel.Security.Cryptography.AesExtra` and
matching DI extensions.

Install this addon only when you actually use AES-SIV — the core `Lyo.Encryption` package no longer pulls `Dorssel.Security.Cryptography.AesExtra`.

## Dependency injection

Requires **`IKeyStore`**. Configure keys with `AddLocalKeyStore` / `AddKeyedLocalKeyStore` and read **`IConfiguration`** inside the `configure` callback (see [
`Lyo.Keystore`](../Lyo.Keystore/README.md)).

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

See [`Lyo.Encryption`](../Lyo.Encryption/README.md) for the full registration table and core `AddEncryptionServiceKeyed` overloads.

## Performance

BenchmarkDotNet on Intel Core Ultra 7 155U (.NET 10.0.9, June 2026): **17.0 ms encrypt / 16.4 ms decrypt @ 1 MB** (~25× slower than AES-GCM). Deterministic SIV mode; choose for
nonce-misuse resistance, not peak throughput. Full tables: [`BENCHMARK_SUMMARY.md`](../Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md).
