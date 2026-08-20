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
| **API & query**         | Minimal APIs and CRUD on Entity Framework Core. Typed and dynamic builders, result caching with auto-invalidation, nested WhereClause filters, projection, property-level patch, bulk with per-item fallback, and CSV/XLSX/JSON export. | [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md) · [Lyo.Query.Models](Lyo.Net/Data/Query/Lyo.Query.Models/README.md)                                                                                                                          |
| **Query client UI**     | Blazor components (for example a data grid) that speak the same query shapes as the API.                                                                                                                                                 | [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md)                                                                                                                                                                                              |
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
| [`Lyo.Net/Data/`](Lyo.Net/Data/)                                   | Data handling and persistence helpers: file storage (local/S3/Azure Blob), compression, CSV/XLSX/PDF, images, Postgres migration helpers, `Lyo.Query.Models` shapes, QR codes, file-system watching, temporary IO, and related parsers/processors.                                                                                |
| [`Lyo.Net/Features/`](Lyo.Net/Features/)                           | Composable product features (often EF-backed): comments, notes, favorites, ratings, tags, typed config, contact forms, profanity filter, short URLs. Meant to plug into host apps alongside Core and Data.                                                                                                                        |
| [`Lyo.Net/Apps/`](Lyo.Net/Apps/)                                   | Sample and reference HTTP hosts (for example centralized typed config backed by `Lyo.Config` and PostgreSQL; see `Lyo.Config.Api` packages).                                                                                                                                                                                      |
| [`Lyo.Net/Integration/`](Lyo.Net/Integration/)                     | Application-facing integration: minimal APIs and query (`Lyo.Api`), Blazor web components and reporting, browser automation ([`Lyo.Web.Automation`](Lyo.Net/Integration/Web/Automation/README.md): Selenium / Playwright, JSON plans), background jobs, Discord bot. Wires Core/Data/Features into runnable hosts.                 |
| [`Lyo.Net/docs/package-layout.md`](Lyo.Net/docs/package-layout.md) | Package taxonomy. Where Core domains, Communication providers, and Integration vendor clients belong (archetypes A–E).                                                                                                                                                                                                            |
| [`Lyo.Net/Security/`](Lyo.Net/Security/)                           | Cryptography (`Lyo.Encryption`), hashing (`Lyo.Hashing`), content-threat heuristics and optional intel (`Lyo.ContentThreatScan*`), encryption benchmarks.                                                                                                                                                                         |
| [`Lyo.Net/Communication/`](Lyo.Net/Communication/)                 | Messaging and media delivery: SMTP email, SMS (including Twilio), and text-to-speech providers.                                                                                                                                                                                                                                   |
| [`Lyo.Net/Tools/`](Lyo.Net/Tools/)                                 | Host apps and utilities (gateway, test API/console) for trying components end-to-end.                                                                                                                                                                                                                                             |
| [`k6/`](k6/)                                                       | Load-testing scripts. See [k6 framework: Person Query API](k6/framework-person/README.md) and [K6 benchmark analysis](Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md).                                                                                                                                                  |

Individual projects are mostly **one folder per NuGet-style package** (for example `Lyo.Something`). The sections below list **every** in-repo `README.md` beside a library, grouped by top-level area.

---

## All packages with READMEs

<!-- catalog:packages:start -->

### Communication

- [Lyo.Email](Lyo.Net/Communication/Email/Lyo.Email/README.md): SMTP email through MailKit. `EmailService` implements `IEmailService`.
- [Lyo.Email.Models](Lyo.Net/Communication/Email/Lyo.Email.Models/README.md): Shared models, options, error codes, and event arguments for the `Lyo.Email` SMTP service.
- [Lyo.Email.Postgres](Lyo.Net/Communication/Email/Lyo.Email.Postgres/README.md): PostgreSQL schema and `EmailDbContext` for logging emails sent by `Lyo.Email`. This package does not subscribe to `EmailService` events. Consumers map and insert rows themselves.
- [Lyo.Email.Web.Components](Lyo.Net/Communication/Email/Lyo.Email.Web.Components/README.md): Blazor (MudBlazor) workbench for sending email through an injected `IEmailService`.
- [Lyo.MessageQueue](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue/README.md): `IMqService` is the queue and exchange contract. Schedulers, workers, and gateways compile against one interface and swap `Lyo.MessageQueue.*` brokers behind it.
- [Lyo.MessageQueue.RabbitMq](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md): `IMqService` implementation (`RabbitMqService`) on `RabbitMQ.Client`. Also registered as `IRabbitMqService` for exchanges and other RabbitMQ-only methods.
- [Lyo.MessageQueue.RabbitMq.Web.Components](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq.Web.Components/README.md): Blazor components for RabbitMQ exchanges, bindings, and broker workbenches.
- [Lyo.MessageQueue.Web.Components](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.Web.Components/README.md): Blazor components for provider-neutral message queue dashboards and workbenches.
- [Lyo.Sms](Lyo.Net/Communication/Sms/Lyo.Sms/README.md): SMS contracts and shared send pipeline. Providers (`Lyo.Sms.Twilio`, and others) implement `SmsServiceBase`.
- [Lyo.Sms.Models](Lyo.Net/Communication/Sms/Lyo.Sms.Models/README.md): Shared types for `Lyo.Sms`: payloads, paging, events, normalization, and base options. This package does not send SMS. Implementations live in provider packages (`Lyo.Sms.Twilio`, and others).
- [Lyo.Sms.Postgres](Lyo.Net/Communication/Sms/Lyo.Sms.Postgres/README.md): EF Core PostgreSQL store for outbound SMS logs (`SmsLogEntity`). This package does not send SMS. It wires `SmsDbContext` so workers or gateways can persist send outcomes.
- [Lyo.Sms.Twilio](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio/README.md): Twilio SMS and MMS through `Lyo.Sms`. `TwilioSmsService` implements `ISmsService`.
- [Lyo.Sms.Twilio.Postgres](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio.Postgres/README.md): EF Core PostgreSQL store for Twilio SMS traces: `TwilioSmsDbContext` and `TwilioSmsLogEntity`.
- [Lyo.Sms.Web.Components](Lyo.Net/Communication/Sms/Lyo.Sms.Web.Components/README.md): Blazor (MudBlazor) workbench for an injected `ISmsService`. Uses MudBlazor and snackbar helpers from `Lyo.Web.Components`.
- [Lyo.Stt](Lyo.Net/Communication/Speech/Lyo.Stt/README.md): Speech-to-text contract for Lyo. Ships `ISttService`, `SttServiceBase`, request/result/options/event records, and metric name constants. No provider packages ship in this repo.
- [Lyo.Translation](Lyo.Net/Communication/Translation/Lyo.Translation/README.md): **Archetype B (capability).** Providers (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) stay under `Communication/Translation/`, not `Integration/`. See package layout.
- [Lyo.Translation.Aws](Lyo.Net/Communication/Translation/Lyo.Translation.Aws/README.md): Amazon Translate implementation of `ITranslationService`. Translates text, runs bounded bulk translation, infers language via a Translate call, and probes connectivity with `ListLanguages`.
- [Lyo.Translation.Google](Lyo.Net/Communication/Translation/Lyo.Translation.Google/README.md): Google Cloud Translation v2 implementation of `ITranslationService`. `GoogleTranslationService` extends `TranslationServiceBase` and calls the REST API over HTTP.
- [Lyo.Translation.Web.Components](Lyo.Net/Communication/Translation/Lyo.Translation.Web.Components/README.md): Blazor (MudBlazor) workbench for the configured `Lyo.Translation` implementation.
- [Lyo.Tts](Lyo.Net/Communication/Speech/Lyo.Tts/README.md): Contracts and shared TTS behavior: provider-agnostic interfaces, a non-generic facade, and a base service with bulk synthesis, metrics, and lifecycle events.
- [Lyo.Tts.AwsPolly](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly/README.md): Amazon Polly TTS. `AwsPollyTtsService` extends `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk synthesis, metrics, and DI helpers.
- [Lyo.Tts.AwsPolly.Web.Components](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly.Web.Components/README.md): Blazor (MudBlazor) workbench for trying `Lyo.Tts.AwsPolly` from a host app.
- [Lyo.Tts.Models](Lyo.Net/Communication/Speech/Lyo.Tts.Models/README.md): Shared TTS requests, results, options, and event payloads. Provider packages reference this instead of each other.
- [Lyo.Tts.Typecast](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast/README.md): Typecast TTS via `Lyo.Typecast.Client`. `TypecastTtsService` synthesizes audio through `TypecastClient`, can load the voice catalog for validation (`LoadVoicesAsync`), and uses the bulk pipeline and Typecast-namespaced metrics from `Lyo.Tts`.
- [Lyo.Tts.Typecast.Web.Components](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast.Web.Components/README.md): Blazor (MudBlazor) workbench for trying `Lyo.Tts.Typecast` from a host app.
- [Lyo.Tts.WindowsSpeech](Lyo.Net/Communication/Speech/Lyo.Tts.WindowsSpeech/README.md): Windows SAPI text-to-speech. `WindowsSpeechTtsService` uses the built-in Speech API.

### Core

- [Lyo.Audit](Lyo.Net/Core/Audit/Lyo.Audit/README.md): Audit trail library with two records: `AuditChange` (entity change tracking) and `AuditEvent` (events to log).
- [Lyo.Audit.Postgres](Lyo.Net/Core/Audit/Lyo.Audit.Postgres/README.md): PostgreSQL implementation of Lyo.Audit using Entity Framework Core. Persists `AuditChange` and `AuditEvent` records to PostgreSQL with JSONB columns for dictionary data.
- [Lyo.Benchmark](Lyo.Net/Core/Benchmark/Lyo.Benchmark/README.md): Benchmark-only helpers shared by every `*.Benchmarks` executable. The BenchmarkDotNet analogue of `Lyo.Testing`.
- [Lyo.Benchmark.Models](Lyo.Net/Core/Benchmark/Lyo.Benchmark.Models/README.md): Models and builders for the Lyo benchmark report schema (`lyo.bench/v1`).
- [Lyo.Cache](Lyo.Net/Core/Cache/Lyo.Cache/README.md): Local `ICacheService` plus typed byte payload methods. Serialize once, store framed bytes, optionally compress or encrypt on .NET 10+.
- [Lyo.Cache.Fusion](Lyo.Net/Core/Cache/Lyo.Cache.Fusion/README.md): `FusionCacheService` adapts `ZiggyCreatures.FusionCache` to `ICacheService` so `Lyo.Api`, workers, and feature modules can swap in-memory `Lyo.Cache` for Fusion plus an optional Redis backplane without rewriting call sites.
- [Lyo.ChangeTracker](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker/README.md): Generic entity change history built around `Lyo.EntityReference.Models.EntityRef`. Record property-level changes for any entity type without coupling the tracker to a specific aggregate.
- [Lyo.ChangeTracker.Postgres](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker.Postgres/README.md): PostgreSQL implementation of `Lyo.ChangeTracker`. Persists entity-scoped change history using `Lyo.EntityReference.Models.EntityRef` for both the target entity and the optional actor.
- [Lyo.Common](Lyo.Net/Core/Common/Lyo.Common/README.md): Shared primitives: ID generators, file/MIME/language/HTTP/file-size metadata, geometry, secure RNG, typed extensions, and shared `System.Text.Json` options.
- [Lyo.DateAndTime](Lyo.Net/Core/DateAndTime/Lyo.DateAndTime/README.md): Date, time, US timezone conversion, day-of-week scheduling, and US holiday metadata. Static and thread-safe. No mutable shared state.
- [Lyo.Diagnostic](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic/README.md): Stack trace decoding, exception classification, breadcrumb trails, an in-memory error inbox, sanitisation, and structured logging.
- [Lyo.Diagnostic.AspNetCore](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.AspNetCore/README.md): ASP.NET Core integration for `Lyo.Diagnostic`. Scoped breadcrumb trails per request and exception recording to the in-memory error inbox plus structured logging, without replacing existing problem-details middleware.
- [Lyo.Diagnostic.Web.Components](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.Web.Components/README.md): Blazor (Server / Interactive) workbench for analyzing and triaging .NET stack traces and exception payloads with `Lyo.Diagnostic`.
- [Lyo.Diff](Lyo.Net/Core/Diff/Lyo.Diff/README.md): Side-by-side comparison for human-readable text and object graphs.
- [Lyo.EntityReference.Models](Lyo.Net/Core/EntityReference/Lyo.EntityReference.Models/README.md): Typed pair of logical entity kind (`EntityType`) and identifier string (`EntityId`), plus helpers for composite keys, JSON, opaque tokens, validation, and domain row shapes.
- [Lyo.EntityReference.Postgres](Lyo.Net/Core/EntityReference/Lyo.EntityReference.Postgres/README.md): Entity Framework Core building blocks for relation rows (subject/actor associations) and source link rows (import provenance) on PostgreSQL.
- [Lyo.Exceptions](Lyo.Net/Core/Exceptions/Lyo.Exceptions/README.md): Exception types and argument validation helpers used by Lyo packages.
- [Lyo.Geolocation](Lyo.Net/Core/Geolocation/Lyo.Geolocation/README.md): Provider-agnostic geospatial operations and persistence contracts.
- [Lyo.Geolocation.Models](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Models/README.md): Neutral data contracts for `Lyo.Geolocation` and `Lyo.Geolocation.Postgres`.
- [Lyo.Geolocation.Postgres](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Postgres/README.md): PostgreSQL persistence for canonical geolocation data using Entity Framework Core.
- [Lyo.Health](Lyo.Net/Core/Health/Lyo.Health/README.md): Interface for services that report their own health. Implement `IHealth`. There is no central health service.
- [Lyo.Lock](Lyo.Net/Core/Lock/Lyo.Lock/README.md): Key-based exclusive locks and keyed semaphores (bounded concurrency per key), plus in-memory implementations for a single process.
- [Lyo.Lock.Redis](Lyo.Net/Core/Lock/Lyo.Lock.Redis/README.md): Distributed `ILockService` on Redis via StackExchange.Redis. Use this when multiple app instances must exclude each other on the same logical key.
- [Lyo.Mathematics](Lyo.Net/Core/Mathematics/Lyo.Mathematics/README.md): C# contracts for the Lyo math stack: physical quantities as structs, 2D/3D vectors and small matrices, typed inputs/results for formulas, and a small registry for discoverability.
- [Lyo.Metrics](Lyo.Net/Core/Metrics/Lyo.Metrics/README.md): Thread-safe counters, gauges, histograms, timings, errors, and events, with in-memory, OpenTelemetry, and null implementations.
- [Lyo.Metrics.OpenTelemetry](Lyo.Net/Core/Metrics/Lyo.Metrics.OpenTelemetry/README.md): OpenTelemetry implementation of `IMetrics` for exporting metrics to OpenTelemetry-compatible backends.
- [Lyo.Metrics.Statistics](Lyo.Net/Core/Metrics/Lyo.Metrics.Statistics/README.md): Statistical analysis extensions for `Lyo.Metrics` histograms. Provides percentile / quartile / moving-average / anomaly-detection helpers on top of the metrics primitives in `Lyo.Metrics`.
- [Lyo.Notification](Lyo.Net/Core/Notification/Lyo.Notification/README.md): In-process publish/subscribe for small domain events. Not durable, not distributed, and not ordered across machines. Only useful when every publisher and handler lives in the same process.
- [Lyo.PackageMetadata](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata/README.md): Multi-ecosystem `PackageMetadata` rows, `PackageMetadataRegistration`, `IPackageMetadataStore`, and `PackageArtifactDigest` helpers for correlating stack-trace namespaces with persisted catalog data.
- [Lyo.PackageMetadata.Postgres](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata.Postgres/README.md): EF Core persistence for `Lyo.PackageMetadata.IPackageMetadataStore`.
- [Lyo.People.Models](Lyo.Net/Core/People/Lyo.People.Models/README.md): `Person`, contact, employment, identification, and relationship records for the people domain.
- [Lyo.People.Postgres](Lyo.Net/Core/People/Lyo.People.Postgres/README.md): PostgreSQL persistence for Lyo.People.Models using Entity Framework Core.
- [Lyo.Privacy](Lyo.Net/Core/Privacy/Lyo.Privacy/README.md): Redacts emails, phones, Luhn card numbers, IBAN, secrets, IDs, URLs, IPs, and street lines in free text, JSON, and XML.
- [Lyo.Privacy.AspNetCore](Lyo.Net/Core/Privacy/Lyo.Privacy.AspNetCore/README.md): ASP.NET Core DI integration for `Lyo.Privacy`: registers `ITextRedactor` / `IStructuredRedactor`, binds `PrivacyRedactorOptions` from configuration, and supports keyed per-tenant or per-feature…
- [Lyo.Privacy.Web.Components](Lyo.Net/Core/Privacy/Lyo.Privacy.Web.Components/README.md): Blazor (Server / Interactive) workbench components for `Lyo.Privacy`. Lets operators preview, compare, and tune redaction policies without round-tripping through a host config edit.
- [Lyo.Resilience](Lyo.Net/Core/Resilience/Lyo.Resilience/README.md): A thin wrapper around Polly for resilience pipelines with configuration-from-appsettings support and built-in logging.
- [Lyo.Result](Lyo.Net/Core/Result/Lyo.Result/README.md): `Result` / `Result<T>` with `Error` graphs, builders, bulk/paged envelopes, and `Task` composition. Separate from `Lyo.Common` `Result`.
- [Lyo.Schedule.Models](Lyo.Net/Core/Schedule/Lyo.Schedule.Models/README.md): DTO-only assembly that describes a schedule. Used by `Lyo.Scheduler`, `Lyo.Job.Postgres`, and any consumer that needs a transport-friendly representation of "when does this run".
- [Lyo.Schedule.Web.Components](Lyo.Net/Core/Schedule/Lyo.Schedule.Web.Components/README.md): Blazor component(s) for building and previewing `Lyo.Schedule.Models.ScheduleDefinition` values interactively.
- [Lyo.Scheduler](Lyo.Net/Core/Scheduler/Lyo.Scheduler/README.md): In-process scheduler service for executing actions at scheduled times. Supports **SetTimes**, **Interval**, **OneShot**, and **Cron** schedules (5- or 6-field expressions) with logging, metrics, and…
- [Lyo.Scheduler.Cache](Lyo.Net/Core/Scheduler/Lyo.Scheduler.Cache/README.md): Cache-backed `ISchedulerStateStore` for `Lyo.Scheduler`. Persists each schedule's `LastRunUtc` / `NextRunUtc` / state markers through `Lyo.Cache` so cron/interval/one-shot schedules survive process…
- [Lyo.Scientific](Lyo.Net/Core/Scientific/Lyo.Scientific/README.md): Scientific domain models, reference datasets, SI-oriented unit helpers, and formula discovery built on `Lyo.Mathematics`.
- [Lyo.Streams](Lyo.Net/Core/Streams/Lyo.Streams/README.md): `TeeStream`, `CountingStream`, `ProgressStream`, `ConcatenatedStream`, and related stream wrappers. Incremental hashing lives in `Lyo.Hashing` (`HashingStream`).
- [Lyo.Testing](Lyo.Net/Core/Testing/Lyo.Testing/README.md): xUnit v3 helpers: fluent `Should*` assertions, exception and collection helpers, polling assertions, and an `ITestOutputHelper` logger.
- [Lyo.Testing.Containers](Lyo.Net/Core/Testing/Lyo.Testing.Containers/README.md): xUnit v3 fixtures around Testcontainers for PostgreSQL and RabbitMQ.
- [Lyo.TextEncoding](Lyo.Net/Core/Common/Lyo.TextEncoding/README.md): Binary codecs (Base64 / Base64Url / Hex) and charset encode/decode/convert with CodePages, detection, PEM/MIME, and injectable services.
- [Lyo.Validation](Lyo.Net/Core/Validation/Lyo.Validation/README.md): C# validators, fluent rule builders, validation attributes, and `WhereClause` schemas that return `Lyo.Result.Result<T>` failures.
- [Lyo.Validation.Postgres](Lyo.Net/Core/Validation/Lyo.Validation.Postgres/README.md): PostgreSQL persistence for `ValidationSchema` documents (WhereClause JSONB) via `IValidationSchemaStore`.
- [Lyo.Webhook](Lyo.Net/Core/Webhook/Lyo.Webhook/README.md): Inbound webhook verification for ASP.NET Core: raw body and headers, HMAC helpers, `MapWebhook().Verify().Handle()`, and `Lyo.Metrics` timings.
- [Lyo.Webhook.Twilio](Lyo.Net/Core/Webhook/Lyo.Webhook.Twilio/README.md): Twilio webhook signature validation for `Lyo.Webhook`. Compares `X-Twilio-Signature` to an HMAC-SHA1 (Base64) of the public request URL plus sorted key+value form parameters.

### Data

- [Lyo.Barcode](Lyo.Net/Data/Barcode/Lyo.Barcode/README.md): Barcode generation and decoding contracts: IBarcodeService, request and options models, and BarcodeBuilder.
- [Lyo.Barcode.Native](Lyo.Net/Data/Barcode/Lyo.Barcode.Native/README.md): IBarcodeService implementation for Lyo.Barcode. No third-party barcode generator.
- [Lyo.Barcode.TestWorkbench.Web.Components](Lyo.Net/Data/Barcode/Lyo.Barcode.TestWorkbench.Web.Components/README.md): MudBlazor page wrapper that hosts <BarcodeWorkbench /> from Lyo.Barcode.Web.Components inside a MudContainer for the Lyo gateway test harness.
- [Lyo.Barcode.Web.Components](Lyo.Net/Data/Barcode/Lyo.Barcode.Web.Components/README.md): MudBlazor components that call IBarcodeService from Lyo.Barcode.
- [Lyo.Compression](Lyo.Net/Data/Compression/Lyo.Compression/README.md): Compress and decompress bytes, strings, streams, and files through ICompressionService. One default codec, plus ICompressionResolver for per-algorithm dispatch.
- [Lyo.Compression.BZip2](Lyo.Net/Data/Compression/Lyo.Compression.BZip2/README.md): BZip2 compression addon for `Lyo.Compression`. Registers a BZip2 `ICompressorFactory`.
- [Lyo.Compression.Lz4](Lyo.Net/Data/Compression/Lyo.Compression.Lz4/README.md): LZ4 compression addon for `Lyo.Compression`. Registers an `LZ4` `ICompressorFactory` backed by `EasyCompressor.LZ4`.
- [Lyo.Compression.Lzma](Lyo.Net/Data/Compression/Lyo.Compression.Lzma/README.md): LZMA compression addon for `Lyo.Compression`. Registers an LZMA `ICompressorFactory`.
- [Lyo.Compression.Snappier](Lyo.Net/Data/Compression/Lyo.Compression.Snappier/README.md): Snappy compression addon for `Lyo.Compression`. Registers a Snappier `ICompressorFactory`.
- [Lyo.Compression.Xz](Lyo.Net/Data/Compression/Lyo.Compression.Xz/README.md): XZ / LZMA2 compression addon for `Lyo.Compression`. Registers an XZ `ICompressorFactory`.
- [Lyo.Compression.Zstd](Lyo.Net/Data/Compression/Lyo.Compression.Zstd/README.md): Zstandard compression addon for `Lyo.Compression`. Registers a Zstd `ICompressorFactory`.
- [Lyo.Csv](Lyo.Net/Data/Csv/Lyo.Csv/README.md): Owned CSV stack implementing Lyo.Csv.Models. CsvService composes a CsvWriter and CsvReader over an internal tokenizer/writer with typed binders. No third-party CSV library.
- [Lyo.Csv.Models](Lyo.Net/Data/Csv/Lyo.Csv.Models/README.md): Interfaces and value types for the Lyo CSV stack. Lyo.Csv implements this contract so consumers can depend on ICsvService, ICsvReader, and ICsvWriter without the implementation package.
- [Lyo.DataTable](Lyo.Net/Data/DataTable/Lyo.DataTable/README.md): Empty package placeholder reserving the `Lyo.DataTable` name. The runtime types (`DataTable`, `DataTableRow`, `DataTableBuilder`, cell types, HTML renderer) all live in `Lyo.DataTable.Models`.
- [Lyo.DataTable.Models](Lyo.Net/Data/DataTable/Lyo.DataTable.Models/README.md): Mutable in-memory data table with sparse columns, thin cells, an optional format map, fluent builders, and an HTML renderer.
- [Lyo.FFmpeg](Lyo.Net/Data/FFmpeg/Lyo.FFmpeg/README.md): Wraps the ffmpeg, ffprobe, and ffplay CLIs (via CliWrap) behind IAudioPlayer, IAudioProber, and IAudioConverter from Lyo.FFmpeg.Models.
- [Lyo.FFmpeg.Models](Lyo.Net/Data/FFmpeg/Lyo.FFmpeg.Models/README.md): Contracts and models for Lyo.FFmpeg: IAudioPlayer, IAudioProber, IAudioConverter, AudioConversionRequest, AudioConversionOptions, AudioProbeResult, and FFmpegOptions.
- [Lyo.FileMetadataStore](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore/README.md): File identity without bytes. Canonical Guid file identifiers and metadata, not blob I/O.
- [Lyo.FileMetadataStore.Postgres](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md): Postgres IFileMetadataStore plus adjunct stores used by richer file pipelines.
- [Lyo.FileMetadataStore.Sqlite](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore.Sqlite/README.md): SQLite IFileMetadataStore using Entity Framework Core. Same store and adjunct services as Lyo.FileMetadataStore.Postgres, for embedded, offline-first, and local-dev hosts.
- [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md): Save, stream-save, read, delete, and metadata for files. Optional compression (Lyo.Compression), two-key encryption (Lyo.Encryption), duplicate hashing, access policies, malware scans, audit hooks, multipart uploads (IMultipartUploadService), and presigned/direct-upload/copy on cloud backends.
- [Lyo.FileStorage.AzureBlob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.AzureBlob/README.md): Azure Blob Storage implementation of IFileStorageService using Azure.Storage.Blobs.
- [Lyo.FileStorage.Ftp](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Ftp/README.md): FTP-backed IFileStorageService via Lyo.Ftp.Client.
- [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md): S3-compatible storage for Lyo.FileStorage (AWS S3, Backblaze B2, MinIO, and others) via AWSSDK.S3.
- [Lyo.FileStorage.Sftp](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Sftp/README.md): SFTP-backed IFileStorageService via Lyo.Sftp.Client.
- [Lyo.FileStorage.Web.Components](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Web.Components/README.md): Blazor Server / Interactive UI for Lyo.FileStorage. Workbench grids and dialogs for file metadata, expected storage keys, download access links, and two-key encryption keys.
- [Lyo.FileSystemWatcher](Lyo.Net/Data/FileSystemWatcher/Lyo.FileSystemWatcher/README.md): Snapshot-based file watcher for .NET. Detects creates, deletes, changes, moves, and renames with debounce and SHA256 hashing.
- [Lyo.Formatter](Lyo.Net/Data/Formatter/Lyo.Formatter/README.md): SmartFormat.NET templating for user-defined strings: named placeholders, lists, pluralization, and culture-aware formatting.
- [Lyo.Formatter.Web.Components](Lyo.Net/Data/Formatter/Lyo.Formatter.Web.Components/README.md): Blazor pair for live SmartFormat editing: a debounced template box and an annotated preview that color-links `{keys}` to replacements. Works on WASM.
- [Lyo.Ftp.Client](Lyo.Net/Data/Ftp/Lyo.Ftp.Client/README.md): Pooled FluentFTP client with PathHelpers jail, logging, and Lyo.Metrics. Prefer `*Async`.
- [Lyo.IO.Temp](Lyo.Net/Data/IOTemp/Lyo.IO.Temp/README.md): Create and manage temporary files and directories with sessions, naming strategies, and overflow handling.
- [Lyo.IO.Temp.Ftp](Lyo.Net/Data/IOTemp/Lyo.IO.Temp.Ftp/README.md): FTP-backed IIOTempStorageProvider for Lyo.IO.Temp.
- [Lyo.IO.Temp.Sftp](Lyo.Net/Data/IOTemp/Lyo.IO.Temp.Sftp/README.md): SFTP-backed IIOTempStorageProvider for Lyo.IO.Temp.
- [Lyo.Images](Lyo.Net/Data/Images/Lyo.Images/README.md): Raster image processing for .NET using SixLabors.ImageSharp.
- [Lyo.Images.Ocr](Lyo.Net/Data/Images/Lyo.Images.Ocr/README.md): OCR contracts for Lyo: `IOcrEngine`, request/response models, Y-up pixel bounding boxes (aligned with `BoundingBox2D`), coordinate helpers, and shared options.
- [Lyo.Images.Ocr.Tesseract](Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract/README.md): Tesseract implementation of `IOcrEngine` from `Lyo.Images.Ocr`. Calls are serialized with an internal lock because native Tesseract instances are not safely concurrent.
- [Lyo.Images.OpenCv](Lyo.Net/Data/Images/Lyo.Images.OpenCv/README.md): OpenCV helpers for .NET via OpenCvSharp4. Separate from higher-level pipelines (e.g. comic overlay) so hosts pull native OpenCV only where needed.
- [Lyo.Images.Skia](Lyo.Net/Data/Images/Lyo.Images.Skia/README.md): SkiaSharp `IImageService` from `Lyo.Images`: resize, crop, rotate, watermark, convert, thumbnails, compression, metadata, palette, batch.
- [Lyo.Images.Web.Components](Lyo.Net/Data/Images/Lyo.Images.Web.Components/README.md): Blazor / MudBlazor workbenches for `Lyo.Images`: `IImageService` ops and a spritesheet animator/extractor on `ISpriteSheetExportService`.
- [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md): PdfPig-backed reading and PDFsharp-backed editing for `Lyo.Pdf.Models`. `PdfService` is the entry point; it returns disposable `IPdfReader` instances for read/extract workflows and `IPdfWriter`…
- [Lyo.Pdf.Models](Lyo.Net/Data/Pdf/Lyo.Pdf.Models/README.md): Interfaces and value types for the Lyo PDF stack. Defines the contracts implemented by `Lyo.Pdf` so consumers can depend on `IPdfService`, `IPdfReader`, `IPdfWriter`, and `ITextExtractor` without…
- [Lyo.Pdf.Ocr](Lyo.Net/Data/Pdf/Lyo.Pdf.Ocr/README.md): Renders a PDF page to PNG via `Lyo.Pdf.Rendering`, runs `IOcrEngine`, then maps OCR pixel boxes back into PDF points.
- [Lyo.Pdf.Rendering](Lyo.Net/Data/Pdf/Lyo.Pdf.Rendering/README.md): Rasterizes PDF pages to PNG via PDFtoImage (PDFium + Skia; `bblanchon.PDFium` native packages). Targets `net10.0`.
- [Lyo.Pdf.Web.Components](Lyo.Net/Data/Pdf/Lyo.Pdf.Web.Components/README.md): Blazor / MudBlazor PDF workbenches: HTML to PDF, annotation, and `LyoPdfAnnotator` for drawing bounding-box regions that emit `PdfBoundingBox`.
- [Lyo.Postgres](Lyo.Net/Data/Postgres/Lyo.Postgres/README.md): Shared PostgreSQL migration plumbing for Lyo libraries that ship their own EF Core schema (Audit, Email, ChangeTracker, EntityReference, etc.).
- [Lyo.QRCode](Lyo.Net/Data/QRCode/Lyo.QRCode/README.md): QR code generation and reading for Lyo: `IQRCodeService`, `QRCodeBuilder`, ISO Model 2 encoding in-box (`BuiltInQRCodeService`), optional QRCoder adapter.
- [Lyo.QRCode.QRCoder](Lyo.Net/Data/QRCode/Lyo.QRCode.QRCoder/README.md): QRCoder implementation of `IQRCodeService` from `Lyo.QRCode`. Use this for JPEG / Bitmap on Windows, or QRCoder's renderers.
- [Lyo.QRCode.Web.Components](Lyo.Net/Data/QRCode/Lyo.QRCode.Web.Components/README.md): Blazor / MudBlazor components for QR code generation and preview.
- [Lyo.Query](Lyo.Net/Data/Query/Lyo.Query/README.md): WhereClause AST → LINQ on IQueryable: filter, multi-key sort, in-memory match/explain, with ICache-backed compiled predicates.
- [Lyo.Query.Models](Lyo.Net/Data/Query/Lyo.Query.Models/README.md): Filter / sort / projection DTOs and fluent builders (`WhereClause`, QueryConcrete / QueryProject / root Query) shared by Lyo.Query and Lyo.Api.
- [Lyo.Query.Web.Components](Lyo.Net/Data/Query/Lyo.Query.Web.Components/README.md): Blazor / MudBlazor components for editing and running `Lyo.Query.Models` requests against any Lyo.Api host.
- [Lyo.Sftp.Client](Lyo.Net/Data/Sftp/Lyo.Sftp.Client/README.md): Pooled SSH.NET SFTP client with PathHelpers jail, logging, and Lyo.Metrics. Prefer `*Async`.
- [Lyo.Sqlite](Lyo.Net/Data/Sqlite/Lyo.Sqlite/README.md): Shared SQLite migration plumbing for Lyo libraries that ship their own EF Core schema.
- [Lyo.Xlsx](Lyo.Net/Data/Xlsx/Lyo.Xlsx/README.md): Implementation of `Lyo.Xlsx.Models`. `XlsxService` composes an `XlsxWriter` (streaming `DocumentFormat.OpenXml` writer) and an `XlsxReader` (ExcelDataReader / ClosedXML) to read and write XLSX…
- [Lyo.Xlsx.Models](Lyo.Net/Data/Xlsx/Lyo.Xlsx.Models/README.md): Interfaces and value types for the Lyo XLSX stack. Defines the contract implemented by `Lyo.Xlsx` so consumers can depend on `IXlsxService` / `IXlsxReader` / `IXlsxWriter` without pulling in ClosedXML…

### Features

- [Lyo.Comic](Lyo.Net/Features/Comic/Lyo.Comic/README.md): Domain contracts for a serialized fiction catalog: series (`ComicSeries`, `ComicAlternateTitle`), hierarchy (`ComicVolume`, `ComicChapter`, `ComicPage`), cast (`ComicCharacter`), `ComicSeriesQuery`, `ComicType`/`ComicStatus`, and `IComicStore`.
- [Lyo.Comic.Postgres](Lyo.Net/Features/Comic/Lyo.Comic.Postgres/README.md): PostgreSQL + EF Core implementation of `Lyo.Comic.IComicStore` (`PostgresComicStore`) via `ComicDbContext` and `PostgresComicOptions`.
- [Lyo.Comic.Web.Components](Lyo.Net/Features/Comic/Lyo.Comic.Web.Components/README.md): Blazor components for browsing, previewing, and reading comic series. Search panel, result grids and lists, browse cards, and a MangaFire-style tap-to-navigate reader.
- [Lyo.Comment](Lyo.Net/Features/Comment/Lyo.Comment/README.md): Abstractions for threaded, reactable comments on any entity. Each comment has a **subject**, an **actor**, optional `ReplyToCommentId`, and cached like/dislike counters.
- [Lyo.Comment.Postgres](Lyo.Net/Features/Comment/Lyo.Comment.Postgres/README.md): PostgreSQL implementation of `Lyo.Comment` using Entity Framework Core. Persists comments to `comment.comment` and reactions to `comment.comment_reaction`.
- [Lyo.Config](Lyo.Net/Features/Config/Lyo.Config/README.md): Typed, definition-driven configuration for per-entity values (a Discord guild, a tenant). The abstract API lives here. PostgreSQL persistence is in `Lyo.Config.Postgres`.
- [Lyo.Config.Postgres](Lyo.Net/Features/Config/Lyo.Config.Postgres/README.md): PostgreSQL + EF Core implementation of `Lyo.Config.IConfigStore` for typed configuration definitions and per-entity bindings.
- [Lyo.ContactUs](Lyo.Net/Features/ContactUs/Lyo.ContactUs/README.md): Contact-form submission contracts. `IContactUsService` and `ContactUsServiceBase` handle validation, error-code mapping, and logging. Storage lives in sibling packages.
- [Lyo.ContactUs.Postgres](Lyo.Net/Features/ContactUs/Lyo.ContactUs.Postgres/README.md): PostgreSQL + EF Core implementation of `Lyo.ContactUs.IContactUsService` (`PostgresContactUsService`) via `ContactUsDbContext` and `PostgresContactUsOptions`.
- [Lyo.Favorite](Lyo.Net/Features/Favorite/Lyo.Favorite/README.md): Abstractions for "X favorited Y" relationships across any two entities. The API accepts `EntityRef` at the boundary.
- [Lyo.Favorite.Postgres](Lyo.Net/Features/Favorite/Lyo.Favorite.Postgres/README.md): PostgreSQL implementation of `Lyo.Favorite` using Entity Framework Core. Persists favorites to the `favorite.favorite` table (`PostgresFavoriteOptions.Schema = "favorite"`).
- [Lyo.HomeInventory](Lyo.Net/Features/HomeInventory/Lyo.HomeInventory/README.md): Contract for household inventory: large purchases (electronics, appliances) with warranty tracking, kitchen consumables across pantries / freezers, and garage bin locations.
- [Lyo.HomeInventory.Postgres](Lyo.Net/Features/HomeInventory/Lyo.HomeInventory.Postgres/README.md): EF Core implementation of `IHomeInventoryStore` backed by PostgreSQL.
- [Lyo.Note](Lyo.Net/Features/Note/Lyo.Note/README.md): Abstractions for notes attached to entities. Each note has a **subject** (what it is about) and an **actor** (who wrote it), expressed as `EntityRef`.
- [Lyo.Note.Postgres](Lyo.Net/Features/Note/Lyo.Note.Postgres/README.md): PostgreSQL implementation of `Lyo.Note` using Entity Framework Core. Persists notes to the `note.note` table (`PostgresNoteOptions.Schema = "note"`) and ships migrations.
- [Lyo.Profanity](Lyo.Net/Features/Profanity/Lyo.Profanity/README.md): File-based profanity filter. Detects and replaces profane words. Multiple languages, regex patterns, plain word lists, and configurable replacement strategies.
- [Lyo.Rating](Lyo.Net/Features/Rating/Lyo.Rating/README.md): Abstractions for rating and reviewing entities, plus like/dislike reactions on those ratings.
- [Lyo.Rating.Postgres](Lyo.Net/Features/Rating/Lyo.Rating.Postgres/README.md): PostgreSQL implementation of `Lyo.Rating` using Entity Framework Core. Persists ratings to `rating.rating` and reactions to `rating.rating_reaction`.
- [Lyo.ShortUrl](Lyo.Net/Features/ShortUrl/Lyo.ShortUrl/README.md): URL shortening contracts: `IShortUrlService`, `ShortUrlServiceBase` for validation / metrics / error-code mapping, a default `ShortUrlService` that generates short codes (no storage), `UrlShortenBuilder`, and shorten / expand / statistics DTOs.
- [Lyo.ShortUrl.Postgres](Lyo.Net/Features/ShortUrl/Lyo.ShortUrl.Postgres/README.md): EF Core schema and DbContext registration for a PostgreSQL-backed short-URL store.
- [Lyo.Tag](Lyo.Net/Features/Tag/Lyo.Tag/README.md): Abstractions for tagging entities. Tags key off an `EntityRef` (what is tagged) and an optional second `EntityRef` (who applied the tag).
- [Lyo.Tag.Postgres](Lyo.Net/Features/Tag/Lyo.Tag.Postgres/README.md): PostgreSQL implementation of `Lyo.Tag` using Entity Framework Core. Persists tags to the `tag.tag` table (`PostgresTagOptions.Schema = "tag"`) and ships migrations.

### Integration

- [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md): Minimal-API library that maps EF Core entities to REST CRUD. `ApiEndpointBuilder` emits Query, Get, Create, Update, Patch, Delete, Upsert, bulk variants, and optional export.
- [Lyo.Api.Client](Lyo.Net/Integration/Api/Lyo.Api.Client/README.md): HTTP client for Lyo minimal APIs: JSON in/out, gzip/brotli/deflate, query-string encoding for GET DTOs, file upload helpers, and `System.Text.Json` parity with server options when you wire them.
- [Lyo.Api.Export](Lyo.Net/Integration/Api/Lyo.Api.Export/README.md): Optional export for Lyo.Api. Registers the Export CRUD endpoint and `IExportService<TContext>`.
- [Lyo.Api.Models](Lyo.Net/Integration/Api/Lyo.Api.Models/README.md): Shared HTTP contract models for Lyo minimal APIs and their clients. Distinct from `Lyo.Query.Models` (filter trees + projection DTOs).
- [Lyo.Api.Reporting](Lyo.Net/Integration/Api/Lyo.Api.Reporting/README.md): Authenticated HTTP endpoints for Lyo Reporting. Postgres stays service-only (`ReportService` + EF). This package owns `BuildReportingGroup`.
- [Lyo.Api.Tests.Host](Lyo.Net/Integration/Api/Lyo.Api.Tests.Host/README.md): Reference ASP.NET Core minimal-API host used by `Lyo.Api.Tests` and other integration tests as a `WebApplicationFactory<Program>` target.
- [Lyo.Discord.Bot](Lyo.Net/Integration/Discord/Lyo.Discord.Bot/README.md): Library (not an executable) that runs a DSharpPlus Discord bot and upserts guild data into your Lyo API (`Lyo.Discord.Client` to PostgreSQL-backed `Discord/*` endpoints).
- [Lyo.Discord.Client](Lyo.Net/Integration/Discord/Lyo.Discord.Client/README.md): Typed HTTP client for the Discord REST endpoints exposed by `Lyo.Api` (the `Discord/*` group registered by `Lyo.Discord.Postgres`).
- [Lyo.Discord.Models](Lyo.Net/Integration/Discord/Lyo.Discord.Models/README.md): Wire-level DTOs and shared constants for the Discord integration. Used by `Lyo.Discord.Client` (typed HTTP client) and `Lyo.Discord.Postgres` (API host + persistence) so request and response shapes match.
- [Lyo.Discord.Postgres](Lyo.Net/Integration/Discord/Lyo.Discord.Postgres/README.md): PostgreSQL persistence and `Lyo.Api` endpoint mappings for Discord entities. Schema name is fixed to `discord` (`PostgresDiscordOptions.Schema`).
- [Lyo.Endato.Client](Lyo.Net/Integration/Endato/Lyo.Endato.Client/README.md): Typed HTTP client for the Endato data-enrichment REST API.
- [Lyo.Endato.Postgres](Lyo.Net/Integration/Endato/Lyo.Endato.Postgres/README.md): PostgreSQL schema and EF Core context for caching Endato Person Search (PS) and Contact Enrichment (CE) responses. Schema name is `endato`.
- [Lyo.Espn.Fantasy.Football.Client](Lyo.Net/Integration/Espn/Lyo.Espn.Fantasy.Football.Client/README.md): Typed read-only client for the ESPN fantasy football v3 API (`lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`).
- [Lyo.Google.Geolocation.Client](Lyo.Net/Integration/Google/Lyo.Google.Geolocation.Client/README.md): Google Maps REST client and `IGeolocationService` implementation.
- [Lyo.Job.Alerts](Lyo.Net/Integration/Job/Lyo.Job.Alerts/README.md): Hosted `JobAlertConsumer` that subscribes to the `job.notifications.alert` routing key on the `job.events` exchange, deserializes `JobAlertEvent` payloads, and dispatches them through `INotificationPublisher` and/or an optional HTTP webhook.
- [Lyo.Job.Client](Lyo.Net/Integration/Job/Lyo.Job.Client/README.md): Typed HTTP client for the Lyo Job API. Wraps `IApiClient` with run lifecycle methods (`StartAsync`, `LogAsync`, `FinishAsync`, `RequeueAsync`) and worker-instance endpoints from `Lyo.Job.Models.Constants.Rest.Job`.
- [Lyo.Job.Models](Lyo.Net/Integration/Job/Lyo.Job.Models/README.md): Shared DTOs, builders, enums, metrics constants, distributed-tracing helpers, and message-queue contracts for the Lyo job-management subsystem.
- [Lyo.Job.Postgres](Lyo.Net/Integration/Job/Lyo.Job.Postgres/README.md): PostgreSQL persistence and minimal-API host for the Lyo job-management subsystem.
- [Lyo.Job.Scheduler](Lyo.Net/Integration/Job/Lyo.Job.Scheduler/README.md): Hosted `JobScheduler` that polls the Job API for enabled definitions, evaluates schedules (misfire catch-up, blackout calendars, per-schedule time zones), and creates job runs via `IApiClient`.
- [Lyo.Job.SignalR](Lyo.Net/Integration/Job/Lyo.Job.SignalR/README.md): SignalR live job dashboard. `JobEventBroadcaster` subscribes to lifecycle and alert routing keys on the `job.events` exchange and pushes `JobHubEvent` records to connected `JobHub` clients.
- [Lyo.Job.Web.Components](Lyo.Net/Integration/Job/Lyo.Job.Web.Components/README.md): Blazor / MudBlazor dashboard for the Lyo job stack. Add `JobManagement` to a host page for Statistics, Definitions, Schedules, Runs (progress and SLA breach), worker registry, and workflow views.
- [Lyo.Job.Worker](Lyo.Net/Integration/Job/Lyo.Job.Worker/README.md): Worker SDK for the Lyo job system. Subclass `JobWorkerBase` and implement `ExecuteAsync(IJobWorkerContext)`. The base class consumes the priority-enabled worker-type queue.
- [Lyo.Reporting.Client](Lyo.Net/Integration/Reporting/Lyo.Reporting.Client/README.md): Typed HTTP client for the Lyo Reporting API (`netstandard2.0;net10.0`).
- [Lyo.Reporting.Models](Lyo.Net/Integration/Reporting/Lyo.Reporting.Models/README.md): Composition models, fluent builders, API contracts, and generation hooks for Lyo Reporting.
- [Lyo.Reporting.Postgres](Lyo.Net/Integration/Reporting/Lyo.Reporting.Postgres/README.md): PostgreSQL schema (`reporting`), EF migrations, CSV/XLSX/JSON renderers, `ReportService` generation pipeline, and `ReportRetentionService` cleanup.
- [Lyo.Reporting.Web](Lyo.Net/Integration/Reporting/Lyo.Reporting.Web/README.md): Blazor `ReportViewer`, business document templates, and an `IReportRenderer` that emits HTML and PDF.
- [Lyo.Reporting.Web.Components](Lyo.Net/Integration/Reporting/Lyo.Reporting.Web.Components/README.md): MudBlazor ops UI for Lyo Reporting: browse definitions, run reports, and view/download generations.
- [Lyo.Typecast.Client](Lyo.Net/Integration/Typecast/Lyo.Typecast.Client/README.md): Typecast API client for text-to-speech and voice management. `TypecastClient` extends `Lyo.Api.Client.ApiClient`, configures the `X-API-KEY` header from `TypecastClientOptions`, and exposes two…
- [Lyo.Web.Automation](Lyo.Net/Integration/Web/Automation/Lyo.Web.Automation/README.md): Shared browser automation models: element locators, JSON automation plans, session abstraction, and plan runners. No Playwright or Selenium types.
- [Lyo.Web.Automation.Playwright](Lyo.Net/Integration/Web/Automation/Lyo.Web.Automation.Playwright/README.md): Playwright implementation of the `Lyo.Web.Automation` abstractions: launches Chromium / Firefox / WebKit, manages session-scoped browser contexts, and matches the Selenium helpers.
- [Lyo.Web.Automation.Selenium](Lyo.Net/Integration/Web/Automation/Lyo.Web.Automation.Selenium/README.md): Selenium WebDriver implementation of the `Lyo.Web.Automation` abstractions: browser launch (Chrome / Edge / Firefox / Safari + Selenium Grid), session isolation, polling, tabs, frames, and plans.
- [Lyo.Web.Components](Lyo.Net/Integration/Web/Lyo.Web.Components/README.md): Blazor / MudBlazor components for Lyo web UI: data grid, query builder, change-tracking form, file upload, rich-text editor, JSON editor, text-diff viewer, and identifier workbench.
- [Lyo.Web.Components.Export](Lyo.Net/Integration/Web/Lyo.Web.Components.Export/README.md): Export menu items for Lyo data grids. Reference this package (plus optional format packages) and add items to `BulkExportControls`.
- [Lyo.Web.WebRenderer](Lyo.Net/Integration/Web/Renderer/Lyo.Web.WebRenderer/README.md): Server-side rendering of Razor components and HTML→PDF conversion. Razor rendering uses `Microsoft.AspNetCore.Components.Web.HtmlRenderer`; PDF conversion is driven by **PuppeteerSharp** against a…

### Apps

- [Lyo.Config.Api](Lyo.Net/Apps/Config/Lyo.Config.Api/README.md): HTTP host for central app configuration backed by PostgreSQL and `Lyo.Config`.
- [Lyo.Config.Api.Client](Lyo.Net/Apps/Config/Lyo.Config.Api.Client/README.md): Typed HTTP client for `Lyo.Config.Api`. Conditional app-config reads with `If-None-Match` / `?version` polling, optional `X-Api-Key`, and one DI extension.
- [Lyo.Config.Api.Host](Lyo.Net/Apps/Config/Lyo.Config.Api.Host/README.md): Standalone ASP.NET host for `Lyo.Config.Api`.
- [Lyo.Config.Api.Hosting](Lyo.Net/Apps/Config/Lyo.Config.Api.Hosting/README.md): Wires `IConfigApiClient` (`Lyo.Config.Api.Client`) into `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Options`. A `BackgroundService` polls a shared `ResolvedConfigRecord` ledger.
- [Lyo.Config.Api.Models](Lyo.Net/Apps/Config/Lyo.Config.Api.Models/README.md): Contracts for the Config HTTP API: `ConfigResolveOutcome`, `ConfigResolveConditionalResult`, and `HttpStatusDescriptor`.

### Security

- [Lyo.Authentication](Lyo.Net/Security/Authentication/Lyo.Authentication/README.md): Server-side authentication services for Lyo. Two coexisting bearer formats behind a single contract.
- [Lyo.Authentication.AspNetCore](Lyo.Net/Security/Authentication/Lyo.Authentication.AspNetCore/README.md): ASP.NET Core integration for `Lyo.Authentication`. Three schemes coexist behind a single dispatcher.
- [Lyo.Authentication.Client](Lyo.Net/Security/Authentication/Lyo.Authentication.Client/README.md): Consumer-side runtime for the Lyo BFF auth flow. Plugs a web host, typically a Blazor Server gateway or a server-rendered API consumer, into a Lyo authentication API without ever exposing tokens to the browser.
- [Lyo.Authentication.Google](Lyo.Net/Security/Authentication/Lyo.Authentication.Google/README.md): Google profile for `Lyo.Authentication.OpenIdConnect`. Registers `https://accounts.google.com` as a confidential OIDC client in the BFF login flow.
- [Lyo.Authentication.Keycloak](Lyo.Net/Security/Authentication/Lyo.Authentication.Keycloak/README.md): Keycloak profile for `Lyo.Authentication.OpenIdConnect`. Wires one or more Keycloak realms as confidential OIDC clients in the BFF login flow.
- [Lyo.Authentication.Models](Lyo.Net/Security/Authentication/Lyo.Authentication.Models/README.md): Wire-shape data for `Lyo.Authentication`. The half of the auth stack that's safe to ship to anyone, including Blazor WebAssembly clients.
- [Lyo.Authentication.OpenIdConnect](Lyo.Net/Security/Authentication/Lyo.Authentication.OpenIdConnect/README.md): OpenID Connect client base for Lyo. The Lyo API is the OIDC confidential client (BFF pattern). The frontend never sees the IdP and never receives tokens by URL fragment.
- [Lyo.Authentication.Postgres](Lyo.Net/Security/Authentication/Lyo.Authentication.Postgres/README.md): PostgreSQL persistence for `Lyo.Authentication`. Replaces the in-memory stores from the base lib with EF Core-backed implementations of `IApiTokenStore`, `IUserStore`, and `IExternalIdentityStore`.
- [Lyo.Authentication.Web.Components](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components/README.md): Host-agnostic Razor / MudBlazor components for Lyo authentication. Ships the Login, Auth Debug, and Profile pages plus the abstractions that the Server and Wasm host adapters implement.
- [Lyo.Authentication.Web.Components.Server](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components.Server/README.md): Blazor Server host adapter for `Lyo.Authentication.Web.Components`. Plugs the shared login / debug / profile pages into the BFF-cookie auth runtime in `Lyo.Authentication.Client`.
- [Lyo.Authentication.Web.Components.Wasm](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components.Wasm/README.md): Blazor WebAssembly host adapter for `Lyo.Authentication.Web.Components`. Implements the same login / debug / profile pages over a pure-browser auth flow. No consumer-side server, no HttpOnly cookie.
- [Lyo.ContentThreatScan](Lyo.Net/Security/ContentThreatScan/Lyo.ContentThreatScan/README.md): Heuristic scanning and numeric disposition scoring for readable text payloads: scripts, markup, suspicious SQL-ish patterns.
- [Lyo.ContentThreatScan.Intel](Lyo.Net/Security/ContentThreatScan/Lyo.ContentThreatScan.Intel/README.md): Optional `DefaultContentThreatReputationPipeline` for Malware Bazaar, VirusTotal, and `clamd` INSTREAM (TCP).
- [Lyo.Encryption](Lyo.Net/Security/Encryption/Lyo.Encryption/README.md): Authenticated encryption for .NET. AEAD, RSA hybrids, and envelope (two-key) flows with optional Lyo.KeyStore lookup.
- [Lyo.Encryption.AesCcm](Lyo.Net/Security/Encryption/Lyo.Encryption.AesCcm/README.md): AES-CCM authenticated encryption addon for `Lyo.Encryption`. Provides `AesCcmEncryptionService` (BouncyCastle-backed on all targets) and matching DI extensions.
- [Lyo.Encryption.AesSiv](Lyo.Net/Security/Encryption/Lyo.Encryption.AesSiv/README.md): AES-SIV (RFC 5297) deterministic authenticated encryption addon for `Lyo.Encryption`. Provides `AesSivEncryptionService` backed by `Dorssel.Security.Cryptography.AesExtra` and matching DI extensions.
- [Lyo.Encryption.XChaCha20Poly1305](Lyo.Net/Security/Encryption/Lyo.Encryption.XChaCha20Poly1305/README.md): XChaCha20-Poly1305 (24-byte nonce, 32-byte key) authenticated-encryption addon for `Lyo.Encryption`.
- [Lyo.Hashing](Lyo.Net/Security/Hashing/Lyo.Hashing/README.md): Digests (SHA-256/384/512), optional MD5 for non-security fingerprints only, non-cryptographic checksums (CRC-32/CRC-32C/CRC-64/Adler-32), hexadecimal encoding (`HexEncoding`), incremental hashing (`HashingStream`), sparse file fingerprints (`SparseFileFingerprinter`), and injectable `IHashingService` / `HashingService`.
- [Lyo.KeyStore](Lyo.Net/Security/KeyStore/Lyo.KeyStore/README.md): Key encryption key (KEK) storage and rotation contracts for `Lyo.Encryption`.
- [Lyo.KeyStore.Aws](Lyo.Net/Security/KeyStore/Lyo.KeyStore.Aws/README.md): `AwsKeyStore` takes an `IAmazonSecretsManager` client and a secret-name prefix. It implements `Lyo.KeyStore.IKeyStore` and `Lyo.KeyStore.IKeyInventoryStore`, so admin UIs and key-rotation jobs can encrypt against it and list `keyId`s and versions.

### Tools

- [Lyo.Cli](Lyo.Net/Tools/Lyo.Cli/README.md): Installable `lyo` command-line tool for encryption, encoding, compression, hashing, IDs, query build/exec, and CSV/XLSX.
- [Lyo.Preview](Lyo.Net/Tools/Lyo.Preview/README.md): Cross-platform preview in the system default browser. `BrowserPreview` starts an `HttpListener` on `127.0.0.1` (random free port), serves one byte buffer per call, opens the URL, and drops the entry after the browser fetches it.
- [Lyo.TestApi](Lyo.Net/Tools/Lyo.TestApi/README.md): Minimal-API host that backs `Lyo.TestGateway` and `Lyo.TestConsole`. Wires Lyo Postgres stores, the RabbitMQ job system, S3 file storage with two-key encryption, and the file-storage workbench endpoints.
- [Lyo.TestConsole](Lyo.Net/Tools/Lyo.TestConsole/README.md): Scratch host for exercising Lyo services from a long-lived `Microsoft.Extensions.Hosting` process.
- [Lyo.TestGateway](Lyo.Net/Tools/Lyo.TestGateway/README.md): Interactive Blazor Server workbench for the Lyo platform. About 30 routed test pages (cache, locks, file storage, PDF, and more) plus a thin proxy so each page can hit a remote API or in-process services.
- [Lyo.Tools.Postgres](Lyo.Net/Tools/Lyo.Tools.Postgres/README.md): Spectre.Console TUI for running and rolling back EF Core migrations against Lyo Postgres `DbContext`s, plus a couple of Bogus-powered seeders.

<!-- catalog:packages:end -->

### Load testing (k6)

- [k6 framework: Person Query API](k6/framework-person/README.md): k6 workloads and query shapes against `TestApi` persons.
- [K6 benchmark analysis](Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md): latest archived run metrics and comparison to common API stacks (Hasura/PostgREST, typical ORM
  APIs, etc.).

### Performance snapshots (latest archived runs)

| Suite                                                                                                  | Date       | Environment                                           | Headline results                                                                                                                                                                                                                                                                                                                                                      |
|--------------------------------------------------------------------------------------------------------|------------|-------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Compression** ([summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md))  | 2026-06-28 | .NET 10.0.9, Linux Mint 22.1, Core Ultra 7 155U       | LZ4 fastest compress @ 1 MB (**~128 µs**); Zstd fastest decompress (**~71 µs @ 1 MB**, **~13 ms @ 100 MB**); Zstd streaming compress **~31×** GZip @ 100 MB, **~5×** @ 1 GB                                                                                                                                                                                           |
| **Encryption** ([summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)) | 2026-06-30 | .NET 10.0.0, Ubuntu 24.04, Core Ultra 7 155U (AES-NI) | AES-GCM **906 µs / 614 µs** @ 1 MB; ChaCha **1.23 ms / 947 µs**; XChaCha **2.7 / 2.7 ms**; CCM **14 ms**; SIV **20 ms**; stream **~1.2 GB/s** @ 100 MB; hybrid **837 µs** enc @ 1 MB; RSA dec 1 MB **2.6 s**                                                                                                                                                          |
| **K6 Query API** ([analysis](Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md))                | 2026-07-27 | TestApi + PostgreSQL + k6 on same laptop              | Full 12-suite matrix (Query / QueryProject / root Query × load/stress/spike/soak): root Query fastest (**~31–50 ms p95** load/spike/soak, **~701 ms p95** stress); QueryProject close behind (**~42–65 ms p95**, **~434 ms** stress); full-entity Query has heavier tails (**~103 ms** load, **~1.32 s** stress); status/shape checks **100%** across ~1.35M requests |

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
