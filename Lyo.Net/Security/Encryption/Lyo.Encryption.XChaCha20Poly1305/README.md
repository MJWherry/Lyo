# Lyo.Encryption.XChaCha20Poly1305

XChaCha20-Poly1305 (24-byte nonce, 32-byte key) authenticated-encryption addon for `Lyo.Encryption`. Uses an HChaCha20 subkey derivation step then BouncyCastle's IETF
ChaCha20-Poly1305 implementation for the inner AEAD.

Install this addon only when you actually use XChaCha20-Poly1305 — the core `Lyo.Encryption` package no longer pulls BouncyCastle on `net10`.

## Dependency injection

Requires **`IKeyStore`**. Use `configure =>` on key-store registration to bind secrets from **`IConfiguration`** ([`Lyo.Keystore`](../../Lyo.Keystore/README.md)).

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

See [`Lyo.Encryption`](../Lyo.Encryption/README.md) for RSA helpers and `AddDefaultTwoKeyEncryptionService<T>()`.
