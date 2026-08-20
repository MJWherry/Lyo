# Lyo.KeyStore

Key encryption key (KEK) storage and rotation contracts for [`Lyo.Encryption`](../Lyo.Encryption/README.md). Encryption services call `IKeyStore` by `keyId` and optional version string so ciphertext can outlive a single key-material rotation.

Vocabulary: the KEK lives in the store. Data encryption keys (DEKs) used by envelope / two-key flows are generated per operation by the encryption layer and are not persisted in the keystore. Only the KEK that wraps them is stored.

## Examples

### Register with DI

```csharp
using Lyo.KeyStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Unkeyed
services.AddLocalKeyStore(ks =>
{
    ks.UpdateKeyFromString("app", configuration["Encryption:CurrentKek"]!);
});

// Keyed (pair with AddEncryptionServiceKeyed / addon *ServiceKeyed)
const string storeKey = "primary";
services.AddKeyedLocalKeyStore(storeKey, ks =>
{
    ks.AddKeyFromString("app", "v1", configuration["Encryption:Kek:v1"]!);
    ks.SetCurrentVersion("app", "v1");
});
```

### Register with DI (2)

```json
{
  "Encryption": {
    "CurrentKek": "replace-in-user-secrets",
    "Kek": {
      "v1": "versioned-secret"
    }
  }
}
```

## IKeyStore methods

Versions are strings, for example `"1"`, `"2025-01"`, or opaque ids from an HSM. `GetCurrentVersion` returns the version used when callers omit an explicit version on encrypt.

| Concern | Members |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Read material | `GetKey`, `GetKeyAsync`, `GetCurrentKey`, `GetCurrentKeyAsync` |
| Read version pointer | `GetCurrentVersion`, `GetCurrentVersionAsync` |
| Write / rotate | `AddKey`, `AddKeyAsync`, `AddKeyFromString`, `AddKeyFromStringAsync`, `SetCurrentVersion`, `SetCurrentVersionAsync`, `UpdateKey`, `UpdateKeyAsync`, `UpdateKeyFromString`, `UpdateKeyFromStringAsync` |
| Existence | `HasKey`, `HasKeyAsync` |
| Metadata | `GetKeyMetadata`, `SetKeyMetadata`, async variants; `GetSaltForVersion` when derivation salts are tracked per version |

`UpdateKey*` allocates a new version, monotonic in `LocalKeyStore`, and sets it current. Use this for rotation. `AddKey` pins an exact `(keyId, version)` pair. Use that when importing known version labels from another system.

## Exceptions

Failures surface as `KeyNotFoundException`, `InvalidKeyException`, `KeyVersionNotFoundException`, all rooted at `EncryptionKeyException`. Log with `keyId`. Never log raw key bytes. Pair with metrics so silent misconfiguration does not look like bad client data.

## Key derivation (`KeyDerivation/`)

HKDF (RFC 5869), PBKDF2-SHA256 helpers, and Argon2 adapters live in this assembly so onboarding UIs can derive stable bytes from passphrases consistently with `AddKeyFromString`. Prefer `SecureKeyGenerator` when generating random material instead of ad-hoc RNG.

Implementations of `IKeyDerivationService`:

| Service | Notes |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `Pbkdf2KeyDerivationService` | PBKDF2 (SHA-256 by default). Iteration count and output length are configurable. |
| `HkdfKeyDerivationService` | HKDF-Extract + HKDF-Expand (SHA-256 by default). Derives sub-keys from existing key material. |
| `Argon2KeyDerivationService` | Argon2id with configurable memory / parallelism / time-cost via constructor parameters (BouncyCastle on `netstandard2.0`). |

## Key validation (`KeyValidator`)

- `ValidateKeyOrThrow(byte[] keyMaterial, ISymmetricKeyMaterialSize spec)`. Rejects null/empty buffers and key lengths that aren't in the algorithm's accepted set.
- `IsValid(...)` / `TryValidate(...)`. Non-throwing variants for UIs that report validation failures inline.
- Optional entropy/heuristic checks (e.g. all-zero buffers, repeating patterns) so that obviously bad imports fail early.

## Inventory (`IKeyInventoryStore`)

Optional capability for admin UIs and audits: enumerate logical `keyId`s and versions. Not every store implements listing. Probe for `IKeyInventoryStore` or your cloud-specific API before assuming discovery works.

## Dependency injection

| Extension | Registers |
| -------------------------------------------------------------------- | ------------------------------------------------ |
| `AddLocalKeyStore()` | `LocalKeyStore` + unkeyed `IKeyStore` |
| `AddLocalKeyStore(Action<LocalKeyStore> configure)` | Configured `LocalKeyStore` + unkeyed `IKeyStore` |
| `AddKeyedLocalKeyStore(string key, Action<LocalKeyStore> configure)` | Per-key `LocalKeyStore` + `IKeyStore` |

Configuration uses the `configure =>` lambda. There is no `AddLocalKeyStoreFromConfiguration`. Read `IConfiguration` inside `configure`, the same pattern as other Lyo libraries:

Example `appsettings.json` values consumed manually in `configure`:

## Local development (`LocalKeyStore`)

In-memory store for tests and local apps:

```csharp
services.AddLocalKeyStore(ks =>
{
    ks.AddKeyFromString("app", "v1", "local-dev-secret");
    ks.SetCurrentVersion("app", "v1");
});
```

`AddKeyedLocalKeyStore` registers distinct `LocalKeyStore` instances per DI key. Useful when a single process hosts multiple logical tenants, if you are careful about keyed resolution and never cross-wire `IKeyStore` instances.

`LocalKeyStore.RemoveKey(string keyId, string version)` retires a non-current version and returns `false` when the version doesn't exist or matches `GetCurrentVersion`. Call `SetCurrentVersion` before pruning the previous current.

**Production.** `LocalKeyStore` is not durable and not audited. Swap for `Lyo.KeyStore.Aws`, Azure Key Vault, PKCS#11, or another `IKeyStore` that meets your retention and access policies.

## Cloud bridge

See [`Lyo.KeyStore.Aws`](../Lyo.KeyStore.Aws/README.md) for `AwsKeyStore` and helpers that align with AWS Secrets Manager style payloads.

## How encryption uses the store

Symmetric and envelope services resolve `keyId` on encrypt. Ciphertext and stream headers carry `keyId` and version so decrypt can call `GetKey(keyId, version)` even after rotation. Two-key flows wrap per-operation DEKs. KEK rotation can use `ReEncryptDek` on `ITwoKeyEncryptionService` without re-encrypting bulk payload. See the encryption README.

## Operational checklist

- **Thread safety.** Custom stores must tolerate concurrent `Get*` while admins `Add*` / `SetCurrentVersion`.
- **Rotation.** Keep old versions until all ciphertext referencing them is re-encrypted or retired. Track `GetCurrentVersion` separately from the newest encrypt version.
- **Backups.** Database and blob snapshots do not replace key governance. Export and access control live in infra policy.
- **Configuration.** Prefer environment-specific `keyId` namespaces (`tenant:prod:comic-files`) to avoid accidental cross-environment decrypt.

## Dependency injection

`Microsoft.Extensions.DependencyInjection.Extensions` in this package: `AddLocalKeyStore()` registers a shared `LocalKeyStore` as `IKeyStore`. `AddLocalKeyStore(Action<LocalKeyStore>)` configures keys before the container finishes building. Keyed encryption registration lives in [`EncryptionServiceExtensions`](../Lyo.Encryption/Extensions/EncryptionServiceExtensions.cs), for example `AddEncryptionServiceKeyed`. See also [`Lyo.Encryption/README.md`](../Lyo.Encryption/README.md).

## Encryption guide

Algorithm choice, stream formats, threat modeling, and long-form examples remain in [`../README.md`](../README.md), the folder-level encryption guide.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)