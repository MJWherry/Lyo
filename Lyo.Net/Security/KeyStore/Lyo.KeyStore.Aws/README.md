# Lyo.KeyStore.Aws

`AwsKeyStore` takes an `IAmazonSecretsManager` client and a secret-name prefix. It implements `Lyo.KeyStore.IKeyStore` and `Lyo.KeyStore.IKeyInventoryStore`, so admin UIs and key-rotation jobs can encrypt against it and list `keyId`s and versions.

The backing secret is stored as JSON (`{ "<keyId>": "plaintext-or-derived-material", ... }`) under a single secret per prefix. Logical version strings map onto AWS `VersionId` stages. Unresolved version requests fall through to `AWSCURRENT`. String values run through key derivation so callers receive cryptographic-length KEK bytes usable by `AesGcmEncryptionService` and the other symmetric services in `Lyo.Encryption`.

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
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)