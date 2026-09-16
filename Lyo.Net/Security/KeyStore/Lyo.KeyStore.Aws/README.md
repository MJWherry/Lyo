# Lyo.KeyStore.Aws

`AwsKeyStore` takes an `IAmazonSecretsManager` client and a secret-name prefix. It implements `Lyo.KeyStore.IKeyStore` and `Lyo.KeyStore.IKeyInventoryStore`, so admin UIs and key-rotation jobs can encrypt against it and list `keyId`s and versions.

The backing secret is stored as JSON (`{ "<keyId>": "<material>", ... }`) under a single secret per prefix. Logical version strings map onto AWS `VersionId` values, including short labels such as `"1"` and `"2025-01"`. `AddKey` stores raw key bytes with the prefix `lyo-kek-raw:v1:`. `GetKey` returns those bytes unchanged. A secret value with no prefix still goes through derive-on-read, so secrets already in AWS keep their meaning. `AddKeyFromString` derives once, the same way `LocalKeyStore` does, then stores the derived bytes as raw. `GetCurrentVersion` only reads. `AddKey` sets the logical version as current when none is set.

## AwsKeyStore methods

- `IKeyStore.` `GetKey`, `GetCurrentKey`, `GetCurrentVersion`, `AddKey`, `UpdateKey`, `HasKey`, metadata, salt-for-version. Sync and async variants.
- `IKeyInventoryStore.` `GetAvailableKeyIdsAsync(CancellationToken)`, `GetAvailableVersionsAsync(string keyId, CancellationToken)` so listings, rotation reports, and DEK migrations can pivot off the live store rather than a local index.

## AwsKeyStoreOptions

| Property | Default | Notes |
| --------------------------------- | ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SectionName` | `AwsKeyStore` | Default appsettings subsection. |
| `AccessKeyId` / `SecretAccessKey` | unset | Static credentials. When both are set they win over `Profile`. Omit to use `Profile` or the AWS default credential chain (IAM role, env vars, `default` profile). |
| `Profile` | unset | Named profile from `~/.aws/credentials` / `~/.aws/config`. Used when static keys are omitted. If set but missing, registration fails rather than falling back to `default`. |
| `Region` | `us-east-2` (when unspecified) | Resolved via `RegionEndpoint.GetBySystemName`. |
| `SecretNamePrefix` | `lyo/kek` fallback | Logical secret prefix used to scope keys across environments (`dev/MyApp/KeyStore`, `prod/MyApp/KeyStore`, …). |

## Dependency injection

| Extension | Purpose |
| -------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `services.AddAwsKeyStore(Func<IServiceProvider,string> resolvePrefix)` | Resolve the prefix from DI (multi-tenant hosts), register `AwsKeyStore` as a singleton. |
| `services.AddAwsKeyStoreFromConfiguration(IConfiguration, configSectionName = "AwsKeyStore")` | Bind `AwsKeyStoreOptions` + register `IAmazonSecretsManager` (when missing) + register `AwsKeyStore` and `IKeyStore`. |
| `services.AddAmazonSecretsManagerFromConfiguration(IConfiguration, configSectionName = "AwsKeyStore")` | Standalone `IAmazonSecretsManager` registration (no keystore). Honours static keys, then `Profile`, then the default credential chain; also honours region. |
| `services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix)` / `<TKeyStore>` | Register a keyed `ITwoKeyEncryptionService` stack using `AwsKeyStore` plus paired `AesGcmEncryptionService` for both DEK and KEK. |
| `services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix, AwsKeyStoreOptions?)` | Same, but with explicit AWS options (region/credentials) rather than configuration binding. |
| `services.AddTwoKeyEncryptionFromConfiguration(IConfiguration, keyedServiceName, configSectionName)` / `<TKeyStore>` | Bind `AwsKeyStoreOptions` from configuration and wire the keyed two-key stack in one call. |

`AddAwsKeyStore(Func<...>)` registers `AwsKeyStore` as a concrete singleton only, no `IKeyStore` indirection. `AddAwsKeyStoreFromConfiguration` and the two-key extensions also register `IKeyStore` so `Lyo.Encryption` can resolve it.

## Raw key bytes and short versions

`AwsKeyStore.RawKeyPrefix` is `lyo-kek-raw:v1:`. `AddKey` writes that prefix plus base64. `GetKey` strips it and returns the bytes, so a key copied from `LocalKeyStore` decrypts the same ciphertext. Values without the prefix still run through PBKDF2 on read.

Any non-empty logical version is resolved through the version map first. Only when that map has no entry, and the string is at least 32 characters, is it tried as an AWS `VersionId`. Short current versions are stored and returned as written. `GetCurrentVersion` does not write. One prefix is one Secrets Manager secret, and every `AddKey` stores a new version of that whole JSON document, so a large copy can hit AWS version retention. `KeyStoreMigrator` surfaces that exception and does not hide it.

## See also

- [`Lyo.KeyStore`](../Lyo.KeyStore/README.md). Interfaces and local store.
- [`Lyo.Encryption`](../Lyo.Encryption/README.md). Encryption services that consume `IKeyStore`.
- [`../README.md`](../README.md). Encryption guide: algorithms, stream formats, threat model.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Encryption` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `AWSSDK.SecretsManager` `4.0.100.3` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)