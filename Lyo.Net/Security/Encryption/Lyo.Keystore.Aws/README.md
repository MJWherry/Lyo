# Lyo.Keystore.Aws

**`AwsKeyStore`** (**`IAmazonSecretsManager`** + secret name prefix) implements **`Lyo.Keystore.IKeyStore`** and **`IKeyInventoryStore`**.

The backing secret is JSON `{ "keyId": "plaintext-or-material", ... }`. Logical versions map onto AWS **`VersionId`** stages; unresolved requests fall through **`AWSCURRENT`**.
String values run through derivation so callers receive **cryptographic‑length KEK bytes** usable by **`AesGcmEncryptionService`** and friends.

## DI

- **`AddAwsKeyStore(Func<IServiceProvider,string>)`** — resolves prefix per provider, registers **`AwsKeyStore`**.
- **`AddAwsKeyStoreFromConfiguration`** — binds **`SecretNamePrefix`**, wires **`IKeyStore`** when **`IAmazonSecretsManager`** absent.
- **`AddAmazonSecretsManagerFromConfiguration`** / **`AwsKeystoreOptions`** — access key vs default chain, **`Region`** (default **`us-east-2`**), prefix.
- **`AddTwoKeyEncryptionServiceKeyed`** / **`AddTwoKeyEncryptionFromConfiguration`** — full keyed **`ITwoKeyEncryptionService`** stacks ( **`AwsKeyStore`** + paired *
  *`AesGcmEncryptionService`** DEK/KEK).

See **`Lyo.Keystore`** contracts and **`../README.md`** for encryption‑stack context.
