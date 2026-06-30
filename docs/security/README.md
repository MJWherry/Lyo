# Security

Lyo ships cryptography and security-sensitive packages (encryption, hashing,
authentication providers, content-threat scanning). This page describes the
security model at the project level. For vulnerability reporting, see the root
[`SECURITY.md`](../../SECURITY.md). For cryptographic design details, see
[encryption.md](encryption.md).

## Security-relevant packages

| Package(s) | Role |
|------------|------|
| [`Lyo.Encryption`](../../Lyo.Net/Security/Encryption/README.md) (+ algorithm add-ons) | Authenticated encryption (AES-GCM, ChaCha20-Poly1305, AES-CCM, AES-SIV, XChaCha20-Poly1305), RSA/hybrid, envelope/two-key. |
| `Lyo.Keystore` / `Lyo.Keystore.Aws` | Key resolution by `keyId`/version; `LocalKeyStore` (dev) and AWS KMS-backed stores. |
| [`Lyo.Hashing`](../../Lyo.Net/Security/Hashing/Lyo.Hashing/README.md) | SHA-2 digests, stream hashing; MD5 only for non-security fingerprints. |
| `Lyo.Authentication.*` | OpenID Connect / Keycloak / Google providers and identity persistence. |
| [`Lyo.ContentThreatScan`](../../Lyo.Net/Security/ContentThreat/Lyo.ContentThreatScan/README.md) (+ `.Intel`) | Heuristic content scoring and optional reputation intel (Malware Bazaar, VirusTotal, `clamd`). |

## Threat model (overview)

Lyo is a library toolkit; the ultimate trust boundary is the **consuming
application**. The notes below scope what the libraries do and do not defend
against.

### Assets

- Plaintext and the symmetric/asymmetric key material protecting it.
- Key Encryption Keys (KEKs) held in a key store, and Data Encryption Keys (DEKs)
  wrapped per operation.
- Authentication credentials/tokens handled by the auth providers.

### What the crypto layer defends against

- **Confidentiality + integrity of data at rest / in transit through the library:**
  all symmetric algorithms are AEAD, so tampering with ciphertext, nonce, length
  framing, or tag is detected on decrypt and surfaces as
  `DecryptionFailedException`. This is covered by tests including tamper detection
  and cross-algorithm rejection (see
  [`StreamingChunkFormatTests`](../../Lyo.Net/Security/Encryption/Lyo.Encryption.Tests/StreamingChunkFormatTests.cs)).
- **Nonce reuse:** nonces are generated automatically (hybrid random prefix +
  per-chunk counter for streaming); per-chunk nonce uniqueness is tested.
- **DoS via oversized input:** services enforce `MaxInputSize`/`MinInputSize`.

### What is the caller's responsibility

- **Key custody.** `LocalKeyStore` is in-memory and for development only. In
  production, use a managed `IKeyStore` (AWS KMS via `Lyo.Keystore.Aws`, Azure Key
  Vault, HSM, or your own implementation) and never commit key material.
- **Key rotation.** Rotate KEKs on a schedule; old key versions are retained so
  existing ciphertext stays decryptable.
- **Secret delivery.** Supply KEK secrets and vendor API keys via your platform's
  secret manager, not source or images.
- **Transport security.** Use TLS for any network transport; the library secures
  payloads, not the channel.
- **Error hygiene.** Do not leak sensitive details from caught exceptions; log
  and alert on repeated `DecryptionFailedException` (possible attack).
- **Algorithm/parameter choice.** Defaults follow current guidance (AES-256-GCM,
  RSA >= 2048 with OAEP-SHA256, 3072+ recommended for new RSA keys), but the
  caller must pick appropriate algorithms for their threat model.

### Out of scope

- Side-channel resistance beyond what the underlying platform/BCL/BouncyCastle
  primitives provide.
- Protecting plaintext once it has been decrypted into the host process memory.
- The security of third-party vendor services integrated via Communication /
  Integration packages.

## Reporting a vulnerability

Please do not open public issues for security problems. Follow the process in the
root [`SECURITY.md`](../../SECURITY.md).
