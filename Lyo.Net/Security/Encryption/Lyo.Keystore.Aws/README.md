# Lyo.Keystore.Aws

**`AwsKeyStore`** (an `IAmazonSecretsManager` client + secret-name prefix) implements both **`Lyo.Keystore.IKeyStore`** and **`Lyo.Keystore.IKeyInventoryStore`**, so admin UIs and
key-rotation jobs can both encrypt against it and enumerate available `keyId`s / versions.

The backing secret is stored as JSON (`{ "<keyId>": "plaintext-or-derived-material", ... }`) under a single secret per prefix. Logical version strings map onto AWS `VersionId`
stages; unresolved version requests fall through to `AWSCURRENT`. String values run through key derivation so callers receive cryptographic-length KEK bytes usable by
`AesGcmEncryptionService` and the other symmetric services in `Lyo.Encryption`.

## `AwsKeyStore` API

- **`IKeyStore`** — `GetKey`, `GetCurrentKey`, `GetCurrentVersion`, `AddKey`, `UpdateKey`, `HasKey`, metadata, salt-for-version (all sync + async variants).
- **`IKeyInventoryStore`** — `GetAvailableKeyIdsAsync(CancellationToken)`, `GetAvailableVersionsAsync(string keyId, CancellationToken)` so listings, rotation reports, and DEK
  migrations can pivot off the live store rather than a local index.

## Options — `AwsKeystoreOptions`

| Property                          | Default                        | Notes                                                                                                          |
|-----------------------------------|--------------------------------|----------------------------------------------------------------------------------------------------------------|
| `SectionName`                     | `AwsKeyStore`                  | Default appsettings subsection.                                                                                |
| `AccessKeyId` / `SecretAccessKey` | unset                          | Static credentials. Omit to fall through to the AWS default credential chain (IAM role, env vars, profile).    |
| `Region`                          | `us-east-2` (when unspecified) | Resolved via `RegionEndpoint.GetBySystemName`.                                                                 |
| `SecretNamePrefix`                | `lyo/kek` fallback             | Logical secret prefix used to scope keys across environments (`dev/MyApp/KeyStore`, `prod/MyApp/KeyStore`, …). |

## Dependency injection

| Extension                                                                                                            | Purpose                                                                                                                                                              |
|----------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `services.AddAwsKeyStore(Func<IServiceProvider,string> resolvePrefix)`                                               | Resolve the prefix from DI (multi-tenant hosts), register `AwsKeyStore` as a singleton.                                                                              |
| `services.AddAwsKeyStoreFromConfiguration(IConfiguration, configSectionName = "AwsKeyStore")`                        | Bind `AwsKeystoreOptions` + register `IAmazonSecretsManager` (when missing) + register `AwsKeyStore` and `IKeyStore`.                                                |
| `services.AddAmazonSecretsManagerFromConfiguration(IConfiguration, configSectionName = "AwsKeyStore")`               | Standalone `IAmazonSecretsManager` registration (no keystore). Honours static keys, region, and falls through to the default credential chain when keys are omitted. |
| `services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix)` / `<TKeyStore>`                       | Register a full keyed `ITwoKeyEncryptionService` stack using `AwsKeyStore` + paired `AesGcmEncryptionService` for both DEK and KEK.                                  |
| `services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix, AwsKeystoreOptions?)`                  | Same, but with explicit AWS options (region/credentials) rather than configuration binding.                                                                          |
| `services.AddTwoKeyEncryptionFromConfiguration(IConfiguration, keyedServiceName, configSectionName)` / `<TKeyStore>` | Bind `AwsKeystoreOptions` from configuration and wire the keyed two-key stack in one call.                                                                           |

Note that `AddAwsKeyStore(Func<...>)` registers `AwsKeyStore` as a concrete singleton only (no `IKeyStore` indirection); `AddAwsKeyStoreFromConfiguration` and the two-key
extensions also register `IKeyStore` so the rest of `Lyo.Encryption` can resolve it generically.

## See also

- [`Lyo.Keystore`](../Lyo.Keystore/README.md) — interfaces and local store.
- [`Lyo.Encryption`](../Lyo.Encryption/README.md) — encryption services that consume `IKeyStore`.
- [`../README.md`](../README.md) — encryption umbrella (algorithms, stream formats, threat model).
