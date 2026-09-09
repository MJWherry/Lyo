# Package layout (taxonomy-first)

This document is the standard for where new Lyo packages live and how they are named. The repo does **not** use a single global rule (pure vendor-first or pure feature-first). Each package is classified by **archetype**, then placed accordingly.

## Classification checklist

Answer in order:

1. **Lyo canonical model in Core?** Then **Archetype A.** `Core/{Domain}/`, e.g. `Lyo.People.Models`, `Lyo.Geolocation.Postgres`.
2. Else **pluggable Lyo interface under Communication or Security?** Then **Archetype B.** `Communication|Security/{Capability}/`, e.g. `Lyo.Translation.Google`.
3. Else **thin HTTP/SDK client** (host maps into Core or feeds an Archetype B adapter)? Then **Archetype C.** `Integration/{Vendor}/`, e.g. `Lyo.Endato.Client`.
4. Else **vendor-owned product** (own models/schema, not a Core projection)? Then **Archetype D.** `Integration/{Vendor}/` full stack, e.g. `Lyo.Discord.Postgres`.
5. Else **platform/infrastructure** (Plugins, Apps, Data, Tools, Examples)? Then **Archetype E.** Existing top-level areas. Drift’s store is `Security/Drift` (security control), not Plugins.

## Archetypes

### A. Lyo domain (canonical)

| | |
|---|---|
| **When** | Shared business model and persistence owned by Lyo (People, Geolocation, EntityReference). |
| **Path** | `Core/{Domain}/` |
| **Packages** | `Lyo.{Domain}.Models`, `Lyo.{Domain}` (contracts), `Lyo.{Domain}.Postgres`, tests |
| **Must not** | Reference vendor SDKs, vendor HTTP clients, or vendor DTO packages |

### B. Capability + provider

| | |
|---|---|
| **When** | Multiple vendors implement one Lyo interface (`ITranslationService`, `ISmsService`, `ITtsService`, OpenID providers, etc.). |
| **Path** | `Communication/{Capability}/` or `Security/{Capability}/` |
| **Packages** | `Lyo.{Capability}` + `Lyo.{Capability}.{Vendor}` |
| **Stay** | Providers remain under the capability folder. Do **not** move to `Integration/{Vendor}/`. |

**Typecast split (optional).** Extract `Lyo.{Vendor}.{Capability}.Client` under `Integration/{Vendor}/` (Archetype C) and keep `Lyo.{Capability}.{Vendor}` as the thin adapter (`Lyo.Tts.Typecast` → `Lyo.Typecast.Client`).

### C. Vendor client

| | |
|---|---|
| **When** | Calls an external API; maps to Core at **host/worker** ingest, or supplies an Archetype B adapter. |
| **Path** | `Integration/{Vendor}/` |
| **Name** | `Lyo.{Vendor}.{Domain}.Client` or `Lyo.{Vendor}.{Product}.Client` |
| **Examples** | `Lyo.Endato.Client`, `Lyo.Google.Geolocation.Client`, `Lyo.Typecast.Client`, `Lyo.Espn.Fantasy.Football.Client` |

Geolocation is **not** Communication. Google Maps lives here (Archetype C), not beside `Lyo.Translation.Google` (Archetype B).

### D. Vendor product vertical

| | |
|---|---|
| **When** | The integration is the product (own models/schema), not canonical Lyo domain storage. |
| **Path** | `Integration/{Vendor}/` (Models, Client, Postgres, Bot, …) |
| **Examples** | `Lyo.Discord.*`, `Lyo.Endato.Postgres` (vendor cache/staging; separate from `people` schema) |

### E. Platform

| | |
|---|---|
| **When** | Cross-cutting app infrastructure, not a vendor product. |
| **Path** | `Plugins/*`, `Apps/*` (`Api`, `Web`, `Http`, plus product HTTP/process folders), `Data/*`, `Tools/*`, `Examples/*`. Drift plugin packages live under `Security/Drift`. |

## Dependency law

**Allowed**

- `Communication` / `Security` → `Core`
- `Integration` (vendor client) → `Core` (e.g. `Lyo.Google.Geolocation.Client` → `Lyo.Geolocation.Models`)
- `Communication` / `Security` provider → `Integration` vendor client (Typecast: `Lyo.Tts.Typecast` → `Lyo.Typecast.Client`)

**Forbidden**

- `Core` → `Apps` platform packages, `Integration` vendors, or vendor SDKs
- Moving Archetype B providers out of `Communication`/`Security` into `Integration/{Vendor}/`

**Host**

- Wire vendor client → mapper → Core store (`IPeopleStore`, `IGeolocationStore`, etc.).
- Define external source type strings (e.g. `EndatoPsPerson`, `GoogleMapsPlace`) in the vendor/mapper package, not in Core. Persist on `*_source` rows as **`source_entity_type`** / **`source_entity_id`** with a module owner column (e.g. `person_id`). EntityReference does not add PostgreSQL FK constraints on source links.
- Tenant-scoped **relations** (favorite, note, …) use **`for_entity_*`** / **`from_entity_*`** for subject/actor endpoints via `EntityRelationEntityBase`.

## Naming

| Archetype | Assembly | Folder |
|-----------|----------|--------|
| A | `Lyo.People.Models`, `Lyo.Geolocation.Postgres` | `Core/People/Lyo.People.Models` |
| B | `Lyo.Translation.Google` | `Communication/Translation/Lyo.Translation.Google` |
| C | `Lyo.{Vendor}.{Domain}.Client` | `Integration/{Vendor}/Lyo.{Vendor}.{Domain}.Client` |
| D | `Lyo.{Vendor}.*` | `Integration/{Vendor}/Lyo.{Vendor}.*` |

`.Client` is required for thin HTTP/SDK wrappers (C). Optional for monolithic B providers (`Lyo.Sms.Twilio`).

## Worked examples

### People + Endato (A + C + D)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.People.Models`, `Lyo.People.Postgres` | A | Canonical `people.*` + `*_source` |
| `Lyo.Endato.Client` | C | Endato REST API |
| `Lyo.Endato.Postgres` | D | Vendor-side persistence (not `people` schema) |

Host/worker: Endato DTO → mapper → `Person` + `person_source` rows (`source`: `EndatoPsPerson`/vendor id, owner via `person_id`) → `IPeopleStore`. Optional `EntityRef` to `geolocation.address` for enriched locations.

### Geolocation + Google (A + C)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Geolocation`, `Lyo.Geolocation.Models`, `Lyo.Geolocation.Postgres` | A | Canonical `geolocation.address` + `address_source` |
| `Lyo.Google.Geolocation.Client` | C | Maps API + `IGeolocationService` (monolithic; split optional later) |

Path: **`Integration/Google/Lyo.Google.Geolocation.Client`** (vendor folder; Geolocation has no Communication parent).

### Translation + Google (B)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Translation` | B (abstract) | `ITranslationService` |
| `Lyo.Translation.Google` | B | Google Translate provider |

Stays under `Communication/Translation/`.

### Typecast (C + B)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Typecast.Client` | C | Raw Typecast API |
| `Lyo.Tts.Typecast` | B | `ITtsService` adapter |

### ESPN (C, client-only)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Espn.Fantasy.Football.Client` | C | Fantasy API client; no Core domain DB |

### Discord (D)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Discord.Models`, `Client`, `Postgres`, `Bot` | D | Full Discord vertical |

### Media (E, Data)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Media.Models` | E | Sibling `IAudio*` / `IVideo*` contracts, encoder catalogs, probe/play/convert records. Images stay `IImageService` in `Data/Images/`. |
| `Lyo.FFmpeg`, `Lyo.FFmpeg.Models`, `Lyo.FFmpeg.Tests` | E | One ffmpeg CLI implementation of both families. Lives under `Data/Media/`, not `Data/FFmpeg/`. |

---

## Inventory: Integration (vendor / product)

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Endato.Client` | C | `Integration/Endato/` | - | - | Ingest → Core/People in host |
| `Lyo.Endato.Postgres` | D | `Integration/Endato/` | - | - | Vendor DB, not `people` |
| `Lyo.Google.Geolocation.Client` | C | `Integration/Google/` | - | 1 | Also implements `IGeolocationService`; split deferred |
| `Lyo.Typecast.Client` | C | `Integration/Typecast/` | - | - | Used by `Lyo.Tts.Typecast` |
| `Lyo.Espn.Fantasy.Football.Client` | C | `Integration/Espn/` | - | 2 | Name lacks `.Client` suffix; acceptable |
| `Lyo.Discord.Models` | D | `Integration/Discord/` | - | - | |
| `Lyo.Discord.Client` | D | `Integration/Discord/` | - | - | |
| `Lyo.Discord.Postgres` | D | `Integration/Discord/` | - | - | |
| `Lyo.Discord.Bot` | D | `Integration/Discord/` | - | - | |

## Inventory: Apps (HTTP and processes)

**Plugins** = models / Postgres / `.Web.Components`. **Apps** = HTTP and processes: `.Api` / `.Api.Host` / `.Client`, Worker / Scheduler / Agent, plus the platform toolkit (`Apps/Api`, `Apps/Web`, `Apps/Http`). Plugins Postgres may still reference `Lyo.Api` for QueryProject/CRUD helpers; route mapping (`Build*Group`) lives in Apps.

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Api` | E | `Apps/Api/` | - | - | |
| `Lyo.Api.Client` | E | `Apps/Api/` | - | - | Lyo-only: problem-details, Query/QueryProject. Vendors use `Lyo.Http.Client` |
| `Lyo.Http.Client` | E | `Apps/Http/` | - | - | Generic HTTP + plans + extract; `IHttpClientFactory` |
| `Lyo.Http.Client.Flared` | E | `Apps/Http/` | - | - | FlareSolverr `/v1` ThroughSolver; compose profile `flared` |
| `Lyo.Api.Models` | E | `Apps/Api/` | - | - | |
| `Lyo.Api.FileStorage.Models` | E | `Apps/Api/` | - | - | HTTP DTOs for file-storage API; referenced by API + clients |
| `Lyo.Api.FileStorage` | E | `Apps/Api/` | - | - | File-storage HTTP + FileMetadata QueryProject (platform wrapper over Data) |
| `Lyo.Api.Export*` | E | `Apps/Api/` | - | - | |
| `Lyo.Api.Authentication` | E | `Apps/Api/` | - | - | Auth admin QueryProject; host-supplied EndpointAuth (platform wrapper over Security). Do not rename to `Lyo.Authentication.Api`. |
| `Lyo.Web.*` | E | `Apps/Web/` | - | - | |
| `Lyo.Job.Api` | E | `Apps/Job/` | - | - | HTTP mapping (`BuildJobGroup`) |
| `Lyo.Job.Api.Host` | E | `Apps/Job/` | - | - | Standalone host; embed in another API when composing |
| `Lyo.Job.Client` | E | `Apps/Job/` | - | - | Typed HTTP client for workers/schedulers |
| `Lyo.Job.Worker` | E | `Apps/Job/` | - | - | |
| `Lyo.Job.Scheduler` | E | `Apps/Job/` | - | - | |
| `Lyo.Job.Alerts` | E | `Apps/Job/` | - | - | |
| `Lyo.Reporting.Api` | E | `Apps/Reporting/` | - | - | HTTP (`BuildReportingGroup`); Postgres stays service-only |
| `Lyo.Reporting.Api.Host` | E | `Apps/Reporting/` | - | - | Standalone host |
| `Lyo.Reporting.Client` | E | `Apps/Reporting/` | - | - | Typed HTTP client |
| `Lyo.Drift.Api` | E | `Apps/Drift/` | - | - | HTTP ingest (`BuildDriftGroup`) |
| `Lyo.Drift.Api.Host` | E | `Apps/Drift/` | - | - | Standalone host |
| `Lyo.Drift.Client` | E | `Apps/Drift/` | - | - | |
| `Lyo.Drift.Agent` | E | `Apps/Drift/` | - | - | Collector process |
| `Lyo.Comic.Api` | E | `Apps/Comic/` | - | - | HTTP over `Plugins/Comic` |
| `Lyo.Comic.Api.Host` | E | `Apps/Comic/` | - | - | Standalone host (`http://localhost:5101`) |
| `Lyo.HomeInventory.Api` | E | `Apps/HomeInventory/` | - | - | HTTP over `Plugins/HomeInventory` |
| `Lyo.HomeInventory.Api.Host` | E | `Apps/HomeInventory/` | - | - | Standalone host (`http://localhost:5102`) |
| `Lyo.Config.Api*` | E | `Apps/Config/` | - | - | Product HTTP surface over `Plugins/Config` |

## Inventory: Plugins

Drop-in modules. No `Build*Group` route mapping.

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Comment*` | E | `Plugins/Comment/` | - | - | |
| `Lyo.Note*` | E | `Plugins/Note/` | - | - | |
| `Lyo.Favorite*` | E | `Plugins/Favorite/` | - | - | |
| `Lyo.Tag*` | E | `Plugins/Tag/` | - | - | |
| `Lyo.Rating*` | E | `Plugins/Rating/` | - | - | |
| `Lyo.ContactUs*` | E | `Plugins/ContactUs/` | - | - | |
| `Lyo.Profanity` | E | `Plugins/Profanity/` | - | - | |
| `Lyo.ShortUrl*` | E | `Plugins/ShortUrl/` | - | - | |
| `Lyo.Config`, `.Postgres`, `.Web.Components` | E | `Plugins/Config/` | - | - | HTTP is `Apps/Config` |
| `Lyo.Comic`, `.Postgres`, `.Web.Components` | E | `Plugins/Comic/` | - | - | HTTP is `Apps/Comic` |
| `Lyo.HomeInventory`, `.Postgres` | E | `Plugins/HomeInventory/` | - | - | HTTP is `Apps/HomeInventory` |
| `Lyo.Job.Models`, `.Postgres`, `.Web.Components`, `.Tests` | E | `Plugins/Job/` | - | - | Store/domain. HTTP/process stay in `Apps/Job` |
| `Lyo.Reporting.Models`, `.Postgres`, `.Web`, `.Web.Components`, `.Tests` | E | `Plugins/Reporting/` | - | - | Store/UI. `AddReportingMaintenanceWorker` stays on Postgres. HTTP is `Apps/Reporting` |

## Inventory: Communication (capabilities)

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Translation` | B | `Communication/Translation/` | - | - | Abstract |
| `Lyo.Translation.Google` | B | `Communication/Translation/` | - | - | Stays |
| `Lyo.Translation.Aws` | B | `Communication/Translation/` | - | - | Stays |
| `Lyo.Sms` | B | `Communication/Sms/` | - | - | Abstract |
| `Lyo.Sms.Twilio` | B | `Communication/Sms/` | - | - | Monolithic SDK+provider |
| `Lyo.Sms.Twilio.Postgres` | D | `Communication/Sms/` | - | - | Twilio-scoped DB |
| `Lyo.Sms.Postgres` | A-like | `Communication/Sms/` | - | - | Lyo SMS persistence |
| `Lyo.Tts` | B | `Communication/Speech/` | - | - | Abstract |
| `Lyo.Tts.Typecast` | B | `Communication/Speech/` | - | - | → `Lyo.Typecast.Client` |
| `Lyo.Tts.AwsPolly` | B | `Communication/Speech/` | - | - | |
| `Lyo.Tts.WindowsSpeech` | B | `Communication/Speech/` | - | - | Packable Windows TTS provider (zero in-tree hosts; keep) |
| `Lyo.Stt` | B | `Communication/Speech/` | - | - | Abstract STT contract; no in-repo provider |
| `Lyo.Webhook` | B | `Communication/Webhook/` | - | 7 | ASP.NET inbound webhooks. `AssemblyName`/`PackageId` unchanged. |
| `Lyo.Webhook.Twilio` | B | `Communication/Webhook/` | - | 7 | HMAC verifier; no Twilio SDK. Stays a B provider, not Integration. |
| `Lyo.Email` | B | `Communication/Email/` | - | - | |
| `Lyo.Email.Postgres` | A-like | `Communication/Email/` | - | - | |
| `Lyo.MessageQueue` | B | `Communication/MessageQueue/` | - | - | |
| `Lyo.MessageQueue.RabbitMq` | B | `Communication/MessageQueue/` | - | - | |

## Inventory: Security (capabilities)

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Authentication` | B | `Security/Authentication/` | - | - | |
| `Lyo.Authentication.Google` | B | `Security/Authentication/` | - | - | Stays |
| `Lyo.Authentication.Keycloak` | B | `Security/Authentication/` | - | - | Stays |
| `Lyo.Authentication.OpenIdConnect` | B | `Security/Authentication/` | - | - | |
| `Lyo.Authentication.Postgres` | A-like | `Security/Authentication/` | - | - | Identity store |
| `Lyo.Encryption` + algorithm packages | E/B | `Security/Encryption/` | - | - | |
| `Lyo.KeyStore` | E/B | `Security/KeyStore/` | - | - | |
| `Lyo.KeyStore.Aws` | B | `Security/KeyStore/` | - | - | AWS Secrets Manager provider |
| `Lyo.KeyStore.Web.Components` | B | `Security/KeyStore/` | - | - | In-process IKeyStore workbench (no HTTP) |
| `Lyo.ContentThreatScan` | B | `Security/ContentThreatScan/` | - | - | |
| `Lyo.ContentThreatScan.Intel` | B | `Security/ContentThreatScan/` | - | - | |
| `Lyo.Drift.Models` | E | `Security/Drift/` | - | - | File-integrity / config-drift contracts. Not a capability+provider. Uses Hashing. |
| `Lyo.Drift.Postgres` | E | `Security/Drift/` | - | - | Store only. HTTP is `Lyo.Drift.Api`. |
| `Lyo.Drift.Web.Components` | E | `Security/Drift/` | - | - | Workbench UI |
| `Lyo.Drift.Tests` | E | `Security/Drift/` | - | - | Store + TestServer against `Lyo.Drift.Api` |

## Inventory: Core domains (reference)

| Project | Archetype | Path |
|---------|-----------|------|
| `Lyo.People.Models` | A | `Core/People/` |
| `Lyo.People.Postgres` | A | `Core/People/` |
| `Lyo.Geolocation.Models` | A | `Core/Geolocation/` |
| `Lyo.Geolocation` | A | `Core/Geolocation/` |
| `Lyo.Geolocation.Postgres` | A | `Core/Geolocation/` |
| `Lyo.EntityReference.Models` | A | `Core/EntityReference/` |
| `Lyo.EntityReference.Postgres` | A | `Core/EntityReference/` |
| `Lyo.Validation` | A | `Core/Validation/` |
| `Lyo.Validation.Postgres` | A | `Core/Validation/` |
| `Lyo.Web.Primitives` | E | `Core/Web/` — Blazor primitives used by workbenches. Platform-shaped; stays in Core unless a later move is approved. |
| `Lyo.Schedule.Models` / `Lyo.Scheduler*` | E | `Core/Schedule/` and `Core/Scheduler/` — calendar models vs in-process scheduler. |
| `Lyo.Metrics*` | E | `Core/Metrics/` |
| `Lyo.Webhook*` | B | `Communication/Webhook/` — `AssemblyName`/`PackageId` unchanged. Do not move providers into Integration. |

Zero-inbound **packable** libraries (`Lyo.Stt`, `Lyo.Tts.WindowsSpeech`, `Lyo.Translation.Google`, `Lyo.Scheduler.Cache`, `Lyo.Pdf.Ocr`, `Lyo.Google.Geolocation.Client`, `Lyo.Config.Api.Hosting`) are public NuGet features, not orphans. `Lyo.DataTable` is a name-reservation package (types live in `Lyo.DataTable.Models`); do not unlist without an explicit decision.

**Core → Security (crypto).** `Lyo.Cache` / `Lyo.Cache.Fusion` → `Lyo.Encryption`, and `Lyo.Diagnostic` / `Lyo.Privacy` / `Lyo.PackageMetadata` → `Lyo.Hashing`, are allowed exceptions: hashing and encryption are treated as primitives those Core packages need. Do not "fix" by deleting the references. See [cleanup.md](cleanup.md).

### EntityReference (Archetype A)

Shared `EntityRef` value type and PostgreSQL persistence helpers. Two row families:

| Package | Role |
|---------|------|
| `Lyo.EntityReference.Models` | `EntityRef`, relation domain (`EntityRelationRow`, `EntityRelationEndpoints`, `EntityRelationValidation`), source provenance (`EntitySourceRecord`, `IEntitySourceDerived`, `EntitySourceValidation`), JSON/composite/interceptors |
| `Lyo.EntityReference.Postgres` | EF bases: `EntityRelationEntityBase` (subject/actor → `for_entity_*` / `from_entity_*`), `EntitySourceLinkEntityBase` (`source_entity_*` + `imported_at`), `EntitySourceDerivedEntityBase` (`LocallyModifiedAt`); shared indexes; no PG FK on source links |

Domain modules (People, Geolocation, Favorite, …) subclass these bases and own module-specific owner columns and store logic.

---

## Phase 2: naming audit (no moves)

| Item | Status | Action |
|------|--------|--------|
| `Lyo.Google.Geolocation.Client` | OK | Matches `Lyo.{Vendor}.{Domain}.Client` |
| `Lyo.Espn.Fantasy.Football.Client` | Minor | Product name; `.Client` optional |
| `Lyo.Endato.Client` | OK | Vendor-first segment |
| `Lyo.Sms.Twilio` | OK | Monolithic B; rename not worth churn |
| Split Google geolocation client / adapter | Deferred | Only if second maps vendor added (Typecast pattern) |

## Phase 3: consolidation

| Item | Action |
|------|--------|
| Communication/Security providers → `Integration/{Vendor}` | **Not done** (forbidden by taxonomy) |
| `Integration/People/` folder | **Not created.** Endato→People documented above only |
| Typecast + Tts.Typecast | **Already correct.** Template for future splits |
| Discord | **No change.** Archetype D, stays under `Integration/{Vendor}/` |
| Api / Web / Job / Reporting | **Moved** to `Apps/{Api\|Web\|Job\|Reporting}/`. Archetype E |

---

## Adding a new package

1. Run the classification checklist.
2. Pick folder and assembly name from the naming table.
3. Verify project references obey dependency law.
4. Add a row to the inventory section if the package is Apps, Plugins, Integration, Communication, or Security.
