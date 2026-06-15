# Lyo.Encryption.AesCcm

AES-CCM authenticated encryption addon for `Lyo.Encryption`. Provides `AesCcmEncryptionService` (BouncyCastle-backed on all targets) and matching DI extensions.

Install this addon only when you actually use AES-CCM — the core `Lyo.Encryption` package no longer pulls BouncyCastle on `net10`.

## Dependency injection

Requires a registered **`IKeyStore`** ([`Lyo.Keystore`](../Lyo.Keystore/README.md)).

### Unkeyed (concrete + optional interface default)

```csharp
using Lyo.Encryption;
using Lyo.Encryption.AesCcm;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Keystore;
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

Registers **`AesCcmEncryptionService`**, **`IEncryptionService`**, and **`ITwoKeyEncryptionService`** under the same key.

```csharp
const string keyName = "file-storage";
const string keyStoreName = "file-storage";

services.AddKeyedLocalKeyStore(keyStoreName, ks =>
    ks.UpdateKeyFromString("default-key", configuration["Encryption:KekSecret"]!));

services.AddAesCcmEncryptionServiceKeyed(keyName, keyStoreName, AesGcmKeySizeBits.Bits256);

// Inject: [FromKeyedServices("file-storage")] ITwoKeyEncryptionService
```

See [`Lyo.Encryption`](../Lyo.Encryption/README.md) and [Encryption area `README.md`](../README.md) for mixed DEK/KEK algorithms and RSA registration.

## Performance

BenchmarkDotNet on Intel Core Ultra 7 155U (.NET 10.0.9, June 2026): **12.2 ms encrypt / 11.1 ms decrypt @ 1 MB** (~18× slower than AES-GCM). BouncyCastle-backed path. Full tables: [`BENCHMARK_SUMMARY.md`](../Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md).
