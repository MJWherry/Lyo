# Package layout (taxonomy-first)

This document is the standard for where new Lyo packages live and how they are named. The repo does **not** use a single global rule (pure vendor-first or pure feature-first). Each package is classified by **archetype**, then placed accordingly.

## Classification checklist

Answer in order:

1. **Lyo canonical model in Core?** Then **Archetype A.** `Core/{Domain}/`, e.g. `Lyo.Geolocation.Models`, `Lyo.Geolocation.Postgres`.
2. Else **pluggable Lyo interface under Communication or Security?** Then **Archetype B.** `Communication|Security/{Capability}/`, e.g. `Lyo.Translation.Google`.
3. Else **thin HTTP/SDK client** (host maps into Core or feeds an Archetype B adapter)? Then **Archetype C.** `Integration/{Vendor}/`, e.g. `Lyo.Endato.Client`.
4. Else **vendor-owned product** (own models/schema, not a Core projection)? Then **Archetype D.** `Integration/{Vendor}/` full stack, e.g. `Lyo.Discord.Postgres`.
5. Else **platform/infrastructure** (Plugins, Apps, Data, Tools, Examples)? Then **Archetype E.** Existing top-level areas. Drift’s store is `Security/Drift` (security control), not Plugins.

## Archetypes

### A. Lyo domain (canonical)

| | |
|---|---|
| **When** | Shared business model and persistence owned by Lyo (Geolocation, EntityReference). People is a plugin. |
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

- Wire vendor client → mapper → store (`IGeolocationStore` in Core, `IPeopleStore` in the People plugin).
- Define external source type strings (e.g. `EndatoPsPerson`, `GoogleMapsPlace`) in the vendor/mapper package, not in Core. Persist on `*_source` rows as **`source_entity_type`** / **`source_entity_id`** with a module owner column (e.g. `person_id`). EntityReference does not add PostgreSQL FK constraints on source links.
- Tenant-scoped **relations** (favorite, note, …) use **`for_entity_*`** / **`from_entity_*`** for subject/actor endpoints via `EntityRelationEntityBase`.

## Naming

| Archetype | Assembly | Folder |
|-----------|----------|--------|
| A | `Lyo.Geolocation.Models`, `Lyo.Geolocation.Postgres` | `Core/Geolocation/Lyo.Geolocation.Models` |
| B | `Lyo.Translation.Google` | `Communication/Translation/Lyo.Translation.Google` |
| C | `Lyo.{Vendor}.{Domain}.Client` | `Integration/{Vendor}/Lyo.{Vendor}.{Domain}.Client` |
| D | `Lyo.{Vendor}.*` | `Integration/{Vendor}/Lyo.{Vendor}.*` |

`.Client` is required for thin HTTP/SDK wrappers (C). Optional for monolithic B providers (`Lyo.Sms.Twilio`).

## Worked examples

### People + Endato (E + C + D)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.People.Models`, `Lyo.People.Postgres` | E | Plugin `people.*` + `*_source` under `Plugins/People/` |
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

### File tags (E, Data)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.FileTags` | E | Shared `FileTagKey` catalog and `IFileTagService` dispatcher. No ffmpeg and no ImageSharp. |
| `Lyo.FileTags.FFmpeg` | E | Audio and video tag read (ffprobe) and stream-copy write. Does not claim images. |
| `Lyo.FileTags.Images` | E | Picture EXIF tags. Width, height, and format stay on `IImageService`. |

### Query (E, Data)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Query.Models` | E | WhereClause AST, request DTOs, builders |
| `Lyo.Query.Evaluation` | E | EF-agnostic expression trees + `FuzzyMatch` |
| `Lyo.Query` | E | Cached `IWhereClauseService` |
| `Lyo.Query.Postgres` | E | Rewrite Fuzzy / NotFuzzy to `pg_trgm` `similarity()` |
| `Lyo.Query.Sql` | E | Concrete, projection, and root requests printed as parameterized Postgres SQL |

### Algorithms (E, Data)

| Package | Archetype | Role |
|---------|-----------|------|
| `Lyo.Algorithms.Models` | E | Catalog DTOs, `AlgorithmKind`, and route constants |
| `Lyo.Algorithms` | E | Compiler, type binding, in-memory store, and unsaved preview |
| `Lyo.Algorithms.Postgres` | E | Postgres definition store |
| `Lyo.Algorithms.Sqlite` | E | SQLite definition store |
| `Lyo.Algorithms.Seed` | E | Fishing derby seed revision |
| `Lyo.Algorithms.Web.Components` | E | Blazor workbench: kind editors, typed context bag, and live breakdown |

---

## Inventory: Integration (vendor / product)

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Endato.Client` | C | `Integration/Endato/` | - | - | Ingest → `Plugins/People` in the host |
| `Lyo.Endato.Postgres` | D | `Integration/Endato/` | - | - | Vendor DB, not `people` |
| `Lyo.Google.Geolocation.Client` | C | `Integration/Google/` | - | 1 | Also implements `IGeolocationService`; split deferred |
| `Lyo.Google.Youtube.Models` | C | `Integration/Google/` | - | - | DTOs only. `Lyo.Video` references this, not the client |
| `Lyo.Google.Youtube.Client` | C | `Integration/Google/` | - | - | Data API. Hosts pass models into `IVideoStore` |
| `Lyo.Typecast.Client` | C | `Integration/Typecast/` | - | - | Used by `Lyo.Tts.Typecast` |
| `Lyo.ClamAv.Client` | C | `Integration/ClamAv/` | - | - | clamav-rest. Used by `Lyo.ContentThreatScan.ClamAv` |
| `Lyo.Espn.Fantasy.Football.Client` | C | `Integration/Espn/` | - | 2 | Name lacks `.Client` suffix; acceptable |
| `Lyo.OpenSubtitles.Client` | C | `Integration/OpenSubtitles/` | - | - | Consumer REST. Host stores subtitle files |
| `Lyo.Trakt.Client` | C | `Integration/Trakt/` | - | - | Catalog and OAuth. Host stores tokens |
| `Lyo.Discord.Models` | D | `Integration/Discord/` | - | - | |
| `Lyo.Discord.Client` | D | `Integration/Discord/` | - | - | |
| `Lyo.Discord.Postgres` | D | `Integration/Discord/` | - | - | |
| `Lyo.Discord.Bot` | D | `Integration/Discord/` | - | - | |

## Inventory: Apps (HTTP and processes)

**Plugins** = models / Postgres / `.Web.Components`. **Apps** = HTTP and processes: `.Api` / `.Client`, Worker / Scheduler / Agent, plus the platform toolkit (`Apps/Api`, `Apps/Web`, `Apps/Http`). Plugins Postgres may still reference `Lyo.Api` for QueryProject/CRUD helpers; route mapping (`Build*Group`) lives in Apps. Example API hosts (`Lyo.{X}.Api.Example`) live under `Examples/`.

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
| `Lyo.Api.Import*` | E | `Apps/Api/` | - | - | JSON is built into `Lyo.Api.Import`. CSV, XLSX, YAML, XML, and TOML are addon handlers. |
| `Lyo.Api.Authentication` | E | `Apps/Api/` | - | - | Auth admin QueryProject; host-supplied EndpointAuth (platform wrapper over Security). Do not rename to `Lyo.Authentication.Api`. |
| `Lyo.Avalonia.Components` | E | `Apps/Avalonia/` | - | - | Shared Avalonia controls. Tool panes dock inside a window. A window can dock to a screen edge. Desktop apps reference this the way Blazor hosts reference `Lyo.Web.Components`. |
| `Lyo.Web.*` | E | `Apps/Web/` | - | - | |
| `Lyo.Web.WebRenderer.Models` | E | `Apps/Web/Renderer/` | - | - | Serializable `WebRenderReq` / `WebRenderRes`. No Blazor, no Puppeteer. Implemented by `Lyo.Web.WebRenderer`. |
| `Lyo.Web.Automation.PuppeteerSharp` | E | `Apps/Web/Automation/` | - | - | PuppeteerSharp engine for `Lyo.Web.Automation`. Chrome, Chromium, Edge, and Firefox. No browser download. |
| `Lyo.Web.Automation.Api` | E | `Apps/Web/Automation/` | - | - | HTTP run API (`BuildAutomationGroup`). No Playwright, Selenium, or PuppeteerSharp reference. |
| `Lyo.Web.Automation.Client` | E | `Apps/Web/Automation/` | - | - | Typed HTTP client for plan runs, logs, and snapshots. |
| `Lyo.Web.Automation.Web.Components` | E | `Apps/Web/Automation/` | - | - | MudBlazor plan workbench. Takes `IAutomationPlanClient` as a parameter. |
| `Lyo.Job.Api` | E | `Apps/Job/` | - | - | HTTP mapping (`BuildJobGroup`) |
| `Lyo.Job.Client` | E | `Apps/Job/` | - | - | Typed HTTP client for workers/schedulers |
| `Lyo.Job.Worker` | E | `Apps/Job/` | - | - | |
| `Lyo.Job.Scheduler` | E | `Apps/Job/` | - | - | |
| `Lyo.Job.Alerts` | E | `Apps/Job/` | - | - | |
| `Lyo.Job.Workers.Notification` | E | `Apps/Job/` | - | - | Webhook/email/SMS workers; notifying is a workflow step |
| `Lyo.Reporting.Api` | E | `Apps/Reporting/` | - | - | HTTP (`BuildReportingGroup`); Postgres stays service-only |
| `Lyo.Reporting.Client` | E | `Apps/Reporting/` | - | - | Typed HTTP client |
| `Lyo.Drift.Api` | E | `Apps/Drift/` | - | - | HTTP ingest (`BuildDriftGroup`) |
| `Lyo.Drift.Client` | E | `Apps/Drift/` | - | - | |
| `Lyo.Drift.Agent` | E | `Apps/Drift/` | - | - | Collector process |
| `Lyo.Comic.Api` | E | `Apps/Comic/` | - | - | HTTP over `Plugins/Comic` |
| `Lyo.Algorithms.Api` | E | `Apps/Algorithms/` | - | - | Catalog HTTP (`BuildAlgorithmsGroup`). `POST Algorithms/preview` runs an unsaved draft, converts the context with the revision type binding, and returns `value` plus the breakdown. Catalog execute stays in-process |
| `Lyo.Algorithms.Client` | E | `Apps/Algorithms/` | - | - | Typed HTTP client. `PreviewAsync` is the one remote run and does not persist |
| `Lyo.Story.Api` | E | `Apps/Story/` | - | - | HTTP over `Plugins/Story`. Req/Res CRUD plus graph and play |
| `Lyo.Story.Client` | E | `Apps/Story/` | - | - | Typed HTTP client for graph and play. Query stays on `IApiClient` |
| `Lyo.FantasyFootball.Api` | E | `Apps/FantasyFootball/` | - | - | HTTP over `Plugins/FantasyFootball`. Syncs chats and trades through the ESPN client |
| `Lyo.Leaderboard.Api` | E | `Apps/Leaderboard/` | - | - | HTTP over `Plugins/Leaderboard` |
| `Lyo.Leaderboard.Api.Models` | E | `Apps/Leaderboard/` | - | - | Wire DTOs |
| `Lyo.Leaderboard.Api.Client` | E | `Apps/Leaderboard/` | - | - | Typed HTTP client |
| `Lyo.HomeInventory.Api` | E | `Apps/HomeInventory/` | - | - | HTTP over `Plugins/HomeInventory` |
| `Lyo.Recipe.Api` | E | `Apps/Recipe/` | - | - | HTTP over `Plugins/Recipe` |
| `Lyo.Recipe.Client` | E | `Apps/Recipe/` | - | - | Typed HTTP client for extra Recipe routes |
| `Lyo.Ledger.Api` | E | `Apps/Ledger/` | - | - | HTTP over `Plugins/Ledger` |
| `Lyo.Law.Api` | E | `Apps/Law/` | - | - | HTTP over `Plugins/Law`. Node body changes go through upsert |
| `Lyo.Law.Client` | E | `Apps/Law/` | - | - | Typed HTTP client for node upsert and lookup. Query stays on `IApiClient` |
| `Lyo.Billing.Api` | E | `Apps/Billing/` | - | - | HTTP over `Plugins/Billing`. No PDF |
| `Lyo.Billing.Client` | E | `Apps/Billing/` | - | - | Typed HTTP client over `Lyo.Billing.Api` |
| `Lyo.Billing.Web.Components` | E | `Apps/Billing/` | - | - | Gateway dashboard. Talks HTTP to TestApi |
| `Lyo.Payments` | E | `Apps/Payments/` | - | - | `IPaymentCollector` + internal workbench impl. No Stripe |
| `Lyo.FileManager`, `.App`, `.Tests` | E | `Apps/FileManager/` | - | - | Desktop file manager (`lyo-files`) over `Lyo.Api.FileStorage` or a local folder. The store stays in `Data/FileStorage`. |
| `Lyo.Desktop`, `.Tests` | E | `Apps/Desktop/` | - | - | Shared desktop helpers. Global hotkeys, clipboard, window list and activate, and mouse and key input. X11 on Linux, Win32 on Windows. `lyo-shot`, the automation studio, and the CLI use it. |
| `Lyo.Screenshot`, `.App`, `.Tests` | E | `Apps/Screenshot/` | - | - | Desktop capture (`lyo-shot`): still PNG, GIF, and scrolling receipt. |
| `Lyo.Config.Api*` | E | `Apps/Config/` | - | - | Product HTTP surface over `Plugins/Config`. `Lyo.Config.Api.Hosting` is the packable polling library. Example host is `Lyo.Config.Api.Example`. |

## Inventory: Examples

Runnable sample hosts. `IsPackable=false`. Prefer embedding `.Api` in a real host; these show demo wiring (OpenAPI/Scalar, local cache, anonymous auth, hardcoded keys).

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Job.Api.Example` | E | `Examples/` | - | - | Example Job API (`http://localhost:5096`) |
| `Lyo.Reporting.Api.Example` | E | `Examples/` | - | - | Example Reporting API; anonymous auth (`http://localhost:5098`) |
| `Lyo.Drift.Api.Example` | E | `Examples/` | - | - | Example Drift API (`http://localhost:5099`) |
| `Lyo.Comic.Api.Example` | E | `Examples/` | - | - | Example Comic API (`http://localhost:5101`) |
| `Lyo.HomeInventory.Api.Example` | E | `Examples/` | - | - | Example HomeInventory API (`http://localhost:5102`) |
| `Lyo.Recipe.Api.Example` | E | `Examples/` | - | - | Example Recipe API (`http://localhost:5106`) |
| `Lyo.Ledger.Api.Example` | E | `Examples/` | - | - | Example Ledger API (`http://localhost:5104`) |
| `Lyo.Billing.Api.Example` | E | `Examples/` | - | - | Example Billing API (`http://localhost:5105`); wires Ledger |
| `Lyo.Leaderboard.Api.Host` | E | `Examples/` | - | - | Standalone Leaderboard API host. Assembly name stays `.Api.Host` |
| `Lyo.Config.Api.Example` | E | `Examples/` | - | - | Example Config API (`http://localhost:5088`) |
| `Lyo.Job.Worker.Example` | E | `Examples/` | - | - | Sample worker (`job.run.example`) |
| `Lyo.Job.Scheduler.Example` | E | `Examples/` | - | - | Sample scheduler host |
| `Lyo.Drift.Agent.Example` | E | `Examples/` | - | - | Sample Drift agent host |
| `Lyo.Reporting.Business.Example` | E | `Examples/` | - | - | Sample Blazor reporting host |

## Inventory: Plugins

Drop-in modules. No `Build*Group` route mapping.

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Comment*` | E | `Plugins/Comment/` | - | - | `.Seed` EF comments |
| `Lyo.Note*` | E | `Plugins/Note/` | - | - | `.Seed` EF notes |
| `Lyo.Favorite*` | E | `Plugins/Favorite/` | - | - | `.Seed` EF favorites |
| `Lyo.Tag*` | E | `Plugins/Tag/` | - | - | `.Seed` EF tags |
| `Lyo.Rating*` | E | `Plugins/Rating/` | - | - | `.Seed` EF ratings |
| `Lyo.ContactUs*` | E | `Plugins/ContactUs/` | - | - | `.Seed` EF submissions; `.Postgres.Tests` reads |
| `Lyo.Profanity` | E | `Plugins/Profanity/` | - | - | |
| `Lyo.ShortUrl*` | E | `Plugins/ShortUrl/` | - | - | `.Seed` EF short URLs |
| `Lyo.Config`, `.Postgres`, `.Web.Components`, `.Seed` | E | `Plugins/Config/` | - | - | HTTP is `Apps/Config`. `.Seed` writes plaintext definitions |
| `Lyo.Book`, `.Postgres`, `.Postgres.Tests` | E | `Plugins/Book/` | - | - | Library catalog. TestApi CRUD/search this slice |
| `Lyo.People.Models`, `.Postgres`, `.Postgres.Tests` | E | `Plugins/People/` | - | - | Person graph. Schema `people`. References Geolocation and EntityReference. Endato maps in at the host |
| `Lyo.Music`, `.Postgres`, `.Postgres.Tests`, `.Web.Components`, `.Seed` | E | `Plugins/Audio/Music/` | - | - | Music catalog. Schema `music`. TestApi CRUD/search this slice |
| `Lyo.Audiobook`, `.Postgres`, `.Postgres.Tests`, `.Web.Components`, `.Seed` | E | `Plugins/Audio/Audiobook/` | - | - | Spoken-word catalog. Schema `audiobook`. TestApi CRUD/search this slice |
| `Lyo.Podcast`, `.Postgres`, `.Postgres.Tests`, `.Web.Components`, `.Seed` | E | `Plugins/Audio/Podcast/` | - | - | Podcast catalog. Schema `podcast`. TestApi CRUD/search this slice |
| `Lyo.Video`, `.Postgres`, `.Postgres.Tests`, `.Web.Components` | E | `Plugins/Video/` | - | - | Movie and television catalog. Schema `video`. References `Lyo.Google.Youtube.Models` |
| `Lyo.Story`, `.Postgres`, `.Tests`, `.Postgres.Tests`, `.Web.Components`, `.Web.Components.Tests` | E | `Plugins/Story/` | - | - | Play graph. HTTP is `Apps/Story`. Blazor builder is `.Web.Components` |
| `Lyo.Comic`, `.Postgres`, `.Web.Components` | E | `Plugins/Comic/` | - | - | HTTP is `Apps/Comic` |
| `Lyo.HomeInventory`, `.Models`, `.Postgres`, `.FileStorage`, `.Web.Components`, `.Seed`, `.Tests` | E | `Plugins/HomeInventory/` | - | - | HTTP is `Apps/HomeInventory`. `.Seed` writes items, stock, and movements |
| `Lyo.Recipe`, `.Models`, `.Postgres`, `.FileStorage`, `.Web.Components`, `.Seed`, `.Tests` | E | `Plugins/Recipe/` | - | - | HTTP is `Apps/Recipe`. `.Seed` writes a category, ingredients, recipes, lines, and steps |
| `Lyo.Ledger`, `.Postgres`, `.Seed` | E | `Plugins/Ledger/` | - | - | Double-entry CoA/journals. HTTP is `Apps/Ledger`. `.Seed` writes balanced journals |
| `Lyo.Law`, `.Models`, `.Postgres`, `.Postgres.Tests`, `.Web.Components` | E | `Plugins/Law/` | - | - | State statutes, any US state. Schema `law`. Jurisdiction `US` is rejected. HTTP is `Apps/Law` |
| `Lyo.FantasyFootball`, `.Postgres`, `.Postgres.Tests`, `.Web.Components`, `.Web.Components.Tests` | E | `Plugins/FantasyFootball/` | - | - | Chats and trades. Schema `fantasy_football`. HTTP is `Apps/FantasyFootball`. The plugin does not reference the ESPN client |
| `Lyo.Leaderboard`, `.Postgres`, `.Postgres.Tests`, `.Tests`, `.Web.Components` | E | `Plugins/Leaderboard/` | - | - | Boards, membership, invites, entries. Schema `leaderboard`. Entry points come from `Lyo.Algorithms`. HTTP is `Apps/Leaderboard` |
| `Lyo.Billing`, `.Postgres`, `.Seed` | E | `Plugins/Billing/` | - | - | Invoices post journals. HTTP is `Apps/Billing`. No PDF. `.Seed` writes draft invoices |
| `Lyo.Job.Models`, `.Postgres`, `.Web.Components`, `.Seed`, `.Tests` | E | `Plugins/Job/` | - | - | Store/domain. HTTP/process stay in `Apps/Job`. `.Seed` writes disabled definitions |
| `Lyo.Reporting.Models`, `.Postgres`, `.Web`, `.Web.Components`, `.Seed`, `.Tests` | E | `Plugins/Reporting/` | - | - | Store/UI. `AddReportingMaintenanceWorker` stays on Postgres. HTTP is `Apps/Reporting`. `.Seed` writes design samples |

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
| `Lyo.Email` | B | `Communication/Email/` | - | - | Provider-neutral contracts, builder, and pipeline |
| `Lyo.Email.Smtp` | B | `Communication/Email/` | - | - | MailKit SMTP provider |
| `Lyo.Email.SendGrid` | B | `Communication/Email/` | - | - | Twilio SendGrid provider. Monolithic SDK+provider |
| `Lyo.Email.Postgres` | A-like | `Communication/Email/` | - | - | |
| `Lyo.MessageQueue` | B | `Communication/MessageQueue/` | - | - | |
| `Lyo.MessageQueue.RabbitMq` | B | `Communication/MessageQueue/` | - | - | |
| `Lyo.Outbox.Dispatcher` | B | `Communication/MessageQueue/` | - | - | Publishes idempotency outbox rows through `IMqService`. At-least-once. No RabbitMQ reference. |

## Inventory: Security (capabilities)

| Project | Archetype | Path | Target | Phase | Notes |
|---------|-----------|------|--------|-------|-------|
| `Lyo.Authentication` | B | `Security/Authentication/` | - | - | |
| `Lyo.Authorization` | B | `Security/Authorization/` | - | - | Resource grants beside scope checks. Does not parse tokens. |
| `Lyo.Authorization.AspNetCore` | B | `Security/Authorization/` | - | - | `[RequireGrant]` and `.RequireGrant()`. No reference to `Lyo.Authentication`. |
| `Lyo.Authorization.Postgres` | B | `Security/Authorization/` | - | - | Schema `authorization`, table `grant`. |
| `Lyo.Authorization.Web.Components` | B | `Security/Authorization/` | - | - | In-process grant list, editor, and check form. |
| `Lyo.RequestSigning` | B | `Security/RequestSigning/` | - | - | Timestamped HMAC header. The key comes from configuration. |
| `Lyo.RequestSigning.AspNetCore` | B | `Security/RequestSigning/` | - | - | Middleware returns 401 when the signature fails. |
| `Lyo.Authentication.Google` | B | `Security/Authentication/` | - | - | Stays |
| `Lyo.Authentication.Keycloak` | B | `Security/Authentication/` | - | - | Stays |
| `Lyo.Authentication.OpenIdConnect` | B | `Security/Authentication/` | - | - | Protocol plugin. Handoff and `/auth/*` live in `Lyo.Authentication.Sso`. |
| `Lyo.Authentication.Sso` | B | `Security/Authentication/` | - | - | Shared login host: endpoints, handoff, user completion. |
| `Lyo.Authentication.Saml` | B | `Security/Authentication/` | - | - | SAML 2.0 service provider. Unsigned AuthnRequest. Encrypted assertions rejected. |
| `Lyo.Authentication.Okta` | B | `Security/Authentication/` | - | - | Okta SAML profile. Metadata key is the public `exk` id. |
| `Lyo.Authentication.Entra` | B | `Security/Authentication/` | - | - | Entra SAML profile. Federation metadata URL includes `?appid=`. |
| `Lyo.Authentication.Postgres` | A-like | `Security/Authentication/` | - | - | Identity store |
| `Lyo.Encryption` + algorithm packages | E/B | `Security/Encryption/` | - | - | |
| `Lyo.KeyStore` | E/B | `Security/KeyStore/` | - | - | |
| `Lyo.KeyStore.Aws` | B | `Security/KeyStore/` | - | - | AWS Secrets Manager provider |
| `Lyo.KeyStore.Web.Components` | B | `Security/KeyStore/` | - | - | In-process IKeyStore workbench (no HTTP) |
| `Lyo.ContentThreatScan` | B | `Security/ContentThreatScan/` | - | - | |
| `Lyo.ContentThreatScan.Intel` | B | `Security/ContentThreatScan/` | - | - | |
| `Lyo.ContentThreatScan.ClamAv` | B | `Security/ContentThreatScan/` | - | - | HTTP clamav-rest adapter over `Lyo.ClamAv.Client` |
| `Lyo.Drift.Models` | E | `Security/Drift/` | - | - | File-integrity / config-drift contracts. Not a capability+provider. Uses Hashing. |
| `Lyo.Drift.Postgres` | E | `Security/Drift/` | - | - | Store only. HTTP is `Lyo.Drift.Api`. |
| `Lyo.Drift.Web.Components` | E | `Security/Drift/` | - | - | Workbench UI |
| `Lyo.Drift.Tests` | E | `Security/Drift/` | - | - | Store + TestServer against `Lyo.Drift.Api` |

## Inventory: Core domains (reference)

| Project | Archetype | Path |
|---------|-----------|------|
| `Lyo.Geolocation.Models` | A | `Core/Geolocation/` |
| `Lyo.Idempotency` | A | `Core/Idempotency/` |
| `Lyo.Idempotency.Postgres` | A | `Core/Idempotency/` |
| `Lyo.Idempotency.AspNetCore` | A | `Core/Idempotency/` |
| `Lyo.FeatureFlags` | A | `Core/FeatureFlags/` |
| `Lyo.FeatureFlags.AspNetCore` | A | `Core/FeatureFlags/` |
| `Lyo.FeatureFlags.Web.Components` | A | `Core/FeatureFlags/` |
| `Lyo.Outbox` | A | `Core/Outbox/` |
| `Lyo.Outbox.Postgres` | A | `Core/Outbox/` |
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

Domain modules (the People plugin, Geolocation, Favorite, …) subclass these bases and own module-specific owner columns and store logic.

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
