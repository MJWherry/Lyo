# Glossary

Recurring terms and concepts across Lyo. Package names are linked from the root [`README.md`](../README.md). This page defines the ideas behind them.

## Structure and taxonomy

- **Area.** A top-level folder under `Lyo.Net/` (`Core`, `Data`, `Security`, `Communication`, `Integration`, `Features`, `Apps`, `Tools`). See [Architecture](architecture.md).
- **Archetype.** The classification (A–E) that decides where a package lives and how it may depend on others. See [`package-layout.md`](../Lyo.Net/docs/package-layout.md).
- **Capability + provider.** An abstract Lyo interface (`Lyo.{Capability}`) with one or more vendor implementations (`Lyo.{Capability}.{Vendor}`), for example SMS, TTS, translation, authentication. Archetype B.
- **Vendor client.** A thin HTTP/SDK wrapper around an external API, living under `Integration/{Vendor}/`. Archetype C.
- **Dependency law.** The allowed and forbidden reference directions between areas. `Core` never depends on `Integration` or vendor SDKs.

## Domain model

- **EntityRef.** A shared value type identifying any entity (type + id), defined in `Lyo.EntityReference.Models`. Used to relate features (notes, favorites, tags, ...) to arbitrary subjects without hard foreign keys.
- **Entity relation.** A tenant-scoped link between a subject and an actor, persisted with `for_entity_*` / `from_entity_*` columns via `EntityRelationEntityBase`.
- **Entity source.** Provenance for data imported from an external system, persisted with `source_entity_type` / `source_entity_id` on `*_source` rows, owned by a module column (for example `person_id`). EntityReference does not add PostgreSQL FK constraints on source links.
- **Canonical (Lyo) domain.** A business model and persistence owned by Lyo itself (People, Geolocation, EntityReference), as opposed to a vendor's own schema. Archetype A.

## Data and query

- **Query engine.** The filter/include/projection model exposed by `Lyo.Api`, with DTOs and `WhereClause` builders shared via `Lyo.Query.Models`.
- **Projection.** Shaping a query response to selected fields, optionally with computed columns and `entityTypes` metadata.

## Security and encryption

- **AEAD.** Authenticated Encryption with Associated Data. The authenticated cipher families used here (AES-GCM, ChaCha20-Poly1305, AES-CCM/SIV, XChaCha20).
- **KEK / DEK.** Key Encryption Key / Data Encryption Key. Envelope ("two-key") encryption generates a unique DEK per operation and wraps it with a KEK held in the key store.
- **KeyStore.** The `IKeyStore` abstraction for resolving keys by `keyId` and version. `LocalKeyStore` is for development only.
- **Nonce.** A per-operation number-used-once for AEAD. Lyo generates these automatically (per-stream random prefix + per-chunk counter for streaming).
- **Streaming chunk frame.** The on-the-wire layout for streamed ciphertext. See [security/encryption.md](security/encryption.md).

## Tooling

- **Runner / `TARGET`.** The container benchmark/test runner and the variable that selects which projects it builds and runs. See [Configuration](configuration.md) and [Testing](testing.md).
- **Dashboard manifests.** Normalized benchmark/k6 result files under `docs/benchmarks/data/` rendered by the dashboard viewer.
