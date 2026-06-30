# Glossary

Recurring terms and concepts across Lyo. Package names are linked from the root
[`README.md`](../README.md); this page defines the ideas behind them.

## Structure and taxonomy

- **Area** — a top-level folder under `Lyo.Net/` (`Core`, `Data`, `Security`,
  `Communication`, `Integration`, `Features`, `Apps`, `Tools`). See
  [Architecture](architecture.md).
- **Archetype** — the classification (A–E) that decides where a package lives and
  how it may depend on others. See
  [`package-layout.md`](../Lyo.Net/docs/package-layout.md).
- **Capability + provider** — an abstract Lyo interface (`Lyo.{Capability}`) with
  one or more vendor implementations (`Lyo.{Capability}.{Vendor}`), e.g. SMS,
  TTS, translation, authentication. (Archetype B.)
- **Vendor client** — a thin HTTP/SDK wrapper around an external API, living under
  `Integration/{Vendor}/`. (Archetype C.)
- **Dependency law** — the allowed/forbidden reference directions between areas;
  `Core` never depends on `Integration` or vendor SDKs.

## Domain model

- **EntityRef** — a shared value type identifying any entity (type + id), defined
  in `Lyo.EntityReference.Models`. Used to relate features (notes, favorites,
  tags, ...) to arbitrary subjects without hard foreign keys.
- **Entity relation** — a tenant-scoped link between a subject and an actor,
  persisted with `for_entity_*` / `from_entity_*` columns via
  `EntityRelationEntityBase`.
- **Entity source** — provenance for data imported from an external system,
  persisted with `source_entity_type` / `source_entity_id` on `*_source` rows,
  owned by a module column (e.g. `person_id`). EntityReference does not add
  PostgreSQL FK constraints on source links.
- **Canonical (Lyo) domain** — a business model and persistence owned by Lyo
  itself (People, Geolocation, EntityReference), as opposed to a vendor's own
  schema. (Archetype A.)

## Data and query

- **Query engine** — the filter/include/projection model exposed by `Lyo.Api`,
  with DTOs and `WhereClause` builders shared via `Lyo.Query.Models`.
- **Projection** — shaping a query response to selected fields, optionally with
  computed columns and `entityTypes` metadata.

## Security and encryption

- **AEAD** — Authenticated Encryption with Associated Data; the authenticated
  cipher families used here (AES-GCM, ChaCha20-Poly1305, AES-CCM/SIV, XChaCha20).
- **KEK / DEK** — Key Encryption Key / Data Encryption Key. Envelope ("two-key")
  encryption generates a unique DEK per operation and wraps it with a KEK held in
  the key store.
- **KeyStore** — the `IKeyStore` abstraction for resolving keys by `keyId` and
  version; `LocalKeyStore` is for development only.
- **Nonce** — a per-operation number-used-once for AEAD; Lyo generates these
  automatically (per-stream random prefix + per-chunk counter for streaming).
- **Streaming chunk frame** — the on-the-wire layout for streamed ciphertext; see
  [security/encryption.md](security/encryption.md).

## Tooling

- **Runner / `TARGET`** — the container benchmark/test runner and the variable
  that selects which projects it builds and runs. See
  [Configuration](configuration.md) and [Testing](testing.md).
- **Dashboard manifests** — normalized benchmark/k6 result files under
  `docs/benchmarks/data/` rendered by the dashboard viewer.
