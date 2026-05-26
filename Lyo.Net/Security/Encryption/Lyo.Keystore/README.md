# Lyo.Keystore

**Key Encryption Key (KEK)** storage and rotation contracts for [`Lyo.Encryption`](../Lyo.Encryption/README.md). Encryption services call into **`IKeyStore`** by **`keyId`** (and
optional **version** string) so ciphertext can outlive a single key material rotation.

**Vocabulary:** the **KEK** lives in the store. **Data Encryption Keys (DEKs)** used by envelope / two-key flows are generated per operation by the encryption layer and are **not**
persisted in the keystore—only the KEK that wraps them.

## `IKeyStore` at a glance

Versions are **strings** (for example **`"1"`**, **`"2025-01"`**, or opaque ids from an HSM). **`GetCurrentVersion`** returns the version used when callers omit an explicit version
on encrypt.

| Concern                  | Members                                                                                                                                                                                                                                       |
|--------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Read material**        | **`GetKey`**, **`GetKeyAsync`**, **`GetCurrentKey`**, **`GetCurrentKeyAsync`**                                                                                                                                                                |
| **Read version pointer** | **`GetCurrentVersion`**, **`GetCurrentVersionAsync`**                                                                                                                                                                                         |
| **Write / rotate**       | **`AddKey`**, **`AddKeyAsync`**, **`AddKeyFromString`**, **`AddKeyFromStringAsync`**, **`SetCurrentVersion`**, **`SetCurrentVersionAsync`**, **`UpdateKey`**, **`UpdateKeyAsync`**, **`UpdateKeyFromString`**, **`UpdateKeyFromStringAsync`** |
| **Existence**            | **`HasKey`**, **`HasKeyAsync`**                                                                                                                                                                                                               |
| **Metadata**             | **`GetKeyMetadata`**, **`SetKeyMetadata`**, async variants; **`GetSaltForVersion`** when derivation salts are tracked per version                                                                                                             |

**`UpdateKey*`** allocates a **new** version (monotonic in **`LocalKeyStore`**) and sets it current—use this for rotation workflows. **`AddKey`** pins an exact **`(keyId, version)`
** pair—use when importing known version labels from another system.

## Exceptions

Failures surface as **`KeyNotFoundException`**, **`InvalidKeyException`**, **`KeyVersionNotFoundException`**, all rooted at **`EncryptionKeyException`**. Log with **`keyId`** (
never log raw key bytes); pair with metrics so silent misconfiguration does not masquerade as “bad client data.”

## Key derivation (`KeyDerivation/`)

HKDF (RFC 5869), PBKDF2-SHA256 helpers, and Argon2 adapters live in this assembly so onboarding UIs can derive stable bytes from passphrases consistently with **`AddKeyFromString`
**. Prefer **`SecureKeyGenerator`** when generating random material instead of ad-hoc RNG.

Implementations of **`IKeyDerivationService`**:

| Service                          | Notes                                                                                                                      |
|----------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| **`Pbkdf2KeyDerivationService`** | PBKDF2 (SHA-256 by default); iteration count and output length configurable.                                               |
| **`HkdfKeyDerivationService`**   | HKDF-Extract + HKDF-Expand (SHA-256 by default); ideal for deriving sub-keys from existing key material.                   |
| **`Argon2KeyDerivationService`** | Argon2id with configurable memory / parallelism / time-cost via constructor parameters (BouncyCastle on `netstandard2.0`). |

## Key validation (`KeyValidator`)

**`Lyo.Keystore.KeyValidator`** centralizes the size and basic-strength checks used by encryption services and key stores:

- `ValidateKeyOrThrow(byte[] keyMaterial, ISymmetricKeyMaterialSize spec)` — rejects null/empty buffers and key lengths that aren't in the algorithm's accepted set.
- `IsValid(...)` / `TryValidate(...)` — non-throwing variants for UIs that report validation failures inline.
- Optional entropy/heuristic checks (e.g. all-zero buffers, repeating patterns) so that obviously bad imports fail early.

## Inventory (`IKeyInventoryStore`)

Optional capability for admin UIs and audits: enumerate logical **`keyId`**s and versions. Not every production store implements full listing—probe for **`IKeyInventoryStore`** (or
your cloud-specific API) before assuming discovery works.

## Local development (`LocalKeyStore`)

In-memory store for tests and local apps:

```csharp
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

services.AddLocalKeyStore(ks =>
{
    ks.AddKeyFromString("app", "v1", "local-dev-secret");
    ks.SetCurrentVersion("app", "v1");
});
```

**`AddKeyedLocalKeyStore`** registers **distinct** **`LocalKeyStore`** instances per DI key—useful when a single process hosts multiple logical tenants **if** you are careful about
keyed resolution and never cross-wire **`IKeyStore`** instances.

**`LocalKeyStore.RemoveKey(string keyId, string version)`** retires a non-current version (returns `false` when the version doesn't exist or matches `GetCurrentVersion`). Combine
with `SetCurrentVersion` before pruning the previous current.

**Production:** **`LocalKeyStore`** is not durable and not audited—swap for **`Lyo.Keystore.Aws`**, Azure Key Vault, PKCS#11, or another **`IKeyStore`** that meets your retention
and access policies.

## Cloud bridge

See **[`Lyo.Keystore.Aws`](../Lyo.Keystore.Aws/README.md)** for **`AwsKeyStore`** and helpers that align with AWS Secrets Manager style payloads.

## How encryption uses the store

Symmetric and envelope services resolve **`keyId`** on encrypt; ciphertext and stream headers carry **`keyId`** and **version** so decrypt can call **`GetKey(keyId, version)`**
even after rotation. Two-key flows additionally wrap per-operation DEKs—rotation of the KEK can use **`ReEncryptDek`** patterns documented on **`ITwoKeyEncryptionService`** without
re-encrypting bulk payload (see encryption README).

## Operational checklist

1. **Thread safety** — custom stores must tolerate concurrent **`Get*`** while admins **`Add*`** / **`SetCurrentVersion`**.
2. **Rotation** — keep old versions until all ciphertext referencing them is re-encrypted or retired; track **`GetCurrentVersion`** separately from “newest encrypt version.”
3. **Backups** — database + blob snapshots do not replace key governance; export and access control live in infra policy.
4. **Configuration** — prefer environment-specific **`keyId`** namespaces (`tenant:prod:comic-files`) to avoid accidental cross-environment decrypt.

## Dependency injection

**`Microsoft.Extensions.DependencyInjection.Extensions`** (this package):

- **`AddLocalKeyStore()`** — registers a shared **`LocalKeyStore`** as **`IKeyStore`**.
- **`AddLocalKeyStore(Action<LocalKeyStore>)`** — configure keys before the container finishes building.

Keyed encryption registration patterns live in **[`EncryptionServiceExtensions`](../Lyo.Encryption/Extensions/EncryptionServiceExtensions.cs)** (for example *
*`AddEncryptionServiceKeyed`**); see also **[`Lyo.Encryption/README.md`](../Lyo.Encryption/README.md)**.

## Umbrella documentation

Algorithm choice, stream formats, threat modeling, and long-form examples remain in **[`../README.md`](../README.md)** (folder-level encryption guide).

## Related projects

- [`Lyo.Encryption`](../Lyo.Encryption/README.md)
- [`Lyo.Keystore.Aws`](../Lyo.Keystore.Aws/README.md)
