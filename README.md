# LYO. Library for Your Organization.

This is a continual work-in-progress personal development workspace. It is also my portfolio for .NET libraries and related tooling.

This repository is a .NET-focused toolkit of libraries and apps for business data: APIs with a rich query model, durable file handling, document parsing, and cross-cutting infrastructure (security, compression, observability, and more). Most code lives under [`Lyo.Net/`](Lyo.Net/).

**Note.** Generative AI tools were used to help build and maintain parts of this codebase where scale made that practical. Notably the numerical packages **Mathematics** and **Scientific** (including their function libraries), **documentation** (including long-form package READMEs), **test** projects and libraries, and **some JavaScript** (load-testing scripts, Blazor companion scripts, other web-related assets). Human review still applies. Treat those areas with the same scrutiny you would for any large or subtle code.

---

## Major capabilities

<!-- catalog:capabilities:start -->

These are the areas that tend to anchor product work. Each links to deeper docs where they exist in-tree.

| Area                    | What it is                                                                                                                                                                                                                               | Documentation                                                                                                                                                                                                                                     |
|-------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **API & query**         | Minimal APIs and CRUD on Entity Framework Core. Typed and dynamic builders, result caching with auto-invalidation, nested WhereClause filters, projection, property-level patch, bulk with per-item fallback, and CSV/XLSX/JSON export. | [Lyo.Api](Lyo.Net/Apps/Api/Lyo.Api/README.md) · [Lyo.Query.Models](Lyo.Net/Data/Query/Lyo.Query.Models/README.md)                                                                                                                          |
| **Query client UI**     | Blazor components (for example a data grid) that speak the same query shapes as the API.                                                                                                                                                 | [Lyo.Api](Lyo.Net/Apps/Api/Lyo.Api/README.md)                                                                                                                                                                                              |
| **File storage**        | Local, S3, and Azure Blob providers share save/stream/copy/download, staged upload, multipart, duplicate detection, and an optional compress+encrypt pipeline.                                                                           | [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md) · [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md) · [Lyo.FileStorage.AzureBlob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.AzureBlob/README.md)   |
| **Cloud blob backends** | AWS S3-compatible and Azure Blob Storage implementations of the file storage abstractions.                                                                                                                                               | [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md) · [Lyo.FileStorage.AzureBlob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.AzureBlob/README.md)                                                                           |
| **PDF**                 | Load PDFs and extract text via IPdfService: words/lines, bounding boxes, key-value and table-style extraction, merges. Blazor PDF annotator in Lyo.Pdf.Web.Components.                                                                   | [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md) · [Lyo.Pdf.Web.Components](Lyo.Net/Data/Pdf/Lyo.Pdf.Web.Components/README.md)                                                                                                                       |
| **Encryption**          | Authenticated encryption (AES-GCM, ChaCha, CCM, SIV, XChaCha), RSA/hybrid, envelope/two-key, keystore integration.                                                                                                                       | [Lyo.Encryption](Lyo.Net/Security/Encryption/Lyo.Encryption/README.md) · [benchmark summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)                                                                          |
| **Caching**             | Local and Fusion-backed ICacheService, typed byte payloads, query cache tags for invalidation (with Lyo.Api).                                                                                                                            | [Lyo.Cache](Lyo.Net/Core/Cache/Lyo.Cache/README.md)                                                                                                                                                                                               |
| **Diagnostics**         | Stack decoding, exception classification, breadcrumbs, in-memory error inbox, trace sanitisation. Optional IPackageMetadataStore for namespace-to-package enrichment.                                                                    | [Lyo.Diagnostic](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic/README.md) · [Lyo.Diagnostic.AspNetCore](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.AspNetCore/README.md) · [Lyo.PackageMetadata](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata/README.md) |
| **Content threat scan** | Heuristic scoring for readable text. Optional Malware Bazaar, VirusTotal, and clamd reputation. Composes with Lyo.FileStorage malware scanning.                                                                                          | [Lyo.ContentThreatScan](Lyo.Net/Security/ContentThreatScan/Lyo.ContentThreatScan/README.md) · [Lyo.ContentThreatScan.Intel](Lyo.Net/Security/ContentThreatScan/Lyo.ContentThreatScan.Intel/README.md)                                             |
| **Hashing**             | SHA-2 digests, MD5 for non-security fingerprints, hex helpers, stream hashing, DI-friendly IHashingService.                                                                                                                              | [Lyo.Hashing](Lyo.Net/Security/Hashing/Lyo.Hashing/README.md)                                                                                                                                                                                     |
| **Compression**         | Ten codecs (LZ4, Zstd, Brotli, GZip, and others), streams/files, size limits and bomb protections.                                                                                                                                       | [Lyo.Compression](Lyo.Net/Data/Compression/Lyo.Compression/README.md) · [benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)                                                                              |

<!-- catalog:capabilities:end -->

---

## Repository layout (high level)

| Path                                                               | Comment                                                                                                                                                                                                                                                                                                                           |
|--------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [`Lyo.Net/`](Lyo.Net/)                                             | Main .NET solution root: shared props, solution file, and libraries grouped by the subfolders below.                                                                                                                                                                                                                              |
| [`Lyo.Net/Core/`](Lyo.Net/Core/)                                   | Cross-cutting primitives: caching, diagnostics, validation, metrics, resilience, exceptions, common types, package metadata for diagnostics, math/science, people models, geolocation, webhooks, locks, scheduling, streams, date/time, audit, change tracking, health. Domain-agnostic building blocks for the rest of the stack. |
| [`Lyo.Net/Data/`](Lyo.Net/Data/)                                   | Data handling and persistence helpers: file storage (local/S3/Azure Blob), compression, CSV/XLSX/PDF, images, Postgres migration helpers, `Lyo.Query.Models` shapes, QR codes, file-system watching, temporary IO, seed graphs (`Lyo.Seed`), and related parsers/processors.                                                       |
| [`Lyo.Net/Plugins/`](Lyo.Net/Plugins/)                           | Drop-in modules: contracts/models, Postgres, `.Web.Components` (comments, notes, favorites, ratings, tags, typed config, comic, home inventory, job store, reporting store/UI). Hosts map HTTP from `Apps`.                                                                                                                        |
| [`Lyo.Net/Apps/`](Lyo.Net/Apps/)                                   | HTTP and processes: shared API/query (`Lyo.Api`), Blazor web host, product `.Api` / `.Api.Host` / `.Client`, workers, schedulers, agents. Browser automation lives under [`Lyo.Web.Automation`](Lyo.Net/Apps/Web/Automation/README.md) (Selenium / Playwright, JSON plans). |
| [`Lyo.Net/Examples/`](Lyo.Net/Examples/)                           | Runnable sample hosts and sample libraries (`Lyo.Job.Worker.Example`, `Lyo.Job.Scheduler.Example`, `Lyo.Reporting.Business.Example`).                                                                                                                                                                                                                            |
| [`Lyo.Net/Integration/`](Lyo.Net/Integration/)                     | Vendor clients and product verticals (Discord, Endato, ESPN, Google, Typecast). Not the shared API/web/job stacks.                                                                                                                                                                                                                |
| [`Lyo.Net/docs/package-layout.md`](Lyo.Net/docs/package-layout.md) | Package taxonomy. Where Core domains, Communication providers, Apps platform packages, and Integration vendor clients belong (archetypes A–E).                                                                                                                                                                                    |
| [`Lyo.Net/Security/`](Lyo.Net/Security/)                           | Cryptography (`Lyo.Encryption`), hashing (`Lyo.Hashing`), authentication, content-threat heuristics (`Lyo.ContentThreatScan*`), and the Drift file-integrity store (`Security/Drift`). HTTP/agent for Drift stay under `Apps/Drift`.                                                                                                                                                                         |
| [`Lyo.Net/Communication/`](Lyo.Net/Communication/)                 | Messaging and media delivery: SMTP email, SMS (including Twilio), and text-to-speech providers.                                                                                                                                                                                                                                   |
| [`Lyo.Net/Tools/`](Lyo.Net/Tools/)                                 | Workbenches and utilities (TestGateway, TestApi, TestConsole, CLI, Postgres migrations) for trying components end-to-end.                                                                                                                                                                                                         |
| [`k6/`](k6/)                                                       | Load-testing scripts. See [k6 framework: Person Query API](k6/framework-person/README.md) and [K6 benchmark analysis](Lyo.Net/Apps/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md).                                                                                                                                                  |

Individual projects are mostly **one folder per NuGet-style package** (for example `Lyo.Something`). The sections below list **every** in-repo `README.md` beside a library, grouped by top-level area.

---

## All packages with READMEs

<!-- catalog:packages:start -->

### Communication

- [Lyo.Email](Lyo.Net/Communication/Email/Lyo.Email/README.md): MailKit SMTP sender. `EmailService` is the `IEmailService` implementation.
- [Lyo.Email.Models](Lyo.Net/Communication/Email/Lyo.Email.Models/README.md): Shared models, options, failure codes, and event payloads for the `Lyo.Email` SMTP sender.
- [Lyo.Email.Postgres](Lyo.Net/Communication/Email/Lyo.Email.Postgres/README.md): PostgreSQL EF Core store for email mailbox logs (`EmailLogEntity`). The package never sends or fetches mail. Hosts insert outbound rows after SMTP send and inbound rows after their own IMAP/POP client.
- [Lyo.Email.Web.Components](Lyo.Net/Communication/Email/Lyo.Email.Web.Components/README.md): MudBlazor workbench that sends mail through an injected `IEmailService`.
- [Lyo.MessageQueue](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue/README.md): `IMqService` defines the queue and exchange contract. Schedulers, workers, and gateways compile against one interface and swap `Lyo.MessageQueue.*` brokers behind it.
- [Lyo.MessageQueue.RabbitMq](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md): `IMqService` implementation (`RabbitMqService`) on `RabbitMQ.Client`. Also wired as `IRabbitMqService` for exchanges and other RabbitMQ-only methods.
- [Lyo.MessageQueue.RabbitMq.Web.Components](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq.Web.Components/README.md): Blazor UI for RabbitMQ exchanges, bindings, and broker workbenches.
- [Lyo.MessageQueue.Web.Components](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.Web.Components/README.md): Blazor UI for provider-neutral queue dashboards and workbenches.
- [Lyo.Sms](Lyo.Net/Communication/Sms/Lyo.Sms/README.md): Shared SMS contracts and send pipeline. Providers (`Lyo.Sms.Twilio`, and others) implement `SmsServiceBase`.
- [Lyo.Sms.Models](Lyo.Net/Communication/Sms/Lyo.Sms.Models/README.md): Shared types used by `Lyo.Sms`: payloads, paging, events, normalization, and base options. The package never sends SMS. Implementations live in provider packages (`Lyo.Sms.Twilio`, and others).
- [Lyo.Sms.Postgres](Lyo.Net/Communication/Sms/Lyo.Sms.Postgres/README.md): PostgreSQL EF Core store for outbound SMS logs (`SmsLogEntity`). The package never sends SMS. It wires `SmsDbContext` so workers or gateways can store send outcomes.
- [Lyo.Sms.Twilio](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio/README.md): Twilio SMS and MMS via `Lyo.Sms`. `TwilioSmsService` implements `ISmsService`.
- [Lyo.Sms.Twilio.Postgres](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio.Postgres/README.md): PostgreSQL EF Core store for Twilio SMS traces: `TwilioSmsDbContext` and `TwilioSmsLogEntity`.
- [Lyo.Sms.Web.Components](Lyo.Net/Communication/Sms/Lyo.Sms.Web.Components/README.md): MudBlazor workbench for an injected `ISmsService`. Uses MudBlazor and snackbar helpers in `Lyo.Web.Components`.
- [Lyo.Stt](Lyo.Net/Communication/Speech/Lyo.Stt/README.md): Lyo speech-to-text contract. Ships `ISttService`, `SttServiceBase`, request/result/options/event records, and metric name constants. This repo ships no provider packages.
- [Lyo.Translation](Lyo.Net/Communication/Translation/Lyo.Translation/README.md): **Archetype B (capability).** Providers (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) remain under `Communication/Translation/`, not under `Integration/`. See package layout.
- [Lyo.Translation.Aws](Lyo.Net/Communication/Translation/Lyo.Translation.Aws/README.md): Amazon Translate that implements `ITranslationService`. Translates text, runs bounded bulk translation, infers language via a Translate call, and probes connectivity with `ListLanguages`.
- [Lyo.Translation.Google](Lyo.Net/Communication/Translation/Lyo.Translation.Google/README.md): Google Cloud Translation v2 that implements `ITranslationService`. `GoogleTranslationService` extends `TranslationServiceBase` and calls the REST API over HTTP.
- [Lyo.Translation.Web.Components](Lyo.Net/Communication/Translation/Lyo.Translation.Web.Components/README.md): MudBlazor workbench for whichever `Lyo.Translation` implementation the host wired.
- [Lyo.Tts](Lyo.Net/Communication/Speech/Lyo.Tts/README.md): TTS contracts and shared behavior: provider-agnostic interfaces, a non-generic facade, and a base service with bulk synthesis, metrics, and lifecycle events.
- [Lyo.Tts.AwsPolly](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly/README.md): TTS via Amazon Polly. `AwsPollyTtsService` extends `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk synthesis, metrics, and DI helpers.
- [Lyo.Tts.AwsPolly.Web.Components](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly.Web.Components/README.md): MudBlazor workbench for exercising `Lyo.Tts.AwsPolly` inside a host app.
- [Lyo.Tts.Models](Lyo.Net/Communication/Speech/Lyo.Tts.Models/README.md): Shared TTS requests, results, options, and event payloads. Provider packages depend on this package instead of on each other.
- [Lyo.Tts.Typecast](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast/README.md): Typecast TTS through `Lyo.Typecast.Client`. `TypecastTtsService` synthesizes audio through `TypecastClient`, can load the voice catalog for validation (`LoadVoicesAsync`), and uses the bulk pipeline plus Typecast-namespaced metrics from `Lyo.Tts`.
- [Lyo.Tts.Typecast.Web.Components](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast.Web.Components/README.md): MudBlazor workbench for exercising `Lyo.Tts.Typecast` inside a host app.
- [Lyo.Tts.WindowsSpeech](Lyo.Net/Communication/Speech/Lyo.Tts.WindowsSpeech/README.md): Text-to-speech through Windows SAPI. `WindowsSpeechTtsService` uses the built-in Speech API.
- [Lyo.Webhook](Lyo.Net/Communication/Webhook/Lyo.Webhook/README.md): ASP.NET Core inbound webhook verification: headers and raw body, HMAC helpers, `MapWebhook().Verify().Handle()`, and `Lyo.Metrics` timings.
- [Lyo.Webhook.Twilio](Lyo.Net/Communication/Webhook/Lyo.Webhook.Twilio/README.md): Twilio webhook signature checks for `Lyo.Webhook`. Compares `X-Twilio-Signature` to an HMAC-SHA1 (Base64) of the public request URL plus sorted key+value form parameters.

### Core

- [Lyo.Audit](Lyo.Net/Core/Audit/Lyo.Audit/README.md): Audit trail with two records: `AuditChange` (entity change tracking) and `AuditEvent` (events to log).
- [Lyo.Audit.Postgres](Lyo.Net/Core/Audit/Lyo.Audit.Postgres/README.md): EF Core persistence for Lyo.Audit in PostgreSQL. Stores `AuditChange` and `AuditEvent` rows with JSONB columns for dictionary data.
- [Lyo.Benchmark](Lyo.Net/Core/Benchmark/Lyo.Benchmark/README.md): Helpers used only by `*.Benchmarks` executables. The BenchmarkDotNet counterpart to `Lyo.Testing`.
- [Lyo.Benchmark.Models](Lyo.Net/Core/Benchmark/Lyo.Benchmark.Models/README.md): Builders and models for the Lyo benchmark report schema (`lyo.bench/v1`).
- [Lyo.Cache](Lyo.Net/Core/Cache/Lyo.Cache/README.md): In-process `ICacheService` plus typed byte-payload methods. Serialize once, store framed bytes, and optionally compress or encrypt on .NET 10+.
- [Lyo.Cache.Fusion](Lyo.Net/Core/Cache/Lyo.Cache.Fusion/README.md): `ZiggyCreatures.FusionCache` is adapted to `ICacheService` by `FusionCacheService` so `Lyo.Api`, workers, and feature modules can swap in-memory `Lyo.Cache` for Fusion plus an optional Redis backplane without rewriting call sites.
- [Lyo.ChangeTracker](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker/README.md): Generic entity change history around `Lyo.EntityReference.Models.EntityRef`. Record property-level changes for any entity type without tying the tracker to one aggregate.
- [Lyo.ChangeTracker.Postgres](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker.Postgres/README.md): PostgreSQL adapter for `Lyo.ChangeTracker`. Stores entity-scoped change history using `Lyo.EntityReference.Models.EntityRef` for both the target entity and the optional actor.
- [Lyo.Common.Core](Lyo.Net/Core/Common/Lyo.Common.Core/README.md): Bottom of the Lyo stack: enums, identifier generators, scalar and string extensions, type conversion, path helpers, secure randomness, and HTTP constants, with no Lyo dependency beyond `Lyo.Exceptions`.
- [Lyo.Common.Json](Lyo.Net/Core/Common/Lyo.Common.Json/README.md): `LyoJsonSerializerOptions` — the HTTP JSON contract every Lyo API and first-party client shares — plus opt-in converters for upstreams that quote their scalars.
- [Lyo.Common.Metadata](Lyo.Net/Core/Common/Lyo.Common.Metadata/README.md): Lyo registry records — file type, MIME, language, port, HTTP status, HTTP header, geography, and CLR type catalogs — plus the extensions that bridge enums to those records.
- [Lyo.DateAndTime](Lyo.Net/Core/DateAndTime/Lyo.DateAndTime/README.md): Dates, times, US timezone conversion, day-of-week scheduling, and US holiday metadata. Static and thread-safe. No mutable shared state.
- [Lyo.Diagnostic](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic/README.md): Decode stack traces, classify exceptions, keep breadcrumb trails, hold an in-memory error inbox, sanitise output, and write structured logs.
- [Lyo.Diagnostic.AspNetCore](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.AspNetCore/README.md): ASP.NET Core integration for `Lyo.Diagnostic`. Per-request scoped breadcrumb trails and exception recording to the in-memory error inbox plus structured logging, without replacing existing problem-details middleware.
- [Lyo.Diagnostic.Web.Components](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.Web.Components/README.md): Blazor (Server / Interactive) workbench that uses `Lyo.Diagnostic` to analyze and triage .NET stack traces and exception payloads.
- [Lyo.Diff](Lyo.Net/Core/Diff/Lyo.Diff/README.md): Compare human-readable text and object graphs side by side.
- [Lyo.EntityReference.Models](Lyo.Net/Core/EntityReference/Lyo.EntityReference.Models/README.md): Typed pair of logical entity kind (`EntityType`) and identifier string (`EntityId`), with helpers for composite keys, JSON, opaque tokens, validation, and domain row shapes.
- [Lyo.EntityReference.Postgres](Lyo.Net/Core/EntityReference/Lyo.EntityReference.Postgres/README.md): EF Core building blocks on PostgreSQL for relation rows (subject/actor associations) and source-link rows (import provenance).
- [Lyo.Exceptions](Lyo.Net/Core/Exceptions/Lyo.Exceptions/README.md): Exception types and argument-validation helpers shared by Lyo packages.
- [Lyo.Geolocation](Lyo.Net/Core/Geolocation/Lyo.Geolocation/README.md): Geospatial operations and persistence contracts that do not pick a vendor.
- [Lyo.Geolocation.Models](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Models/README.md): Vendor-neutral data contracts used by `Lyo.Geolocation` and `Lyo.Geolocation.Postgres`.
- [Lyo.Geolocation.Postgres](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Postgres/README.md): Entity Framework Core persistence of canonical geolocation data in PostgreSQL.
- [Lyo.Health](Lyo.Net/Core/Health/Lyo.Health/README.md): Contract for services that report their own health. Implement `IHealth`. There is no central health service.
- [Lyo.Lock](Lyo.Net/Core/Lock/Lyo.Lock/README.md): Exclusive locks by key and keyed semaphores (bounded concurrency per key), plus in-memory implementations for a single process.
- [Lyo.Lock.Redis](Lyo.Net/Core/Lock/Lyo.Lock.Redis/README.md): Redis-backed `ILockService` through StackExchange.Redis. Reach for this when several app instances must exclude each other on one logical key.
- [Lyo.Mathematics](Lyo.Net/Core/Mathematics/Lyo.Mathematics/README.md): C# contracts for the Lyo math stack: physical quantities as structs, 2D/3D vectors and small matrices, typed inputs/results for formulas, and a small registry so they can be discovered.
- [Lyo.Metrics](Lyo.Net/Core/Metrics/Lyo.Metrics/README.md): Thread-safe counters, gauges, histograms, timings, errors, and events — in-memory, OpenTelemetry, and null implementations.
- [Lyo.Metrics.DependencyInjection](Lyo.Net/Core/Metrics/Lyo.Metrics.DependencyInjection/README.md): Service-collection registration for Lyo.Metrics, split out so the metrics library itself has no NuGet dependencies.
- [Lyo.Metrics.OpenTelemetry](Lyo.Net/Core/Metrics/Lyo.Metrics.OpenTelemetry/README.md): `IMetrics` implementation that exports through OpenTelemetry to OpenTelemetry-compatible backends.
- [Lyo.Metrics.Statistics](Lyo.Net/Core/Metrics/Lyo.Metrics.Statistics/README.md): Histogram statistics on top of `Lyo.Metrics`: percentile, quartile, moving-average, and anomaly-detection helpers.
- [Lyo.Notification](Lyo.Net/Core/Notification/Lyo.Notification/README.md): Small domain events via in-process publish/subscribe. Not distributed, not durable, and not ordered across machines. Only useful when every publisher and handler lives in the same process.
- [Lyo.PackageMetadata](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata/README.md): `PackageMetadata` rows across ecosystems, `PackageMetadataRegistration`, `IPackageMetadataStore`, and `PackageArtifactDigest` helpers that correlate stack-trace namespaces with persisted catalog data.
- [Lyo.PackageMetadata.Postgres](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata.Postgres/README.md): EF Core store behind `Lyo.PackageMetadata.IPackageMetadataStore`.
- [Lyo.Parameters](Lyo.Net/Core/Parameters/Lyo.Parameters/README.md): Keyed parameter contracts and the parameter-definition model shared by jobs, reports, and query templates.
- [Lyo.People.Models](Lyo.Net/Core/People/Lyo.People.Models/README.md): People-domain records: `Person`, contact, employment, identification, and relationships.
- [Lyo.People.Postgres](Lyo.Net/Core/People/Lyo.People.Postgres/README.md): EF Core persistence of Lyo.People.Models in PostgreSQL.
- [Lyo.Privacy](Lyo.Net/Core/Privacy/Lyo.Privacy/README.md): Masks emails, phones, Luhn card numbers, IBAN, secrets, IDs, URLs, IPs, and street lines in free text, JSON, and XML.
- [Lyo.Privacy.AspNetCore](Lyo.Net/Core/Privacy/Lyo.Privacy.AspNetCore/README.md): DI wiring for `Lyo.Privacy` on ASP.NET Core: registers `ITextRedactor` / `IStructuredRedactor`, binds `PrivacyRedactorOptions` from configuration, and allows keyed per-tenant or per-feature…
- [Lyo.Privacy.Web.Components](Lyo.Net/Core/Privacy/Lyo.Privacy.Web.Components/README.md): Blazor (Server / Interactive) workbench components for `Lyo.Privacy`. Operators can preview, compare, and tune redaction policies without a host config round-trip.
- [Lyo.Resilience](Lyo.Net/Core/Resilience/Lyo.Resilience/README.md): Thin Polly wrapper for resilience pipelines, with appsettings binding and built-in logging.
- [Lyo.Result](Lyo.Net/Core/Result/Lyo.Result/README.md): `Result` / `Result<T>` with `Error` graphs, builders, bulk/paged envelopes, and `Task` composition. Distinct from `Lyo.Common.Core` `Result`.
- [Lyo.Schedule.Models](Lyo.Net/Core/Schedule/Lyo.Schedule.Models/README.md): Schedule-only DTOs. `Lyo.Scheduler`, `Lyo.Job.Postgres`, and other callers use this when they need a transport-friendly answer to "when does this run".
- [Lyo.Schedule.Web.Components](Lyo.Net/Core/Schedule/Lyo.Schedule.Web.Components/README.md): Blazor component(s) for interactively building and previewing `Lyo.Schedule.Models.ScheduleDefinition` values.
- [Lyo.Scheduler](Lyo.Net/Core/Scheduler/Lyo.Scheduler/README.md): In-process scheduler that runs actions at scheduled times. Supports **SetTimes**, **Interval**, **OneShot**, and **Cron** schedules (5- or 6-field expressions) with logging, metrics, and…
- [Lyo.Scheduler.Cache](Lyo.Net/Core/Scheduler/Lyo.Scheduler.Cache/README.md): Cache-backed `ISchedulerStateStore` for `Lyo.Scheduler`. Writes each schedule's `LastRunUtc` / `NextRunUtc` / state markers through `Lyo.Cache` so cron/interval/one-shot schedules survive process…
- [Lyo.Scientific](Lyo.Net/Core/Scientific/Lyo.Scientific/README.md): Scientific domain models, reference datasets, SI-oriented unit helpers, and formula discovery on top of `Lyo.Mathematics`.
- [Lyo.Streams](Lyo.Net/Core/Streams/Lyo.Streams/README.md): `TeeStream`, `CountingStream`, `ProgressStream`, `ConcatenatedStream`, and related wrappers. Incremental hashing lives in `Lyo.Hashing` (`HashingStream`).
- [Lyo.SystemInformation](Lyo.Net/Core/SystemInformation/Lyo.SystemInformation/README.md): Inventory of the host machine — hardware, software, drives, network interfaces, and monitors (including EDID parsing) — plus structured logging of that snapshot.
- [Lyo.Testing](Lyo.Net/Core/Testing/Lyo.Testing/README.md): Helpers for xUnit v3: fluent `Should*` asserts, exception and collection helpers, polling checks, and a logger backed by `ITestOutputHelper`.
- [Lyo.Testing.Containers](Lyo.Net/Core/Testing/Lyo.Testing.Containers/README.md): Testcontainers fixtures for xUnit v3 covering Redis, RabbitMQ, and PostgreSQL, plus a DI-wired Postgres service fixture.
- [Lyo.TextEncoding](Lyo.Net/Core/Common/Lyo.TextEncoding/README.md): Binary codecs (Base64 / Base64Url / Hex) plus charset encode/decode/convert with CodePages, detection, PEM/MIME, and injectable services.
- [Lyo.Validation](Lyo.Net/Core/Validation/Lyo.Validation/README.md): C# validators, fluent rule builders, validation attributes, and `WhereClause` schemas that surface failures as `Lyo.Result.Result<T>`.
- [Lyo.Validation.Models](Lyo.Net/Core/Validation/Lyo.Validation.Models/README.md): Contracts for validation plus the data-driven rule model: schemas, rules, messages, and projecting a where-clause into `Error`.
- [Lyo.Validation.Postgres](Lyo.Net/Core/Validation/Lyo.Validation.Postgres/README.md): PostgreSQL store for `ValidationSchema` documents (WhereClause JSONB) behind `IValidationSchemaStore`.
- [Lyo.Web.Primitives](Lyo.Net/Core/Web/Lyo.Web.Primitives/README.md): Shared MudBlazor UI primitives for every Lyo component package, with no query, data-grid, or API dependencies.

### Data

- [Lyo.Barcode](Lyo.Net/Data/Barcode/Lyo.Barcode/README.md): Contracts for generating and decoding barcodes: IBarcodeService, request and options models, plus BarcodeBuilder.
- [Lyo.Barcode.Native](Lyo.Net/Data/Barcode/Lyo.Barcode.Native/README.md): Lyo.Barcode `IBarcodeService` with no third-party barcode generator.
- [Lyo.Barcode.TestWorkbench.Web.Components](Lyo.Net/Data/Barcode/Lyo.Barcode.TestWorkbench.Web.Components/README.md): MudBlazor page that puts <BarcodeWorkbench /> from Lyo.Barcode.Web.Components in a MudContainer for the Lyo gateway test harness.
- [Lyo.Barcode.Web.Components](Lyo.Net/Data/Barcode/Lyo.Barcode.Web.Components/README.md): MudBlazor UI that talks to Lyo.Barcode's IBarcodeService.
- [Lyo.Compression](Lyo.Net/Data/Compression/Lyo.Compression/README.md): Compress and decompress bytes, strings, streams, and files with ICompressionService. One default codec, plus ICompressionResolver for per-algorithm dispatch.
- [Lyo.Compression.BZip2](Lyo.Net/Data/Compression/Lyo.Compression.BZip2/README.md): Addon that plugs BZip2 into `Lyo.Compression` by registering a BZip2 `ICompressorFactory`.
- [Lyo.Compression.Lz4](Lyo.Net/Data/Compression/Lyo.Compression.Lz4/README.md): `Lyo.Compression` addon that registers an `LZ4` `ICompressorFactory` using `EasyCompressor.LZ4`.
- [Lyo.Compression.Lzma](Lyo.Net/Data/Compression/Lyo.Compression.Lzma/README.md): LZMA plugin for `Lyo.Compression` that registers an LZMA `ICompressorFactory`.
- [Lyo.Compression.Snappier](Lyo.Net/Data/Compression/Lyo.Compression.Snappier/README.md): Snappy support for `Lyo.Compression` via a Snappier `ICompressorFactory`.
- [Lyo.Compression.Xz](Lyo.Net/Data/Compression/Lyo.Compression.Xz/README.md): XZ / LZMA2 support for `Lyo.Compression` through an XZ `ICompressorFactory`.
- [Lyo.Compression.Zstd](Lyo.Net/Data/Compression/Lyo.Compression.Zstd/README.md): Zstandard addon for `Lyo.Compression` that registers a Zstd `ICompressorFactory`.
- [Lyo.Csv](Lyo.Net/Data/Csv/Lyo.Csv/README.md): In-house CSV stack for Lyo.Csv.Models. CsvService composes a CsvWriter and CsvReader over an internal tokenizer/writer with typed binders. No third-party CSV library.
- [Lyo.Csv.Models](Lyo.Net/Data/Csv/Lyo.Csv.Models/README.md): CSV stack contracts and value types. Depend on ICsvService, ICsvReader, and ICsvWriter here; Lyo.Csv is the implementation package.
- [Lyo.DataTable](Lyo.Net/Data/DataTable/Lyo.DataTable/README.md): Placeholder package that holds the `Lyo.DataTable` name only. Runtime types (`DataTable`, `DataTableRow`, `DataTableBuilder`, cell types, HTML renderer) ship in `Lyo.DataTable.Models`.
- [Lyo.DataTable.Models](Lyo.Net/Data/DataTable/Lyo.DataTable.Models/README.md): In-memory mutable table: sparse columns, thin cells, optional format map, fluent builders, HTML renderer.
- [Lyo.FFmpeg](Lyo.Net/Data/Media/Lyo.FFmpeg/README.md): CliWrap wrapper around ffmpeg, ffprobe, and ffplay for convert, play, and stream of audio and video.
- [Lyo.FFmpeg.Models](Lyo.Net/Data/Media/Lyo.FFmpeg.Models/README.md): FFmpeg host options and command-line model for Lyo.FFmpeg.
- [Lyo.FileMetadataStore](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore/README.md): File identity without the bytes. Canonical Guid identifiers and metadata, not blob I/O.
- [Lyo.FileMetadataStore.Postgres](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md): Postgres IFileMetadataStore plus the adjunct stores richer file pipelines use.
- [Lyo.FileMetadataStore.Sqlite](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore.Sqlite/README.md): EF Core SQLite IFileMetadataStore. Same store and adjunct services as Lyo.FileMetadataStore.Postgres, for embedded, offline-first, and local-dev hosts.
- [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md): Save, stream-save, read, delete, and file metadata. Optional compression (Lyo.Compression), two-key encryption (Lyo.Encryption), duplicate hashing, access policies, audit hooks, multipart (IMultipartUploadService), and presigned/direct-upload/copy on cloud backends.
- [Lyo.FileStorage.AzureBlob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.AzureBlob/README.md): Azure.Storage.Blobs IFileStorageService for Azure Blob Storage.
- [Lyo.FileStorage.Ftp](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Ftp/README.md): `IFileStorageService` on FTP through Lyo.Ftp.Client.
- [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md): Lyo.FileStorage on S3-compatible endpoints (AWS S3, Backblaze B2, MinIO, and others) through AWSSDK.S3.
- [Lyo.FileStorage.Sftp](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Sftp/README.md): `IFileStorageService` on SFTP through Lyo.Sftp.Client.
- [Lyo.FileStorage.Web.Components](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Web.Components/README.md): Blazor Server / Interactive UI for Lyo.FileStorage. Trees, grids, and dialogs for file metadata, expected storage keys, download access links, and DEK migrate/rotate.
- [Lyo.FileSystemWatcher](Lyo.Net/Data/FileSystemWatcher/Lyo.FileSystemWatcher/README.md): Snapshot-based .NET file watcher. Finds creates, deletes, changes, moves, and renames with debounce and SHA256 hashing.
- [Lyo.FileSystemWatcher.Models](Lyo.Net/Data/FileSystemWatcher/Lyo.FileSystemWatcher.Models/README.md): Persistable file-system snapshot, change, and watch-options DTOs, plus DTO-native change detection.
- [Lyo.FileSystemWatcher.Postgres](Lyo.Net/Data/FileSystemWatcher/Lyo.FileSystemWatcher.Postgres/README.md): PostgreSQL store for FileSystemWatcher snapshots and change events. Service layer only — no HTTP.
- [Lyo.Formatter](Lyo.Net/Data/Formatter/Lyo.Formatter/README.md): SmartFormat.NET templates plus C#-like `{...}` expressions (DateTime, ternary, in-memory LINQ) for user-defined strings.
- [Lyo.Formatter.Web.Components](Lyo.Net/Data/Formatter/Lyo.Formatter.Web.Components/README.md): Blazor pair for live SmartFormat editing: a debounced template box and an annotated preview that color-links `{keys}` to replacements. Runs on WASM.
- [Lyo.Ftp.Client](Lyo.Net/Data/Ftp/Lyo.Ftp.Client/README.md): FluentFTP client with a connection pool, PathHelpers jail, logging, and Lyo.Metrics. Prefer `*Async`.
- [Lyo.IO.FileSystem](Lyo.Net/Data/IO/Lyo.IO.FileSystem/README.md): Path-rooted virtual file system: one contract for local disk, memory, SFTP, FTP, S3, and Azure Blob.
- [Lyo.IO.Temp](Lyo.Net/Data/IO/Temp/Lyo.IO.Temp/README.md): Session-scoped temp files and directories, with naming strategies and overflow policies.
- [Lyo.Images](Lyo.Net/Data/Images/Lyo.Images/README.md): SixLabors.ImageSharp raster processing for .NET.
- [Lyo.Images.Ocr](Lyo.Net/Data/Images/Lyo.Images.Ocr/README.md): Lyo OCR contracts: `IOcrEngine`, request/response models, Y-up pixel boxes (same as `BoundingBox2D`), coordinate helpers, and shared options.
- [Lyo.Images.Ocr.Tesseract](Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract/README.md): Tesseract `IOcrEngine` for `Lyo.Images.Ocr`. An internal lock serializes calls because native Tesseract instances are not safely concurrent.
- [Lyo.Images.OpenCv](Lyo.Net/Data/Images/Lyo.Images.OpenCv/README.md): OpenCvSharp4 helpers for .NET. Split from higher-level pipelines (e.g. comic overlay) so hosts take native OpenCV only where they need it.
- [Lyo.Images.Skia](Lyo.Net/Data/Images/Lyo.Images.Skia/README.md): SkiaSharp `IImageService` for `Lyo.Images`: resize, crop, rotate, watermark, convert, thumbnails, compression, metadata, palette, batch.
- [Lyo.Images.Web.Components](Lyo.Net/Data/Images/Lyo.Images.Web.Components/README.md): Blazor / MudBlazor workbenches for `Lyo.Images`: `IImageService` tools plus a spritesheet animator/extractor on `ISpriteSheetExportService`.
- [Lyo.Media.Models](Lyo.Net/Data/Media/Lyo.Media.Models/README.md): Sibling IAudio* and IVideo* contracts, encoder catalogs, and conversion/play/probe records. Images stay IImageService.
- [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md): Read with PdfPig and edit with PDFsharp for `Lyo.Pdf.Models`. Start at `PdfService`: disposable `IPdfReader` for extract work, `IPdfWriter` for structural edits.
- [Lyo.Pdf.Models](Lyo.Net/Data/Pdf/Lyo.Pdf.Models/README.md): PDF stack contracts and value types. Depend on `IPdfService`, `IPdfReader`, `IPdfWriter`, and `ITextExtractor` here; `Lyo.Pdf` is the PdfPig/PDFsharp implementation.
- [Lyo.Pdf.Ocr](Lyo.Net/Data/Pdf/Lyo.Pdf.Ocr/README.md): PNG-renders a PDF page with `Lyo.Pdf.Rendering`, runs `IOcrEngine`, then lifts OCR pixel boxes into PDF points.
- [Lyo.Pdf.Rendering](Lyo.Net/Data/Pdf/Lyo.Pdf.Rendering/README.md): Turns PDF pages into PNG through PDFtoImage (PDFium + Skia; `bblanchon.PDFium` native packages). Targets `net10.0`.
- [Lyo.Pdf.Web.Components](Lyo.Net/Data/Pdf/Lyo.Pdf.Web.Components/README.md): Blazor / MudBlazor PDF workbenches: HTML to PDF, annotation, and `LyoPdfAnnotator` so drawn regions emit `PdfBoundingBox`.
- [Lyo.Postgres](Lyo.Net/Data/Postgres/Lyo.Postgres/README.md): Shared PostgreSQL host bits for Lyo libraries that own an EF Core schema (Audit, Email, ChangeTracker, EntityReference, etc.): migrations, schema/option building, design-time factories, and health checks.
- [Lyo.QRCode](Lyo.Net/Data/QRCode/Lyo.QRCode/README.md): Generate and read QR codes: `IQRCodeService`, `QRCodeBuilder`, in-box ISO Model 2 encoding (`BuiltInQRCodeService`), optional QRCoder adapter.
- [Lyo.QRCode.QRCoder](Lyo.Net/Data/QRCode/Lyo.QRCode.QRCoder/README.md): `IQRCodeService` from `Lyo.QRCode` implemented with QRCoder. Pick this for JPEG / Bitmap on Windows, or for QRCoder's renderers.
- [Lyo.QRCode.Web.Components](Lyo.Net/Data/QRCode/Lyo.QRCode.Web.Components/README.md): Blazor / MudBlazor UI that generates and previews QR codes.
- [Lyo.Query](Lyo.Net/Data/Query/Lyo.Query/README.md): Turn a WhereClause AST into LINQ on IQueryable: filter, multi-key sort, in-memory match/explain, with ICache-backed compiled predicates.
- [Lyo.Query.Evaluation](Lyo.Net/Data/Query/Lyo.Query.Evaluation/README.md): Evaluate where clauses without a cache: build expressions, match in memory, explain matches, and resolve property paths.
- [Lyo.Query.Models](Lyo.Net/Data/Query/Lyo.Query.Models/README.md): Filter / sort / projection DTOs and fluent builders (`WhereClause`, QueryConcrete / QueryProject / root Query) that Lyo.Query and Lyo.Api share.
- [Lyo.Query.Web.Components](Lyo.Net/Data/Query/Lyo.Query.Web.Components/README.md): Blazor / MudBlazor UI that edits and runs `Lyo.Query.Models` requests against any Lyo.Api host.
- [Lyo.Seed](Lyo.Net/Data/Seed/Lyo.Seed/README.md): Seeder that does not care how items are built: contributors yield them, then EF or Lyo.Api bulk-create writes them.
- [Lyo.Sftp.Client](Lyo.Net/Data/Sftp/Lyo.Sftp.Client/README.md): SSH.NET SFTP client with a connection pool, PathHelpers jail, logging, and Lyo.Metrics. Prefer `*Async`.
- [Lyo.Sqlite](Lyo.Net/Data/Sqlite/Lyo.Sqlite/README.md): Common SQLite migration host bits for Lyo libraries that own an EF Core schema.
- [Lyo.Xlsx](Lyo.Net/Data/Xlsx/Lyo.Xlsx/README.md): `Lyo.Xlsx.Models` implementation. `XlsxService` composes an `XlsxWriter` (streaming `DocumentFormat.OpenXml`) and an `XlsxReader` (ExcelDataReader / ClosedXML).
- [Lyo.Xlsx.Models](Lyo.Net/Data/Xlsx/Lyo.Xlsx.Models/README.md): XLSX stack contracts and value types. Depend on `IXlsxService` / `IXlsxReader` / `IXlsxWriter` here; `Lyo.Xlsx` is the ClosedXML implementation.

### Plugins

- [Lyo.Comic](Lyo.Net/Plugins/Comic/Lyo.Comic/README.md): Domain contracts for a serialized fiction catalog: series (`ComicSeries`, `ComicAlternateTitle`), hierarchy (`ComicVolume`, `ComicChapter`, `ComicPage`), cast (`ComicCharacter`), `ComicSeriesQuery`, `ComicType`/`ComicStatus`, and `IComicStore`.
- [Lyo.Comic.Postgres](Lyo.Net/Plugins/Comic/Lyo.Comic.Postgres/README.md): PostgreSQL and EF Core implementation of `Lyo.Comic.IComicStore` (`PostgresComicStore`) via `ComicDbContext` and `PostgresComicOptions`.
- [Lyo.Comic.Web.Components](Lyo.Net/Plugins/Comic/Lyo.Comic.Web.Components/README.md): Blazor components for browsing, previewing, and reading comic series. Search panel, result grids and lists, browse cards, and a MangaFire-style tap-to-navigate reader.
- [Lyo.Comment](Lyo.Net/Plugins/Comment/Lyo.Comment/README.md): Contracts for threaded, reactable comments on any entity. Each comment has a **subject**, an **actor**, optional `ReplyToCommentId`, and cached like/dislike counters.
- [Lyo.Comment.Postgres](Lyo.Net/Plugins/Comment/Lyo.Comment.Postgres/README.md): Entity Framework Core store for `Lyo.Comment` on PostgreSQL. Comments live in `comment.comment` and reactions in `comment.comment_reaction`.
- [Lyo.Config](Lyo.Net/Plugins/Config/Lyo.Config/README.md): Typed, definition-driven configuration for per-entity values (a Discord guild, a tenant). The abstract API lives here. PostgreSQL persistence is in `Lyo.Config.Postgres`.
- [Lyo.Config.Postgres](Lyo.Net/Plugins/Config/Lyo.Config.Postgres/README.md): PostgreSQL and EF Core implementation of `Lyo.Config.IConfigStore` for typed configuration definitions and per-entity bindings.
- [Lyo.Config.Web.Components](Lyo.Net/Plugins/Config/Lyo.Config.Web.Components/README.md): Blazor / MudBlazor dashboard for Lyo.Config. Add ConfigManagement to a host page for definitions, resolved bindings, and two first-class histories against IConfigStore.
- [Lyo.ContactUs](Lyo.Net/Plugins/ContactUs/Lyo.ContactUs/README.md): Contact-form submission contracts. `IContactUsService` and `ContactUsServiceBase` cover validation, error-code mapping, and logging. Storage sits in sibling packages.
- [Lyo.ContactUs.Postgres](Lyo.Net/Plugins/ContactUs/Lyo.ContactUs.Postgres/README.md): PostgreSQL and EF Core implementation of `Lyo.ContactUs.IContactUsService` (`PostgresContactUsService`) through `ContactUsDbContext` and `PostgresContactUsOptions`.
- [Lyo.Favorite](Lyo.Net/Plugins/Favorite/Lyo.Favorite/README.md): Contracts for "X favorited Y" ties between any two entities. The boundary takes `EntityRef`.
- [Lyo.Favorite.Postgres](Lyo.Net/Plugins/Favorite/Lyo.Favorite.Postgres/README.md): Entity Framework Core store for `Lyo.Favorite` on PostgreSQL. Rows live in `favorite.favorite` (`PostgresFavoriteOptions.Schema = "favorite"`).
- [Lyo.HomeInventory](Lyo.Net/Plugins/HomeInventory/Lyo.HomeInventory/README.md): Contract for household inventory: large purchases (electronics, appliances) with warranty tracking, kitchen consumables across pantries / freezers, and garage bin locations.
- [Lyo.HomeInventory.Postgres](Lyo.Net/Plugins/HomeInventory/Lyo.HomeInventory.Postgres/README.md): EF Core implementation of `IHomeInventoryStore` backed by PostgreSQL.
- [Lyo.Job.Models](Lyo.Net/Plugins/Job/Lyo.Job.Models/README.md): Shared DTOs, builders, enums, metrics constants, distributed-tracing helpers, and message-queue contracts for Lyo job management.
- [Lyo.Job.Postgres](Lyo.Net/Plugins/Job/Lyo.Job.Postgres/README.md): PostgreSQL persistence for Lyo job management.
- [Lyo.Job.Web.Components](Lyo.Net/Plugins/Job/Lyo.Job.Web.Components/README.md): Blazor / MudBlazor dashboard for the Lyo job stack. Drop `JobManagement` on a host page for Statistics, Definitions, Schedules, Runs (progress and SLA breach), worker registry, and workflow views.
- [Lyo.Note](Lyo.Net/Plugins/Note/Lyo.Note/README.md): Contracts for notes hung on entities. A note has a **subject** (what it is about) and an **actor** (who wrote it), both as `EntityRef`.
- [Lyo.Note.Postgres](Lyo.Net/Plugins/Note/Lyo.Note.Postgres/README.md): Entity Framework Core store for `Lyo.Note` on PostgreSQL. Rows live in `note.note` (`PostgresNoteOptions.Schema = "note"`), and the package includes migrations.
- [Lyo.Profanity](Lyo.Net/Plugins/Profanity/Lyo.Profanity/README.md): File-based profanity filter. Finds and replaces profane words. Several languages, regex patterns, plain word lists, and configurable replacement strategies.
- [Lyo.Rating](Lyo.Net/Plugins/Rating/Lyo.Rating/README.md): Contracts for rating and reviewing entities, plus like/dislike reactions on those ratings.
- [Lyo.Rating.Postgres](Lyo.Net/Plugins/Rating/Lyo.Rating.Postgres/README.md): Entity Framework Core store for `Lyo.Rating` on PostgreSQL. Ratings live in `rating.rating` and reactions in `rating.rating_reaction`.
- [Lyo.Reporting.Models](Lyo.Net/Plugins/Reporting/Lyo.Reporting.Models/README.md): Lyo Reporting composition models, fluent builders, API contracts, and generation hooks.
- [Lyo.Reporting.Postgres](Lyo.Net/Plugins/Reporting/Lyo.Reporting.Postgres/README.md): PostgreSQL schema (`reporting`), EF migrations, CSV/XLSX/JSON renderers, the `ReportService` generation pipeline, and `ReportRetentionService` cleanup.
- [Lyo.Reporting.Web](Lyo.Net/Plugins/Reporting/Lyo.Reporting.Web/README.md): Blazor `ReportViewer` plus `IReportRenderer` producing HTML and PDF.
- [Lyo.Reporting.Web.Components](Lyo.Net/Plugins/Reporting/Lyo.Reporting.Web.Components/README.md): MudBlazor ops UI for Lyo Reporting: browse definitions, run reports, design compositions, and view or download generations.
- [Lyo.ShortUrl](Lyo.Net/Plugins/ShortUrl/Lyo.ShortUrl/README.md): URL-shortening contracts: `IShortUrlService`, `ShortUrlServiceBase` for validation / metrics / error-code mapping, a default `ShortUrlService` that mints short codes (no storage), `UrlShortenBuilder`, and DTOs for shorten / expand / statistics.
- [Lyo.ShortUrl.Postgres](Lyo.Net/Plugins/ShortUrl/Lyo.ShortUrl.Postgres/README.md): EF Core schema and DbContext registration for a PostgreSQL-backed short-URL store.
- [Lyo.Tag](Lyo.Net/Plugins/Tag/Lyo.Tag/README.md): Contracts for hanging tags on entities. The target is an `EntityRef`, and the person or system that applied the tag is an optional second `EntityRef`.
- [Lyo.Tag.Postgres](Lyo.Net/Plugins/Tag/Lyo.Tag.Postgres/README.md): Entity Framework Core store for `Lyo.Tag` on PostgreSQL. Rows live in `tag.tag` (`PostgresTagOptions.Schema = "tag"`), and the package includes migrations.

### Integration

- [Lyo.Discord.Bot](Lyo.Net/Integration/Discord/Lyo.Discord.Bot/README.md): Library (not an executable) that runs a DSharpPlus Discord bot and upserts guild data into your Lyo API (`Lyo.Discord.Client` against PostgreSQL-backed `Discord/*` endpoints).
- [Lyo.Discord.Client](Lyo.Net/Integration/Discord/Lyo.Discord.Client/README.md): Typed HTTP client for the Discord REST endpoints `Lyo.Api` exposes (the `Discord/*` group registered by `Lyo.Discord.Postgres`).
- [Lyo.Discord.Models](Lyo.Net/Integration/Discord/Lyo.Discord.Models/README.md): Wire-level DTOs and shared constants for the Discord integration. `Lyo.Discord.Client` (typed HTTP client) and `Lyo.Discord.Postgres` (API host + persistence) both use these so request and response shapes stay aligned.
- [Lyo.Discord.Postgres](Lyo.Net/Integration/Discord/Lyo.Discord.Postgres/README.md): PostgreSQL persistence plus `Lyo.Api` endpoint mappings for Discord entities. Schema name is locked to `discord` (`PostgresDiscordOptions.Schema`).
- [Lyo.Endato.Client](Lyo.Net/Integration/Endato/Lyo.Endato.Client/README.md): Typed HTTP client for Endato's data-enrichment REST API.
- [Lyo.Endato.Postgres](Lyo.Net/Integration/Endato/Lyo.Endato.Postgres/README.md): EF Core context and PostgreSQL schema that cache Endato Person Search (PS) and Contact Enrichment (CE) responses. Schema name is `endato`.
- [Lyo.Endato.Web.Components](Lyo.Net/Integration/Endato/Lyo.Endato.Web.Components/README.md): Blazor workbench UI for Endato person search and enrichment.
- [Lyo.Espn.Fantasy.Football.Client](Lyo.Net/Integration/Espn/Lyo.Espn.Fantasy.Football.Client/README.md): Read-only typed client for ESPN's fantasy football v3 API (`lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`).
- [Lyo.Google.Geolocation.Client](Lyo.Net/Integration/Google/Lyo.Google.Geolocation.Client/README.md): REST client for Google Maps plus an `IGeolocationService` implementation.
- [Lyo.Typecast.Client](Lyo.Net/Integration/Typecast/Lyo.Typecast.Client/README.md): HTTP client for Typecast TTS and voice catalog work. `TypecastClient` subclasses `Lyo.Api.Client.ApiClient`, sets `X-API-KEY` from `TypecastClientOptions`, and surfaces two…

### Apps

- [Lyo.Api](Lyo.Net/Apps/Api/Lyo.Api/README.md): Minimal-API library that maps EF Core entities onto REST CRUD. `ApiEndpointBuilder` emits Query, Get, Create, Update, Patch, Delete, Upsert, bulk variants, and optional export.
- [Lyo.Api.Authentication](Lyo.Net/Apps/Api/Lyo.Api.Authentication/README.md): Administrative HTTP endpoints for Lyo Authentication. Postgres stores stay in `Lyo.Authentication.Postgres`. `BuildAuthenticationApi` lives here.
- [Lyo.Api.Client](Lyo.Net/Apps/Api/Lyo.Api.Client/README.md): Lyo API HTTP client: problem-details (`ApiException`), `ApiRouteBuilder`, Query/QueryProject, correlation. Generic JSON verbs live on `Lyo.Http.Client`.
- [Lyo.Api.Export](Lyo.Net/Apps/Api/Lyo.Api.Export/README.md): Optional export add-on for Lyo.Api. Registers the Export CRUD endpoint and `IExportService<TContext>`.
- [Lyo.Api.Export.Csv](Lyo.Net/Apps/Api/Lyo.Api.Export.Csv/README.md): CSV format handler for Lyo.Api export.
- [Lyo.Api.Export.Xlsx](Lyo.Net/Apps/Api/Lyo.Api.Export.Xlsx/README.md): XLSX format handler for Lyo.Api export.
- [Lyo.Api.FileStorage](Lyo.Net/Apps/Api/Lyo.Api.FileStorage/README.md): HTTP endpoints for Lyo file storage. Hosts map BuildFileStorageApi after a keyed IFileStorageService stack is registered.
- [Lyo.Api.FileStorage.Models](Lyo.Net/Apps/Api/Lyo.Api.FileStorage.Models/README.md): Request and response DTOs for the file-storage HTTP API. This package does not depend on Lyo.FileStorage.
- [Lyo.Api.Models](Lyo.Net/Apps/Api/Lyo.Api.Models/README.md): HTTP contract models shared by Lyo minimal APIs and their clients. Distinct from `Lyo.Query.Models` (projection DTOs + filter trees).
- [Lyo.Api.Tests.Host](Lyo.Net/Apps/Api/Lyo.Api.Tests.Host/README.md): Reference ASP.NET Core minimal-API host that `Lyo.Api.Tests` and other integration tests target via `WebApplicationFactory<Program>`.
- [Lyo.Comic.Api](Lyo.Net/Apps/Comic/Lyo.Comic.Api/README.md): HTTP mapping for the Lyo Comic API (`BuildComicGroup`).
- [Lyo.Comic.Api.Host](Lyo.Net/Apps/Comic/Lyo.Comic.Api.Host/README.md): Standalone ASP.NET host for the Lyo Comic API.
- [Lyo.Config.Api](Lyo.Net/Apps/Config/Lyo.Config.Api/README.md): HTTP host for central app configuration, backed by PostgreSQL and `Lyo.Config`.
- [Lyo.Config.Api.Client](Lyo.Net/Apps/Config/Lyo.Config.Api.Client/README.md): HTTP client typed for `Lyo.Config.Api`. Conditional app-config reads with `?version` / `If-None-Match` polling, optional `X-Api-Key`, HTTP `IConfigStore` (`ConfigApiStore`) over manage routes, and DI extensions.
- [Lyo.Config.Api.Host](Lyo.Net/Apps/Config/Lyo.Config.Api.Host/README.md): ASP.NET host that runs `Lyo.Config.Api` on its own.
- [Lyo.Config.Api.Hosting](Lyo.Net/Apps/Config/Lyo.Config.Api.Hosting/README.md): Connects `IConfigApiClient` (`Lyo.Config.Api.Client`) to `Microsoft.Extensions.Options` and `Microsoft.Extensions.DependencyInjection`. A `BackgroundService` polls a shared `ResolvedConfigRecord` ledger.
- [Lyo.Config.Api.Models](Lyo.Net/Apps/Config/Lyo.Config.Api.Models/README.md): Config HTTP API contracts: `ConfigResolveOutcome`, `ConfigResolveConditionalResult`, and `HttpStatusDescriptor`.
- [Lyo.Drift.Agent](Lyo.Net/Apps/Drift/Lyo.Drift.Agent/README.md): Watches configured directories and posts file-tree and system-info snapshots to the Drift collector.
- [Lyo.Drift.Api](Lyo.Net/Apps/Drift/Lyo.Drift.Api/README.md): HTTP mapping for the Lyo Drift collector API (`BuildDriftGroup`).
- [Lyo.Drift.Api.Host](Lyo.Net/Apps/Drift/Lyo.Drift.Api.Host/README.md): Standalone ASP.NET host for the Lyo Drift collector API.
- [Lyo.Drift.Client](Lyo.Net/Apps/Drift/Lyo.Drift.Client/README.md): HTTP client for the Lyo Drift collector API.
- [Lyo.HomeInventory.Api](Lyo.Net/Apps/HomeInventory/Lyo.HomeInventory.Api/README.md): HTTP mapping for the Lyo HomeInventory API (`BuildHomeInventoryGroup`).
- [Lyo.HomeInventory.Api.Host](Lyo.Net/Apps/HomeInventory/Lyo.HomeInventory.Api.Host/README.md): Standalone ASP.NET host for the Lyo HomeInventory API.
- [Lyo.Http.Client](Lyo.Net/Apps/Http/Lyo.Http.Client/README.md): Generic HTTP client: IHttpClientFactory, JSON verbs, serializable plans, AngleSharp extract, rate limiting, cookies, and opt-in timing/UA rotation.
- [Lyo.Http.Client.Flared](Lyo.Net/Apps/Http/Lyo.Http.Client.Flared/README.md): FlareSolverr adapter for Lyo.Http.Client: ThroughSolver HTML/JSON, cookie+UA streaming downloads, PerSession UA pin. Educational/personal use.
- [Lyo.Job.Alerts](Lyo.Net/Apps/Job/Lyo.Job.Alerts/README.md): Hosted `JobAlertConsumer` that binds the `job.notifications.alert` routing key on the `job.events` exchange, deserializes `JobAlertEvent` payloads, and sends them through `INotificationPublisher` and/or an optional HTTP webhook.
- [Lyo.Job.Api](Lyo.Net/Apps/Job/Lyo.Job.Api/README.md): HTTP mapping for the Lyo Job API (`BuildJobGroup`).
- [Lyo.Job.Api.Host](Lyo.Net/Apps/Job/Lyo.Job.Api.Host/README.md): Standalone ASP.NET host for the Lyo Job API.
- [Lyo.Job.Client](Lyo.Net/Apps/Job/Lyo.Job.Client/README.md): Typed HTTP client for the Lyo Job API. Wraps `IApiClient` and exposes run-lifecycle methods (`StartAsync`, `LogAsync`, `FinishAsync`, `RequeueAsync`) plus worker-instance endpoints from `Lyo.Job.Models.Constants.Rest.Job`.
- [Lyo.Job.Scheduler](Lyo.Net/Apps/Job/Lyo.Job.Scheduler/README.md): A hosted `JobScheduler` polls the Job API for enabled definitions, evaluates schedules (blackout calendars, misfire catch-up, per-schedule time zones), and creates job runs via `IApiClient`.
- [Lyo.Job.Worker](Lyo.Net/Apps/Job/Lyo.Job.Worker/README.md): Lyo job-system worker SDK. Subclass `JobWorkerBase` and implement `ExecuteAsync(IJobWorkerContext)`. The base class consumes the priority-enabled worker-type queue.
- [Lyo.Reporting.Api](Lyo.Net/Apps/Reporting/Lyo.Reporting.Api/README.md): Authenticated HTTP endpoints for Lyo Reporting. Postgres stays service-only (`ReportService` + EF). `BuildReportingGroup` lives in this package.
- [Lyo.Reporting.Api.Host](Lyo.Net/Apps/Reporting/Lyo.Reporting.Api.Host/README.md): Standalone ASP.NET host for the Lyo Reporting API.
- [Lyo.Reporting.Client](Lyo.Net/Apps/Reporting/Lyo.Reporting.Client/README.md): Typed HTTP client targeting the Lyo Reporting API (`netstandard2.0;net10.0`).
- [Lyo.Web.Automation](Lyo.Net/Apps/Web/Automation/Lyo.Web.Automation/README.md): Browser-automation models shared across runners: locators, JSON plans, a session abstraction, and plan execution. Playwright and Selenium types stay out of this package.
- [Lyo.Web.Automation.Playwright](Lyo.Net/Apps/Web/Automation/Lyo.Web.Automation.Playwright/README.md): Playwright backend for the `Lyo.Web.Automation` abstractions: launches Chromium / Firefox / WebKit, owns session-scoped browser contexts, and matches the Selenium helpers.
- [Lyo.Web.Automation.Selenium](Lyo.Net/Apps/Web/Automation/Lyo.Web.Automation.Selenium/README.md): Selenium WebDriver backend for the `Lyo.Web.Automation` abstractions: launch Chrome / Edge / Firefox / Safari (plus Selenium Grid), isolate sessions, and cover polling, tabs, frames, and plans.
- [Lyo.Web.Components](Lyo.Net/Apps/Web/Lyo.Web.Components/README.md): MudBlazor / Blazor pieces for Lyo web UI: data grid, query builder, change-tracking form, file upload, rich-text editor, JSON editor, text-diff viewer, and identifier workbench.
- [Lyo.Web.Components.Export](Lyo.Net/Apps/Web/Lyo.Web.Components.Export/README.md): Menu items that export from Lyo data grids. Reference this package (and any optional format packages) and drop items into `BulkExportControls`.
- [Lyo.Web.Components.Export.Csv](Lyo.Net/Apps/Web/Lyo.Web.Components.Export.Csv/README.md): CSV export menu item for Lyo data grids.
- [Lyo.Web.Components.Export.Xlsx](Lyo.Net/Apps/Web/Lyo.Web.Components.Export.Xlsx/README.md): XLSX export menu item for Lyo data grids.
- [Lyo.Web.Host](Lyo.Net/Apps/Web/Lyo.Web.Host/README.md): Shared Blazor host shell so a second app does not have to copy TestGateway's layout, theme, and client DI.
- [Lyo.Web.WebRenderer](Lyo.Net/Apps/Web/Renderer/Lyo.Web.WebRenderer/README.md): Server-side Razor rendering and HTML→PDF conversion. Razor rendering uses `Microsoft.AspNetCore.Components.Web.HtmlRenderer`; PDF conversion is driven by **PuppeteerSharp** against a…

### Security

- [Lyo.Authentication](Lyo.Net/Security/Authentication/Lyo.Authentication/README.md): Server-side authentication services for Lyo. Two coexisting bearer formats behind a single contract.
- [Lyo.Authentication.AspNetCore](Lyo.Net/Security/Authentication/Lyo.Authentication.AspNetCore/README.md): ASP.NET Core integration for `Lyo.Authentication`. Three schemes coexist behind a single dispatcher.
- [Lyo.Authentication.Client](Lyo.Net/Security/Authentication/Lyo.Authentication.Client/README.md): Consumer-side runtime for the Lyo BFF auth flow. Plugs a web host, typically a Blazor Server gateway or a server-rendered API consumer, into a Lyo authentication API without ever exposing tokens to the browser.
- [Lyo.Authentication.Google](Lyo.Net/Security/Authentication/Lyo.Authentication.Google/README.md): Google profile for `Lyo.Authentication.OpenIdConnect`. Registers `https://accounts.google.com` as a confidential OIDC client in the BFF login flow.
- [Lyo.Authentication.Keycloak](Lyo.Net/Security/Authentication/Lyo.Authentication.Keycloak/README.md): Keycloak profile for `Lyo.Authentication.OpenIdConnect`. Wires one or more Keycloak realms as confidential OIDC clients in the BFF login flow.
- [Lyo.Authentication.Models](Lyo.Net/Security/Authentication/Lyo.Authentication.Models/README.md): Wire-shape data for `Lyo.Authentication`. The half of the auth stack that's safe to ship to anyone, including Blazor WebAssembly clients.
- [Lyo.Authentication.OpenIdConnect](Lyo.Net/Security/Authentication/Lyo.Authentication.OpenIdConnect/README.md): OpenID Connect client base for Lyo. The Lyo API is the OIDC confidential client (BFF pattern). The frontend never sees the IdP and never receives tokens by URL fragment.
- [Lyo.Authentication.Postgres](Lyo.Net/Security/Authentication/Lyo.Authentication.Postgres/README.md): PostgreSQL persistence for `Lyo.Authentication`. Replaces the in-memory stores from the base lib with EF Core-backed implementations of `IApiTokenStore`, `IUserStore`, `IExternalIdentityStore`, `IUserClaimStore`, and `IUserScopeStore`.
- [Lyo.Authentication.Web.Components](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components/README.md): Host-agnostic Razor / MudBlazor components for Lyo authentication. Ships Login, Profile, Tokens, Auth Debug, and the AuthAdminPanel plus the abstractions that the Server and Wasm host adapters implement.
- [Lyo.Authentication.Web.Components.Server](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components.Server/README.md): Blazor Server host adapter for `Lyo.Authentication.Web.Components`. Plugs the shared login / debug / profile pages into the BFF-cookie auth runtime in `Lyo.Authentication.Client`.
- [Lyo.Authentication.Web.Components.Wasm](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components.Wasm/README.md): Blazor WebAssembly host adapter for `Lyo.Authentication.Web.Components`. Implements the same login / debug / profile pages over a pure-browser auth flow. No consumer-side server, no HttpOnly cookie.
- [Lyo.ContentThreatScan](Lyo.Net/Security/ContentThreatScan/Lyo.ContentThreatScan/README.md): Heuristic scanning and numeric disposition scoring for readable text payloads: scripts, markup, suspicious SQL-ish patterns.
- [Lyo.ContentThreatScan.Intel](Lyo.Net/Security/ContentThreatScan/Lyo.ContentThreatScan.Intel/README.md): Optional `DefaultContentThreatReputationPipeline` for Malware Bazaar, VirusTotal, and `clamd` INSTREAM (TCP).
- [Lyo.Drift.Models](Lyo.Net/Security/Drift/Lyo.Drift.Models/README.md): DTOs, enums, and route constants for the Lyo Drift collector: instances, structure snapshots, and diffs.
- [Lyo.Drift.Postgres](Lyo.Net/Security/Drift/Lyo.Drift.Postgres/README.md): PostgreSQL persistence for the Lyo Drift collector.
- [Lyo.Drift.Web.Components](Lyo.Net/Security/Drift/Lyo.Drift.Web.Components/README.md): Blazor workbench for Drift collector instances, snapshots, diffs, and live changes.
- [Lyo.Encryption](Lyo.Net/Security/Encryption/Lyo.Encryption/README.md): Authenticated encryption for .NET. AEAD, RSA hybrids, and envelope (two-key) flows with optional Lyo.KeyStore lookup.
- [Lyo.Encryption.AesCcm](Lyo.Net/Security/Encryption/Lyo.Encryption.AesCcm/README.md): AES-CCM authenticated encryption addon for `Lyo.Encryption`. Provides `AesCcmEncryptionService` (BouncyCastle-backed on all targets) and matching DI extensions.
- [Lyo.Encryption.AesSiv](Lyo.Net/Security/Encryption/Lyo.Encryption.AesSiv/README.md): AES-SIV (RFC 5297) deterministic authenticated encryption addon for `Lyo.Encryption`. Provides `AesSivEncryptionService` backed by `Dorssel.Security.Cryptography.AesExtra` and matching DI extensions.
- [Lyo.Encryption.XChaCha20Poly1305](Lyo.Net/Security/Encryption/Lyo.Encryption.XChaCha20Poly1305/README.md): XChaCha20-Poly1305 (24-byte nonce, 32-byte key) authenticated-encryption addon for `Lyo.Encryption`.
- [Lyo.Hashing](Lyo.Net/Security/Hashing/Lyo.Hashing/README.md): Digests (SHA-256/384/512), optional MD5 for non-security fingerprints only, non-cryptographic checksums (CRC-32/CRC-32C/CRC-64/Adler-32), hexadecimal encoding (`HexEncoding`), incremental hashing (`HashingStream`), sparse file fingerprints (`SparseFileFingerprinter`), and injectable `IHashingService` / `HashingService`.
- [Lyo.KeyStore](Lyo.Net/Security/KeyStore/Lyo.KeyStore/README.md): Key encryption key (KEK) storage and rotation contracts for `Lyo.Encryption`.
- [Lyo.KeyStore.Aws](Lyo.Net/Security/KeyStore/Lyo.KeyStore.Aws/README.md): `AwsKeyStore` takes an `IAmazonSecretsManager` client and a secret-name prefix. It implements `Lyo.KeyStore.IKeyStore` and `Lyo.KeyStore.IKeyInventoryStore`, so admin UIs and key-rotation jobs can encrypt against it and list `keyId`s and versions.
- [Lyo.KeyStore.Web.Components](Lyo.Net/Security/KeyStore/Lyo.KeyStore.Web.Components/README.md): In-process Blazor workbench for IKeyStore. Lists key ids and versions, adds from a string, rotates, and sets current. No HTTP, no raw key bytes.

### Examples

- [Lyo.Drift.Agent.Example](Lyo.Net/Examples/Lyo.Drift.Agent.Example/README.md): Sample Drift agent host.
- [Lyo.Job.Scheduler.Example](Lyo.Net/Examples/Lyo.Job.Scheduler.Example/README.md): Sample job scheduler host.
- [Lyo.Job.Worker.Example](Lyo.Net/Examples/Lyo.Job.Worker.Example/README.md): Sample job worker that subscribes to job.run.example.
- [Lyo.Reporting.Business.Example](Lyo.Net/Examples/Lyo.Reporting.Business.Example/README.md): Sample reporting host that wires Lyo.Reporting into a small Blazor app.

### Tools

- [Lyo.Cli](Lyo.Net/Tools/Lyo.Cli/README.md): Installable `lyo` CLI that covers encryption, encoding, compression, hashing, IDs, query build/exec, and CSV/XLSX.
- [Lyo.TestApi](Lyo.Net/Tools/Lyo.TestApi/README.md): Minimal-API host backing `Lyo.TestGateway` and `Lyo.TestConsole`. Wires Lyo Postgres stores, the RabbitMQ job system, S3 file storage with two-key encryption, and the file-storage workbench endpoints.
- [Lyo.TestConsole](Lyo.Net/Tools/Lyo.TestConsole/README.md): Scratch host used to exercise Lyo services from a long-lived `Microsoft.Extensions.Hosting` process.
- [Lyo.TestGateway](Lyo.Net/Tools/Lyo.TestGateway/README.md): Blazor Server workbench for the Lyo platform. About 30 routed test pages (cache, locks, file storage, PDF, and more) plus a thin proxy so each page can hit a remote API or in-process services.
- [Lyo.Tools.Postgres](Lyo.Net/Tools/Lyo.Tools.Postgres/README.md): Spectre.Console TUI used for EF Core migrations against Lyo Postgres `DbContext`s, plus `Lyo.Seed` contributors (EF direct and Lyo.Api bulk).

<!-- catalog:packages:end -->

### Load testing (k6)

- [k6 framework: Person Query API](k6/framework-person/README.md): k6 workloads and query shapes against `TestApi` persons.
- [K6 benchmark analysis](Lyo.Net/Apps/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md): latest archived run metrics and comparison to common API stacks (Hasura/PostgREST, typical ORM
  APIs, etc.).

### Performance snapshots (latest archived runs)

| Suite                                                                                                  | Date       | Environment                                           | Headline results                                                                                                                                                                                                                                                                                                                                                      |
|--------------------------------------------------------------------------------------------------------|------------|-------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Compression** ([summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md))  | 2026-06-28 | .NET 10.0.9, Linux Mint 22.1, Core Ultra 7 155U       | LZ4 fastest compress @ 1 MB (**~128 µs**); Zstd fastest decompress (**~71 µs @ 1 MB**, **~13 ms @ 100 MB**); Zstd streaming compress **~31×** GZip @ 100 MB, **~5×** @ 1 GB                                                                                                                                                                                           |
| **Encryption** ([summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)) | 2026-06-30 | .NET 10.0.0, Ubuntu 24.04, Core Ultra 7 155U (AES-NI) | AES-GCM **906 µs / 614 µs** @ 1 MB; ChaCha **1.23 ms / 947 µs**; XChaCha **2.7 / 2.7 ms**; CCM **14 ms**; SIV **20 ms**; stream **~1.2 GB/s** @ 100 MB; hybrid **837 µs** enc @ 1 MB; RSA dec 1 MB **2.6 s**                                                                                                                                                          |
| **K6 Query API** ([analysis](Lyo.Net/Apps/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md))                | 2026-07-27 | TestApi + PostgreSQL + k6 on same laptop              | Full 12-suite matrix (Query / QueryProject / root Query × load/stress/spike/soak): root Query fastest (**~31–50 ms p95** load/spike/soak, **~701 ms p95** stress); QueryProject close behind (**~42–65 ms p95**, **~434 ms** stress); full-entity Query has heavier tails (**~103 ms** load, **~1.32 s** stress); status/shape checks **100%** across ~1.35M requests |

---

## Documentation

Project-wide guides live in [`docs/`](docs/README.md). Per-package API docs are the `README.md` beside each library.

| Document                                   | What it covers                                                                                   |
|--------------------------------------------|--------------------------------------------------------------------------------------------------|
| [Documentation index](docs/README.md)      | Entry point for all cross-cutting guides and interactive artifacts.                              |
| [Getting started](docs/getting-started.md) | Prerequisites, consuming a package, a minimal example.                                           |
| [Architecture](docs/architecture.md)       | Area model and dependency law (detail in [`package-layout.md`](Lyo.Net/docs/package-layout.md)). |
| [Configuration](docs/configuration.md)     | Environment variables for the tooling/runner.                                                    |
| [Testing](docs/testing.md)                 | Unit tests, benchmarks, and k6. Local and containerized.                                         |
| [Deployment](docs/deployment.md)           | The container stack and operational notes.                                                       |
| [CI](docs/ci.md)                           | GitHub Actions: `dev` previews, `main` releases, pack scopes.                                    |
| [Publishing](docs/publishing.md)           | Versioning and packing with `scripts/nuget/build_nuget.py`.                                      |
| [Security](docs/security/README.md)        | Security model and crypto design notes ([`SECURITY.md`](SECURITY.md) for reporting).             |
| [Glossary](docs/glossary.md)               | Domain terms and recurring concepts.                                                             |

Interactive HTML, open locally or via Pages: the [project graph](docs/Lyo.ProjectGraph.html) and the [benchmark dashboards](docs/benchmarks/index.html).

## Finding your way

- Start from the **Major capabilities** table for API/query, storage, PDF ([Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md)), encryption, caching, diagnostics, content-threat scanning, hashing, and compression.
- For API query behavior and endpoints, the **Lyo.Api** README is the overview to read first.
- For any other documented package, use **All packages with READMEs** above (complete list as of the last edit).

## Contributing

The license does **not** require users of the library to send changes back. That keeps adoption easy for companies and side projects. We still **welcome** fixes and improvements. See [`CONTRIBUTING.md`](CONTRIBUTING.md) and the [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md). Security issues should follow [`SECURITY.md`](SECURITY.md).

## License

Licensed under the [Apache License, Version 2.0](LICENSE) ([view on apache.org](https://www.apache.org/licenses/LICENSE-2.0)). You may use Lyo in commercial and closed-source software. See the license for attribution and redistribution requirements. Replace "The Lyo authors" in [`LICENSE`](LICENSE) if you want a specific copyright line.
