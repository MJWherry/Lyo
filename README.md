# LYO — **Library for Your Organization.**

This is a continual work-in-progress personal development workspace; it also serves as my portfolio for .NET libraries and related tooling.

This repository is a .NET-focused toolkit of libraries and apps for business data: APIs with a rich query model, durable file handling, document parsing, and cross-cutting
infrastructure (security, compression, observability, and more). Most code lives under [`Lyo.Net/`](Lyo.Net/).

**Note:** Generative AI tools were used to help build and maintain parts of this codebase where scale made that practical—notably complex numerical packages such as **Mathematics**
and **Scientific** (including their function libraries), **documentation** (including long-form package READMEs), **test** projects and libraries, and **some JavaScript** (for
example in load-testing scripts, Blazor companion scripts, or other web-related assets). Human review still applies; treat those areas with the same scrutiny you would for any
large or subtle code.

---

## Major capabilities

<!-- catalog:capabilities:start -->

These are the areas that tend to anchor product work; each links to deeper docs where they exist in-tree.

| Area                    | What it is                                                                                                                                                                                                                                                                                               | Documentation                                                                                                                                                                                                                                     |
|-------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **API & query** | Minimal APIs and CRUD on Entity Framework Core — typed and dynamic builders, result caching with auto-invalidation, nested WhereClause filters, projection, property-level patch, bulk with per-item fallback, and CSV/XLSX/JSON export. | [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md) · [Lyo.Query.Models](Lyo.Net/Data/Query/Lyo.Query.Models/README.md) |
| **Query client UI** | Blazor components (e.g. data grid) that speak the same query shapes as the API. | [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md) |
| **File storage** | Local, S3, and Azure Blob providers share save/stream/copy/download, staged upload, multipart, duplicate detection, and an optional compress+encrypt pipeline. | [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md) · [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md) · [Lyo.FileStorage.Blob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Blob/README.md) |
| **Cloud blob backends** | AWS S3–compatible and Azure Blob Storage implementations for the file storage abstractions. | [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md) · [Lyo.FileStorage.Blob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Blob/README.md) |
| **PDF** | Load PDFs and extract text via IPdfService: words/lines, bounding boxes, key–value and table-style extraction, merges. Blazor PDF annotator in Lyo.Pdf.Web.Components. | [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md) · [Lyo.Pdf.Web.Components](Lyo.Net/Data/Pdf/Lyo.Pdf.Web.Components/README.md) |
| **Encryption** | Authenticated encryption (AES-GCM, ChaCha, CCM, SIV, XChaCha), RSA/hybrid, envelope/two-key, keystore integration. | [Lyo.Encryption](Lyo.Net/Security/Encryption/Lyo.Encryption/README.md) · [benchmark summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md) |
| **Caching** | Local and Fusion-backed ICacheService, typed byte payloads, query cache tags for invalidation (with Lyo.Api). | [Lyo.Cache](Lyo.Net/Core/Cache/Lyo.Cache/README.md) |
| **Diagnostics** | Stack decoding, exception classification, breadcrumbs, in-memory error inbox, trace sanitisation—optional IPackageMetadataStore for namespace→package enrichment. | [Lyo.Diagnostic](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic/README.md) · [Lyo.Diagnostic.AspNetCore](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.AspNetCore/README.md) · [Lyo.PackageMetadata](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata/README.md) |
| **Content threat scan** | Heuristic scoring for readable text; optional Malware Bazaar, VirusTotal, and clamd reputation; composes with Lyo.FileStorage malware scanning. | [Lyo.ContentThreatScan](Lyo.Net/Security/ContentThreat/Lyo.ContentThreatScan/README.md) · [Lyo.ContentThreatScan.Intel](Lyo.Net/Security/ContentThreat/Lyo.ContentThreatScan.Intel/README.md) |
| **Hashing** | SHA-2 digests, MD5 for non-security fingerprints, hex helpers, stream hashing, DI-friendly IHashingService. | [Lyo.Hashing](Lyo.Net/Security/Hashing/Lyo.Hashing/README.md) |
| **Compression** | Ten codecs (LZ4, Zstd, Brotli, GZip, …), streams/files, size limits and bomb protections. | [Lyo Compression Library](Lyo.Net/Data/Compression/Lyo.Compression/README.md) · [benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md) |

<!-- catalog:capabilities:end -->

---

## Repository layout (high level)

| Path                                                               | Comment                                                                                                                                                                                                                                                                                                                           |
|--------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [`Lyo.Net/`](Lyo.Net/)                                             | Main .NET solution root: shared props, solution file, and libraries grouped by the subfolders below.                                                                                                                                                                                                                              |
| [`Lyo.Net/Core/`](Lyo.Net/Core/)                                   | Cross-cutting primitives: caching, diagnostics, validation, metrics, resilience, exceptions, common types, package metadata for diagnostics, math/science, people models, geolocation, webhooks, locks, scheduling, streams, date/time, audit, change tracking, health—domain-agnostic building blocks for the rest of the stack. |
| [`Lyo.Net/Data/`](Lyo.Net/Data/)                                   | Data handling and persistence helpers: file storage (local/S3/Azure Blob), compression, CSV/XLSX/PDF, images, Postgres migration helpers, **`Lyo.Query.Models`** shapes, QR codes, file-system watching, temporary IO, and related parsers/processors.                                                                            |
| [`Lyo.Net/Features/`](Lyo.Net/Features/)                           | Composable product features (often EF-backed): comments, notes, favorites, ratings, tags, typed config, contact forms, profanity filter, short URLs—meant to plug into host apps alongside Core and Data.                                                                                                                         |
| [`Lyo.Net/Apps/`](Lyo.Net/Apps/)                                   | Sample and reference HTTP hosts (for example centralized typed config backed by **`Lyo.Config`** and PostgreSQL—see **`Lyo.Config.Api`** packages).                                                                                                                                                                               |
| [`Lyo.Net/Integration/`](Lyo.Net/Integration/)                     | Application-facing integration: minimal APIs and query (`Lyo.Api`), Blazor web components and reporting, **browser automation** ([`Lyo.Web.Automation`](Lyo.Net/Integration/Web/Automation/README.md): Selenium / Playwright, JSON plans, background jobs, Discord bot—wires Core/Data/Features into runnable surfaces.           |
| [`Lyo.Net/docs/package-layout.md`](Lyo.Net/docs/package-layout.md) | **Package taxonomy** — where Core domains, Communication providers, and Integration vendor clients belong (archetypes A–E).                                                                                                                                                                                                       |
| [`Lyo.Net/Security/`](Lyo.Net/Security/)                           | Cryptography (`Lyo.Encryption`), **hashing** (`Lyo.Hashing`), **content-threat** heuristics and optional intel (`Lyo.ContentThreatScan*`), encryption benchmarks.                                                                                                                                                                 |
| [`Lyo.Net/Communication/`](Lyo.Net/Communication/)                 | Messaging and media delivery: SMTP email, SMS (including Twilio), and text-to-speech providers.                                                                                                                                                                                                                                   |
| [`Lyo.Net/Tools/`](Lyo.Net/Tools/)                                 | Host apps and utilities (e.g. gateway, test API/console) for trying components end-to-end.                                                                                                                                                                                                                                        |
| [`k6/`](k6/)                                                       | Load-testing scripts; see [k6 framework: Person Query API](k6/framework-person/README.md) and [K6 benchmark analysis](Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md).                                                                                                                                                  |

Individual projects are mostly **one folder per NuGet-style package** (e.g. `Lyo.Something`). The sections below list **every** in-repo `README.md` beside a library, grouped by
top-level area.

---

## All packages with READMEs

<!-- catalog:packages:start -->

### Communication

- [Lyo.Email](Lyo.Net/Communication/Email/Lyo.Email/README.md): A production-ready email service library for .NET with SMTP support, built on MailKit.
- [Lyo.Email.Models](Lyo.Net/Communication/Email/Lyo.Email.Models/README.md): Shared models, options, error codes, and event arguments for the `Lyo.Email` SMTP service.
- [Lyo.Email.Postgres](Lyo.Net/Communication/Email/Lyo.Email.Postgres/README.md): PostgreSQL schema and `EmailDbContext` for logging emails sent by `Lyo.Email`. This package does **not** subscribe to `EmailService` events — consumers handle the mapping and insertion (e.g.
- [Lyo.Email.Web.Components](Lyo.Net/Communication/Email/Lyo.Email.Web.Components/README.md): Blazor (MudBlazor) workbench component for sending email through an injected `IEmailService`.
- [Lyo.MessageQueue](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue/README.md): Portable **queue + exchange** abstraction (`IMqService`) so schedulers, workers, and gateways can compile against **one contract** while swapping RabbitMQ—or future brokers—behind…
- [Lyo.MessageQueue.RabbitMq](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md): Concrete `IMqService` (`RabbitMqService`) using `RabbitMQ.Client`, also surfaced as `IRabbitMqService` when you need RabbitMQ-specific knobs (exchanges) that are not part of the shared abstraction.
- [Lyo.MessageQueue.RabbitMq.Web.Components](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq.Web.Components/README.md): Reusable Blazor components for RabbitMQ-specific exchanges, bindings, and broker workbenches.
- [Lyo.MessageQueue.Web.Components](Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.Web.Components/README.md): Reusable Blazor components for provider-neutral message queue dashboards and workbenches.
- [Lyo.Sms](Lyo.Net/Communication/Sms/Lyo.Sms/README.md): A production-ready SMS library for .NET with extensible architecture for multiple providers.
- [Lyo.Sms.Models](Lyo.Net/Communication/Sms/Lyo.Sms.Models/README.md): Shared **domain types** for `Lyo.Sms`: payloads, paging, events, normalization, and base options. There is **no** SMS sending here—implementations live in provider packages (`Lyo.Sms.Twilio`, etc.).
- [Lyo.Sms.Postgres](Lyo.Net/Communication/Sms/Lyo.Sms.Postgres/README.md): **EF Core + PostgreSQL** persistence for **outbound SMS logs** (`SmsLogEntity`). This package does **not** send SMS; it wires a **`SmsDbContext`** so workers or gateways can persist send outcomes…
- [Lyo.Sms.Twilio](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio/README.md): A production-ready Twilio SMS/MMS service implementation for .NET, built on the extensible `Lyo.Sms` library.
- [Lyo.Sms.Twilio.Postgres](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio.Postgres/README.md): EF Core PostgreSQL persistence tailored for **Twilio-outbound (+ metadata)** traces: **`TwilioSmsDbContext`** and **`TwilioSmsLogEntity`**.
- [Lyo.Sms.Web.Components](Lyo.Net/Communication/Sms/Lyo.Sms.Web.Components/README.md): **Blazor (MudBlazor)** workbench UI for exercising an injected **`ISmsService`** (provider-neutral `Result<SmsRequest>` surface). Depends on MudBlazor/snackbar primitives from `Lyo.Web.Components`.
- [Lyo.Stt](Lyo.Net/Communication/Speech/Lyo.Stt/README.md): Provider-agnostic Speech-to-Text **contract** for the Lyo stack. This package ships the interface (`ISttService`), an abstract base class (`SttServiceBase`), the request/result/options/event records…
- [Lyo.Translation](Lyo.Net/Communication/Translation/Lyo.Translation/README.md): **Archetype B (capability).** Providers (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) stay under `Communication/Translation/`, not `Integration/`. See package layout.
- [Lyo.Translation.Aws](Lyo.Net/Communication/Translation/Lyo.Translation.Aws/README.md): Amazon Translate implementation of `ITranslationService`: translate text, bounded **bulk** translation, pragmatic **language detection**, and **`ListLanguages`** connection checks.
- [Lyo.Translation.Google](Lyo.Net/Communication/Translation/Lyo.Translation.Google/README.md): Google Translate implementation of `ITranslationService` for the Lyo stack. `GoogleTranslationService` extends `TranslationServiceBase` and talks to the Google Cloud Translation v2 REST API over HTTP.
- [Lyo.Translation.Web.Components](Lyo.Net/Communication/Translation/Lyo.Translation.Web.Components/README.md): Blazor (MudBlazor) workbench component for exercising the configured `Lyo.Translation` implementation interactively from a host application.
- [Lyo.Tts](Lyo.Net/Communication/Speech/Lyo.Tts/README.md): Contracts and shared behaviour for text-to-speech in Lyo: provider-agnostic interfaces, a non-generic façade, and a base service with bulk synthesis, metrics hooks, and lifecycle events.
- [Lyo.Tts.AwsPolly](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly/README.md): Amazon Polly integration: `AwsPollyTtsService` extends `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk synthesis, metrics, and DI helpers.
- [Lyo.Tts.AwsPolly.Web.Components](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly.Web.Components/README.md): Blazor (MudBlazor) workbench component for trying out the `Lyo.Tts.AwsPolly` implementation interactively from a host application.
- [Lyo.Tts.Models](Lyo.Net/Communication/Speech/Lyo.Tts.Models/README.md): Shared **requests**, **results**, **options**, and **event payloads** for Lyo text-to-speech. Provider assemblies reference this package instead of coupling to each other.
- [Lyo.Tts.Typecast](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast/README.md): Lyo.Typecast.Client–backed synthesis: `TypecastTtsService` resolves audio via `TypecastClient`, supports optional voice catalog loading for validation (`LoadVoicesAsync`), bulk flows from `Lyo.Tts`…
- [Lyo.Tts.Typecast.Web.Components](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast.Web.Components/README.md): Blazor (MudBlazor) workbench component for exercising `Lyo.Tts.Typecast` interactively from a host application.
- [Lyo.Tts.WindowsSpeech](Lyo.Net/Communication/Speech/Lyo.Tts.WindowsSpeech/README.md): Windows Speech Synthesis Text-to-Speech service implementation for the Lyo framework using Windows built-in SAPI (Speech API).

### Core

- [Lyo.Audit](Lyo.Net/Core/Audit/Lyo.Audit/README.md): Audit trail library with two distinct concepts: **AuditChange** (entity change tracking) and **AuditEvent** (events to log).
- [Lyo.Audit.Postgres](Lyo.Net/Core/Audit/Lyo.Audit.Postgres/README.md): PostgreSQL implementation of Lyo.Audit using Entity Framework Core. Persists `AuditChange` and `AuditEvent` records to PostgreSQL with JSONB columns for dictionary data.
- [Lyo.Benchmark.Models](Lyo.Net/Core/Benchmark/Lyo.Benchmark.Models/README.md): Consumer-facing models and builders for the **unified Lyo benchmark report schema** (`lyo.bench/v1`).
- [Lyo.Benchmarking](Lyo.Net/Core/Benchmark/Lyo.Benchmarking/README.md): Benchmark-only helpers shared by every `*.Benchmarks` executable — the BenchmarkDotNet analogue of `Lyo.Testing`.
- [Lyo.Cache](Lyo.Net/Core/Cache/Lyo.Cache/README.md): Local and Fusion-backed **`ICacheService`** implementations with optional **typed byte payload** APIs for serializing values once, storing framed bytes (optional compression / encryption on .NET 10+)…
- [Lyo.Cache.Fusion](Lyo.Net/Core/Cache/Lyo.Cache.Fusion/README.md): **`FusionCacheService`** adapts **`ZiggyCreatures.FusionCache`** to **`ICacheService`** so application code (**`Lyo.Api`**, background workers, feature modules) can swap between * *purely in-memory**…
- [Lyo.ChangeTracker](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker/README.md): Generic entity change history built around `Lyo.EntityReference.Models.EntityRef`. Record property-level changes for any entity type without coupling the tracker to a specific aggregate.
- [Lyo.ChangeTracker.Postgres](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker.Postgres/README.md): PostgreSQL implementation of `Lyo.ChangeTracker`. Persists entity-scoped change history using `Lyo.EntityReference.Models.EntityRef` for both the target entity and the optional actor.
- [Lyo.Common](Lyo.Net/Core/Common/Lyo.Common/README.md): Cross-cutting primitives shared across the Lyo library suite: ID generators, file/MIME/language/HTTP/file-size metadata, geometry, secure RNG, typed extension classes, and shared `System.Text.Json`…
- [Lyo.DateAndTime](Lyo.Net/Core/DateAndTime/Lyo.DateAndTime/README.md): Date, time, US timezone conversion, day-of-week scheduling, and US holiday metadata for .NET. The API is **static** and **thread-safe** (no mutable shared state).
- [Lyo.Diagnostic](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic/README.md): Diagnostic utilities: stack trace decoding, exception classification, **breadcrumb trails**, an **in-memory error inbox**, sanitisation, and structured logging for observability.
- [Lyo.Diagnostic.AspNetCore](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.AspNetCore/README.md): ASP.NET Core integration for **Lyo.Diagnostic**: scoped **breadcrumb** trails per request and **exception recording** to the in-memory error inbox plus structured logging, without replacing your…
- [Lyo.Diagnostic.Web.Components](Lyo.Net/Core/Diagnostic/Lyo.Diagnostic.Web.Components/README.md): Blazor (Server / Interactive) workbench for analyzing and triaging .NET stack traces and exception payloads with `Lyo.Diagnostic`.
- [Lyo.Diff](Lyo.Net/Core/Diff/Lyo.Diff/README.md): Side-by-side comparison utilities for **human-readable text** and **arbitrary object graphs**.
- [Lyo.EntityReference.Models](Lyo.Net/Core/EntityReference/Lyo.EntityReference.Models/README.md): Portable primitives for **entity references** in Lyo: a typed pair of logical entity kind (`EntityType`) and identifier string (`EntityId`), plus helpers for composite keys, JSON, opaque tokens…
- [Lyo.EntityReference.Postgres](Lyo.Net/Core/EntityReference/Lyo.EntityReference.Postgres/README.md): Entity Framework Core building blocks for **relation** rows (subject/actor associations) and **source link** rows (import provenance) on PostgreSQL.
- [Lyo.Exceptions](Lyo.Net/Core/Exceptions/Lyo.Exceptions/README.md): Custom exception types and argument validation helpers for the Lyo library suite. Used across all Lyo packages for consistent error handling and validation.
- [Lyo.Geolocation](Lyo.Net/Core/Geolocation/Lyo.Geolocation/README.md): Provider-agnostic geospatial operations and persistence contracts.
- [Lyo.Geolocation.Models](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Models/README.md): Neutral data contracts for `Lyo.Geolocation` and `Lyo.Geolocation.Postgres`.
- [Lyo.Geolocation.Postgres](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Postgres/README.md): PostgreSQL persistence for canonical geolocation data using Entity Framework Core.
- [Lyo.Health](Lyo.Net/Core/Health/Lyo.Health/README.md): Interface for services that can report their health. Services implement `IHealth` and expose health directly—no central health service.
- [Lyo.Lock](Lyo.Net/Core/Lock/Lyo.Lock/README.md): Key-based **exclusive locks** and **keyed semaphores** (bounded concurrency per key) with a small abstraction layer and in-memory implementations for a single process.
- [Lyo.Lock.Redis](Lyo.Net/Core/Lock/Lyo.Lock.Redis/README.md): Distributed implementation of `ILockService` using **Redis** and StackExchange.Redis. Use this when multiple app instances must exclude each other on the same logical key.
- [Lyo.Mathematics](Lyo.Net/Core/Mathematics/Lyo.Mathematics/README.md): C# **contracts** for the Lyo math stack: physical quantities as structs, 2D/3D vectors and small matrices, typed inputs/results for formulas, and a small **registry** for discoverability.
- [Lyo.Metrics](Lyo.Net/Core/Metrics/Lyo.Metrics/README.md): A flexible, thread-safe metrics library for .NET applications with support for multiple metric types and implementations.
- [Lyo.Metrics.OpenTelemetry](Lyo.Net/Core/Metrics/Lyo.Metrics.OpenTelemetry/README.md): OpenTelemetry implementation of `IMetrics` for exporting metrics to OpenTelemetry-compatible backends.
- [Lyo.Metrics.Statistics](Lyo.Net/Core/Metrics/Lyo.Metrics.Statistics/README.md): Statistical analysis extensions for `Lyo.Metrics` histograms. Provides percentile / quartile / moving-average / anomaly-detection helpers on top of the metrics primitives in `Lyo.Metrics`.
- [Lyo.Notification](Lyo.Net/Core/Notification/Lyo.Notification/README.md): In-process **publish/subscribe** for small domain events. It is **not** durable, **not** distributed, and **not** ordered across machines—only useful when every publisher and handler lives in the…
- [Lyo.PackageMetadata](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata/README.md): Multi-ecosystem **`PackageMetadata`** rows, **`PackageMetadataRegistration`**, **`IPackageMetadataStore`**, and **`PackageArtifactDigest`** helpers for correlating stack-trace namespaces with…
- [Lyo.PackageMetadata.Postgres](Lyo.Net/Core/PackageMetadata/Lyo.PackageMetadata.Postgres/README.md): EF Core persistence for **`Lyo.PackageMetadata.IPackageMetadataStore`**.
- [Lyo.People.Models](Lyo.Net/Core/People/Lyo.People.Models/README.md): People and person-related models for the Lyo library suite.
- [Lyo.People.Postgres](Lyo.Net/Core/People/Lyo.People.Postgres/README.md): PostgreSQL persistence for Lyo.People.Models using Entity Framework Core.
- [Lyo.Privacy](Lyo.Net/Core/Privacy/Lyo.Privacy/README.md): Redaction and sanitization for **free text**, **JSON**, and **XML**: emails, phones, payment-card-shaped numbers (Luhn) with optional **BIN allow/block lists**, **IBAN** (MOD-97), heuristic…
- [Lyo.Privacy.AspNetCore](Lyo.Net/Core/Privacy/Lyo.Privacy.AspNetCore/README.md): ASP.NET Core DI integration for `Lyo.Privacy`: registers `ITextRedactor` / `IStructuredRedactor`, binds `PrivacyRedactorOptions` from configuration, and supports keyed per-tenant or per-feature…
- [Lyo.Privacy.Web.Components](Lyo.Net/Core/Privacy/Lyo.Privacy.Web.Components/README.md): Blazor (Server / Interactive) workbench components for `Lyo.Privacy`. Lets operators preview, compare, and tune redaction policies without round-tripping through a host config edit.
- [Lyo.Resilience](Lyo.Net/Core/Resilience/Lyo.Resilience/README.md): A thin wrapper around Polly for resilience pipelines with configuration-from-appsettings support and built-in logging.
- [Lyo.Result](Lyo.Net/Core/Result/Lyo.Result/README.md): Railway-oriented **`Result` / `Result<T>`** and related types. This package is **orthogonal to** `Lyo.Common` **`Result`** (different namespace and design); many feature libraries pick…
- [Lyo.Schedule.Models](Lyo.Net/Core/Schedule/Lyo.Schedule.Models/README.md): DTO-only assembly that describes a schedule. Used by `Lyo.Scheduler`, `Lyo.Job.Postgres`, and any consumer that needs a transport-friendly representation of "when does this run".
- [Lyo.Schedule.Web.Components](Lyo.Net/Core/Schedule/Lyo.Schedule.Web.Components/README.md): Blazor component(s) for building and previewing `Lyo.Schedule.Models.ScheduleDefinition` values interactively.
- [Lyo.Scheduler](Lyo.Net/Core/Scheduler/Lyo.Scheduler/README.md): In-process scheduler service for executing actions at scheduled times. Supports **SetTimes**, **Interval**, **OneShot**, and **Cron** schedules (5- or 6-field expressions) with logging, metrics, and…
- [Lyo.Scheduler.Cache](Lyo.Net/Core/Scheduler/Lyo.Scheduler.Cache/README.md): Cache-backed `ISchedulerStateStore` for `Lyo.Scheduler`. Persists each schedule's `LastRunUtc` / `NextRunUtc` / state markers through `Lyo.Cache` so cron/interval/one-shot schedules survive process…
- [Lyo.Scientific](Lyo.Net/Core/Scientific/Lyo.Scientific/README.md): Scientific **domain models**, **reference datasets**, **SI-oriented unit helpers**, and **formula discovery** built on `Lyo.Mathematics`.
- [Lyo.Streams](Lyo.Net/Core/Streams/Lyo.Streams/README.md): Common stream implementations including **TeeStream**, **CountingStream**, **ProgressStream**, **ConcatenatedStream**, etc. (**incremental hashing** lives in **`Lyo.Hashing`**: * *`HashingStream`**).
- [Lyo.Testing](Lyo.Net/Core/Lyo.Testing/README.md): Part of the Lyo workspace: shared **xUnit v3** helpers for the rest of the solution — fluent `Should*` assertions, exception/collection helpers, polling-based asynchronous assertions, and an…
- [Lyo.Testing.Containers](Lyo.Net/Core/Lyo.Testing.Containers/README.md): xUnit v3 fixture helpers around **Testcontainers** so integration tests can spin up real backing services without hand-rolling lifecycle plumbing.
- [Lyo.Validation](Lyo.Net/Core/Validation/Lyo.Validation/README.md): `Lyo.Validation` contains reusable C# validators, fluent rule builders, validation attributes, and adapters that return structured `Lyo.Result.Result<T>` failures.
- [Lyo.Webhook](Lyo.Net/Core/Webhook/Lyo.Webhook/README.md): Inbound webhook verification for ASP.NET Core: **raw body + headers**, **HMAC helpers**, a **fluent `MapWebhook().Verify().Handle()`** pipeline, **`Lyo.Metrics` timings and counters**, and…
- [Lyo.Webhook.Twilio](Lyo.Net/Core/Webhook/Lyo.Webhook.Twilio/README.md): **Twilio** webhook signature validation for **`Lyo.Webhook`**: compares **`X-Twilio-Signature`** to an **HMAC-SHA1** (Base64) of the public request URL plus sorted **key+value** form parameters…

### Data

- [Lyo.Barcode](Lyo.Net/Data/Barcode/Lyo.Barcode/README.md): **Barcode generation and decoding abstractions** for Lyo: **`IBarcodeService`**, request/options models, and a fluent **`BarcodeBuilder`**.
- [Lyo.Barcode.Native](Lyo.Net/Data/Barcode/Lyo.Barcode.Native/README.md): Native **`IBarcodeService`** implementation for **`Lyo.Barcode`** with no third-party barcode generator dependency.
- [Lyo.Barcode.TestWorkbench.Web.Components](Lyo.Net/Data/Barcode/Lyo.Barcode.TestWorkbench.Web.Components/README.md): Thin **MudBlazor** wrapper that hosts **`<BarcodeWorkbench />`** from `Lyo.Barcode.Web.Components` inside an `MudContainer` for the Lyo gateway test harness.
- [Lyo.Barcode.Web.Components](Lyo.Net/Data/Barcode/Lyo.Barcode.Web.Components/README.md): Reusable **Blazor / MudBlazor** components for exercising the **`IBarcodeService`** surface from `Lyo.Barcode`.
- [Lyo Compression Library](Lyo.Net/Data/Compression/Lyo.Compression/README.md): A production-ready .NET compression library providing efficient, thread-safe compression with support for multiple algorithms, batch operations, and atomic file operations.
- [Lyo.Compression.BZip2](Lyo.Net/Data/Compression/Lyo.Compression.BZip2/README.md): BZip2 compression addon for `Lyo.Compression`. Registers a BZip2 `ICompressorFactory`.
- [Lyo.Compression.LZ4](Lyo.Net/Data/Compression/Lyo.Compression.LZ4/README.md): LZ4 compression addon for `Lyo.Compression`. Registers an `LZ4` `ICompressorFactory` backed by `EasyCompressor.LZ4`.
- [Lyo.Compression.LZMA](Lyo.Net/Data/Compression/Lyo.Compression.LZMA/README.md): LZMA compression addon for `Lyo.Compression`. Registers an LZMA `ICompressorFactory`.
- [Lyo.Compression.Snappier](Lyo.Net/Data/Compression/Lyo.Compression.Snappier/README.md): Snappy compression addon for `Lyo.Compression`. Registers a Snappier `ICompressorFactory`.
- [Lyo.Compression.XZ](Lyo.Net/Data/Compression/Lyo.Compression.XZ/README.md): XZ / LZMA2 compression addon for `Lyo.Compression`. Registers an XZ `ICompressorFactory`.
- [Lyo.Compression.Zstd](Lyo.Net/Data/Compression/Lyo.Compression.Zstd/README.md): Zstandard compression addon for `Lyo.Compression`. Registers a Zstd `ICompressorFactory`.
- [Lyo.Csv](Lyo.Net/Data/Csv/Lyo.Csv/README.md): CsvHelper-backed implementation of `Lyo.Csv.Models`. `CsvService` composes a `CsvWriter` and `CsvReader` to read and write CSV from files, streams, byte arrays, URLs, and `TextWriter`/`TextReader`.
- [Lyo.Csv.Models](Lyo.Net/Data/Csv/Lyo.Csv.Models/README.md): Interfaces and value types for the Lyo CSV stack. Defines the contract implemented by `Lyo.Csv` so consumers can depend on `ICsvService` / `ICsvReader` / `ICsvWriter` without pulling in CsvHelper…
- [Lyo.DataTable](Lyo.Net/Data/DataTable/Lyo.DataTable/README.md): Empty package placeholder reserving the `Lyo.DataTable` name. The runtime types (`DataTable`, `DataTableRow`, `DataTableBuilder`, cell types, HTML renderer) all live in `Lyo.DataTable.Models`.
- [Lyo.DataTable.Models](Lyo.Net/Data/DataTable/Lyo.DataTable.Models/README.md): Mutable in-memory data table with sparse columns, thin cells, an optional format map, fluent builders, and an HTML renderer.
- [Lyo.Ffmpeg](Lyo.Net/Data/Ffmpeg/Lyo.Ffmpeg/README.md): FFmpeg integration for .NET. Wraps the `ffmpeg` / `ffprobe` / `ffplay` CLIs (via **CliWrap**) behind three contracts from `Lyo.Ffmpeg.Models`: **`IAudioPlayer`**, **`IAudioProber`**…
- [Lyo.Ffmpeg.Models](Lyo.Net/Data/Ffmpeg/Lyo.Ffmpeg.Models/README.md): Engine-neutral **contracts and models** for `Lyo.Ffmpeg`: the three service interfaces (`IAudioPlayer`, `IAudioProber`, `IAudioConverter`), their request/options shapes (`AudioConversionRequest`…
- [Lyo.FileMetadataStore](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore/README.md): **File identity without bytes.** Large systems split:
- [Lyo.FileMetadataStore.Postgres](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md): OLTP **`IFileMetadataStore`** plus adjunct services used by richer file pipelines:
- [Lyo.FileMetadataStore.Sqlite](Lyo.Net/Data/FileMetadataStore/Lyo.FileMetadataStore.Sqlite/README.md): SQLite implementation of **`IFileMetadataStore`** using Entity Framework Core. Functional parity with `Lyo.FileMetadataStore.Postgres` for embedded, offline-first, and local-dev scenarios.
- [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md): Production-oriented **file storage** for .NET: save / stream-save / read / delete / metadata with optional **compression** ( `Lyo.Compression`), **two-key encryption** (`Lyo.Encryption`), **duplicate…
- [Lyo.FileStorage.Blob](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Blob/README.md): **Azure Blob Storage** implementation of `IFileStorageService` using **`Azure.Storage.Blobs`**.
- [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md): S3-compatible storage for **Lyo.FileStorage** (AWS S3, **Backblaze B2**, MinIO, etc.) via **AWSSDK.S3**.
- [Lyo.FileStorage.Web.Components](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Web.Components/README.md): Blazor (Server / Interactive) UI for **`Lyo.FileStorage`** — workbench grids and dialogs for exploring file metadata, generating download access links, and managing two-key encryption keys.
- [Lyo.FileSystemWatcher](Lyo.Net/Data/FileSystemWatcher/Lyo.FileSystemWatcher/README.md): A production-ready file system watcher library for .NET that provides reliable change detection using snapshot-based monitoring, debouncing, and hash-based move/rename detection.
- [Lyo.Formatter](Lyo.Net/Data/Formatter/Lyo.Formatter/README.md): **SmartFormat.NET**-backed templating for user-defined strings: named placeholders, lists, pluralization, and culture-aware formatting.
- [Lyo.IO.Temp](Lyo.Net/Data/IOTemp/Lyo.IO.Temp/README.md): Service for creating and managing temporary files and directories with session support, configurable naming, and overflow handling.
- [Lyo.Images](Lyo.Net/Data/Images/Lyo.Images/README.md): Production-ready **raster image processing** for .NET using **SixLabors.ImageSharp**.
- [Lyo.Images.Ocr](Lyo.Net/Data/Images/Lyo.Images.Ocr/README.md): Engine-agnostic **OCR contracts** for Lyo: **`IOcrEngine`**, request/response models, **Y-up pixel bounding boxes** (aligned with `BoundingBox2D`), coordinate helpers, and shared…
- [Lyo.Images.Ocr.Tesseract](Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract/README.md): **Tesseract** implementation of **`IOcrEngine`** from **`Lyo.Images.Ocr`**. Calls are **serialized** with an internal lock because native Tesseract instances are not safely concurrent.
- [Lyo.Images.OpenCv](Lyo.Net/Data/Images/Lyo.Images.OpenCv/README.md): OpenCV helpers for .NET via **OpenCvSharp4**, kept separate from higher-level pipelines (e.g. comic overlay) so hosts can reference native OpenCV only where needed.
- [Lyo.Images.Skia](Lyo.Net/Data/Images/Lyo.Images.Skia/README.md): **SkiaSharp** implementation of **`IImageService`** from `Lyo.Images`: resize, crop, rotate, watermark, format conversion, thumbnails, compression, metadata (with optional **MetadataExtractor**-based…
- [Lyo.Images.Web.Components](Lyo.Net/Data/Images/Lyo.Images.Web.Components/README.md): Reusable **Blazor / MudBlazor** components for exercising `Lyo.Images`: an `IImageService` workbench and a spritesheet animator/extractor built on `ISpriteSheetExportService`.
- [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md): PdfPig-backed reading and PDFsharp-backed editing for `Lyo.Pdf.Models`. `PdfService` is the entry point; it returns disposable `IPdfReader` instances for read/extract workflows and `IPdfWriter`…
- [Lyo.Pdf.Models](Lyo.Net/Data/Pdf/Lyo.Pdf.Models/README.md): Interfaces and value types for the Lyo PDF stack. Defines the contracts implemented by `Lyo.Pdf` so consumers can depend on `IPdfService`, `IPdfReader`, `IPdfWriter`, and `ITextExtractor` without…
- [Lyo.Pdf.Ocr](Lyo.Net/Data/Pdf/Lyo.Pdf.Ocr/README.md): Glues `Lyo.Pdf.Rendering` (PDFium → PNG) to an `IOcrEngine` from `Lyo.Images.Ocr` and projects OCR pixel boxes back into PDF coordinate space.
- [Lyo.Pdf.Rendering](Lyo.Net/Data/Pdf/Lyo.Pdf.Rendering/README.md): Rasterizes PDF pages to PNG via PDFtoImage (PDFium + Skia under the hood; `bblanchon.PDFium` native packages). Targets `net10.0`.
- [Lyo.Pdf.Web.Components](Lyo.Net/Data/Pdf/Lyo.Pdf.Web.Components/README.md): Reusable Blazor / MudBlazor components for PDF workflows: an HTML → PDF workbench, a PDF annotation workbench, and a low-level annotator (`LyoPdfAnnotator`) that lets end-users draw bounding-box…
- [Lyo.Postgres](Lyo.Net/Data/Postgres/Lyo.Postgres/README.md): Shared PostgreSQL migration plumbing for Lyo libraries that ship their own EF Core schema (Audit, Email, ChangeTracker, EntityReference, etc.).
- [Lyo.QRCode](Lyo.Net/Data/QRCode/Lyo.QRCode/README.md): **QR code generation and reading** for Lyo: **`IQRCodeService`**, **`QRCodeBuilder`**, ISO **Model 2** encoding in-box (**`BuiltInQRCodeService`**), optional **QRCoder** adapter package…
- [Lyo.QRCode.QRCoder](Lyo.Net/Data/QRCode/Lyo.QRCode.QRCoder/README.md): **QRCoder**-backed implementation of **`IQRCodeService`** from `Lyo.QRCode`. Pick this when you need **JPEG / Bitmap** output on Windows or want to use QRCoder's mature renderers; pick the built-in…
- [Lyo.QRCode.Web.Components](Lyo.Net/Data/QRCode/Lyo.QRCode.Web.Components/README.md): Reusable **Blazor** components for QR code generation and preview workflows (**MudBlazor**).
- [Lyo.Query](Lyo.Net/Data/Query/Lyo.Query/README.md): WhereClause AST → LINQ on IQueryable: filter, multi-key sort, in-memory match/explain, with ICache-backed compiled predicates.
- [Lyo.Query.Models](Lyo.Net/Data/Query/Lyo.Query.Models/README.md): Filter / sort / projection DTOs and fluent builders (`WhereClause`, QueryConcrete / QueryProject / root Query) shared by Lyo.Query and Lyo.Api.
- [Lyo.Query.Web.Components](Lyo.Net/Data/Query/Lyo.Query.Web.Components/README.md): Reusable Blazor / MudBlazor components for editing and running `Lyo.Query.Models` requests against any Lyo.Api host.
- [Lyo.Sqlite](Lyo.Net/Data/Sqlite/Lyo.Sqlite/README.md): Shared SQLite migration plumbing for Lyo libraries that ship their own EF Core schema.
- [Lyo.Xlsx](Lyo.Net/Data/Xlsx/Lyo.Xlsx/README.md): Implementation of `Lyo.Xlsx.Models`. `XlsxService` composes an `XlsxWriter` (streaming `DocumentFormat.OpenXml` writer) and an `XlsxReader` (ExcelDataReader / ClosedXML) to read and write XLSX…
- [Lyo.Xlsx.Models](Lyo.Net/Data/Xlsx/Lyo.Xlsx.Models/README.md): Interfaces and value types for the Lyo XLSX stack. Defines the contract implemented by `Lyo.Xlsx` so consumers can depend on `IXlsxService` / `IXlsxReader` / `IXlsxWriter` without pulling in ClosedXML…

### Features

- [Lyo.Comic](Lyo.Net/Features/Comic/Lyo.Comic/README.md): Domain contracts for a **serialized fiction catalog**: series (**`ComicSeries`**, **`ComicAlternateTitle`**), hierarchical organization (**`ComicVolume`**, **`ComicChapter`**, * *`ComicPage`**), cast…
- [Lyo.Comic.Postgres](Lyo.Net/Features/Comic/Lyo.Comic.Postgres/README.md): PostgreSQL + EF Core implementation of **`Lyo.Comic.IComicStore`** (`PostgresComicStore`) backed by **`ComicDbContext`**, **`PostgresComicOptions`**, and `AddPostgresMigrations<ComicDbContext…
- [Lyo.Comic.Web.Components](Lyo.Net/Features/Comic/Lyo.Comic.Web.Components/README.md): Reusable Blazor components for browsing, previewing, and reading comic series — search panel, result grids/lists, browse cards, and a MangaFire-style tap-to-navigate reader.
- [Lyo.Comment](Lyo.Net/Features/Comment/Lyo.Comment/README.md): Abstractions for attaching threaded, reactable comments to any entity. Each comment carries a **subject** (what it is about), an **actor** (author), optional **ReplyToCommentId** for threads, and…
- [Lyo.Comment.Postgres](Lyo.Net/Features/Comment/Lyo.Comment.Postgres/README.md): PostgreSQL implementation of `Lyo.Comment` using Entity Framework Core. Persists comments to the `comment.comment` table and reactions to `comment.comment_reaction` (schema constant…
- [Lyo.Config](Lyo.Net/Features/Config/Lyo.Config/README.md): Typed, definition-driven configuration for **per-entity** values (e.g. a Discord guild, a tenant). The abstract API lives here; **PostgreSQL** persistence is in `Lyo.Config.Postgres`.
- [Lyo.Config.Postgres](Lyo.Net/Features/Config/Lyo.Config.Postgres/README.md): PostgreSQL + EF Core implementation of `Lyo.Config.IConfigStore` for storing typed configuration definitions and per-entity bindings.
- [Lyo.ContactUs](Lyo.Net/Features/ContactUs/Lyo.ContactUs/README.md): Core abstractions for contact-form submission. The interface (`IContactUsService`) and a `ContactUsServiceBase` that handles validation, error-code mapping, and logging live here; concrete storage…
- [Lyo.ContactUs.Postgres](Lyo.Net/Features/ContactUs/Lyo.ContactUs.Postgres/README.md): PostgreSQL + EF Core implementation of `Lyo.ContactUs.IContactUsService` (`PostgresContactUsService`) backed by `ContactUsDbContext`, `PostgresContactUsOptions`, and…
- [Lyo.Favorite](Lyo.Net/Features/Favorite/Lyo.Favorite/README.md): Abstractions for "X favorited Y" relationships across any two entities. The API accepts `EntityRef` at the boundary (so any feature can produce a favorite); the default Postgres store persists…
- [Lyo.Favorite.Postgres](Lyo.Net/Features/Favorite/Lyo.Favorite.Postgres/README.md): PostgreSQL implementation of `Lyo.Favorite` using Entity Framework Core. Persists favorites to the `favorite.favorite` table (schema constant: `PostgresFavoriteOptions.Schema = "favorite"`) with…
- [Lyo.HomeInventory](Lyo.Net/Features/HomeInventory/Lyo.HomeInventory/README.md): Portable contract for **household inventory** — large purchases (electronics, appliances) with warranty tracking, kitchen consumables stocked across pantries / freezers, and bin locations in garages.
- [Lyo.HomeInventory.Postgres](Lyo.Net/Features/HomeInventory/Lyo.HomeInventory.Postgres/README.md): EF Core implementation of `IHomeInventoryStore` backed by PostgreSQL.
- [Lyo.Note](Lyo.Net/Features/Note/Lyo.Note/README.md): Abstractions for storing and retrieving notes attached to arbitrary entities. Each note has a **subject** (what it is about) and an **actor** (who wrote it), expressed as `EntityRef` at the API.
- [Lyo.Note.Postgres](Lyo.Net/Features/Note/Lyo.Note.Postgres/README.md): PostgreSQL implementation of `Lyo.Note` using Entity Framework Core. Persists notes to the `note.note` table (schema constant: `PostgresNoteOptions.Schema = "note"`) with migrations support.
- [Lyo.Profanity](Lyo.Net/Features/Profanity/Lyo.Profanity/README.md): File-based profanity filter service that detects and replaces profane words in text. Supports multiple languages, regex patterns, plain word lists, and configurable replacement strategies.
- [Lyo.Rating](Lyo.Net/Features/Rating/Lyo.Rating/README.md): Abstractions for rating and reviewing arbitrary entities, plus like/dislike reactions on those ratings.
- [Lyo.Rating.Postgres](Lyo.Net/Features/Rating/Lyo.Rating.Postgres/README.md): PostgreSQL implementation of `Lyo.Rating` using Entity Framework Core. Persists ratings to the `rating.rating` table and reactions to `rating.rating_reaction` (schema constant…
- [Lyo.ShortUrl](Lyo.Net/Features/ShortUrl/Lyo.ShortUrl/README.md): Core abstractions for URL shortening: an `IShortUrlService` contract, a `ShortUrlServiceBase` that handles validation / metrics / error-code mapping, a default `ShortUrlService` that **generates**…
- [Lyo.ShortUrl.Postgres](Lyo.Net/Features/ShortUrl/Lyo.ShortUrl.Postgres/README.md): EF Core schema and DbContext registration for a PostgreSQL-backed short-URL store.
- [Lyo.Tag](Lyo.Net/Features/Tag/Lyo.Tag/README.md): Abstractions for tagging arbitrary entities. Tags are keyed off an `EntityRef` (what is tagged) and optionally a second `EntityRef` (who applied the tag), so any feature in the framework can attach…
- [Lyo.Tag.Postgres](Lyo.Net/Features/Tag/Lyo.Tag.Postgres/README.md): PostgreSQL implementation of `Lyo.Tag` using Entity Framework Core. Persists tags to the `tag.tag` table (schema constant: `PostgresTagOptions.Schema = "tag"`) with migrations support.

### Integration

- [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md): Core API library for building RESTful minimal APIs with Entity Framework Core. Provides a fluent `ApiEndpointBuilder` to generate CRUD endpoints with caching, **`ILyoMapper`** -based DTO mapping…
- [Lyo.Api.Client](Lyo.Net/Integration/Api/Lyo.Api.Client/README.md): HTTP client tailored for **Lyo-shaped minimal APIs**: JSON in/out, gzip/brotli/deflate handling, **query-string encoding** for GET DTOs, file upload helpers, and * *`System.Text.Json`** parity with…
- [Lyo.Api.Export](Lyo.Net/Integration/Api/Lyo.Api.Export/README.md): Optional export feature for Lyo.Api. Registers the **Export** CRUD endpoint and `IExportService<TContext>`.
- [Lyo.Api.Models](Lyo.Net/Integration/Api/Lyo.Api.Models/README.md): Shared **HTTP contract** models for Lyo minimal APIs and their **clients**—distinct from `Lyo.Query.Models` (filter trees + projection DTOs).
- [Lyo.Api.Reporting](Lyo.Net/Integration/Api/Lyo.Api.Reporting/README.md): Authenticated HTTP surface for Lyo Reporting. Postgres stays service-only (`ReportService` + EF); this package owns `BuildReportingGroup`.
- [Lyo.Api.Tests.Host](Lyo.Net/Integration/Api/Lyo.Api.Tests.Host/README.md): Reference ASP.NET Core minimal-API host used by `Lyo.Api.Tests` and other integration tests as a `WebApplicationFactory<Program>` target.
- [Lyo.Discord.Bot](Lyo.Net/Integration/Discord/Lyo.Discord.Bot/README.md): Library (not an executable) that runs a **DSharpPlus** Discord bot and **upserts** guild data into your Lyo API (`Lyo.Discord.Client` → PostgreSQL-backed `Discord/*` endpoints).
- [Lyo.Discord.Client](Lyo.Net/Integration/Discord/Lyo.Discord.Client/README.md): Typed HTTP client for the Discord REST surface exposed by `Lyo.Api` (the `Discord/*` group registered by `Lyo.Discord.Postgres`).
- [Lyo.Discord.Models](Lyo.Net/Integration/Discord/Lyo.Discord.Models/README.md): Wire-level DTOs and shared constants for the Discord integration. Used by both `Lyo.Discord.Client` (typed HTTP client) and `Lyo.Discord.Postgres` (API host + persistence) so request/response shapes…
- [Lyo.Discord.Postgres](Lyo.Net/Integration/Discord/Lyo.Discord.Postgres/README.md): PostgreSQL persistence and `Lyo.Api` endpoint mappings for Discord entities. Schema name is fixed to `discord` (`PostgresDiscordOptions.Schema`).
- [Lyo.Endato.Client](Lyo.Net/Integration/Endato/Lyo.Endato.Client/README.md): Typed HTTP client for the Endato data-enrichment REST API.
- [Lyo.Endato.Postgres](Lyo.Net/Integration/Endato/Lyo.Endato.Postgres/README.md): PostgreSQL schema and EF Core context for caching Endato Person Search (PS) and Contact Enrichment (CE) responses. Schema name is `endato`.
- [Lyo.Espn.Fantasy.Football](Lyo.Net/Integration/Espn/Lyo.Espn.Fantasy.Football/README.md): Typed read-only client for the ESPN fantasy football v3 API (`lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`).
- [Lyo.Google.Geolocation.Client](Lyo.Net/Integration/Google/Lyo.Google.Geolocation.Client/README.md): Google Maps REST client and `IGeolocationService` implementation.
- [Lyo.Job.Alerts](Lyo.Net/Integration/Job/Lyo.Job.Alerts/README.md): Hosted **`JobAlertConsumer`** that subscribes to the `job.notifications.alert` routing key on the `job.events` exchange, deserializes **`JobAlertEvent`** payloads, and dispatches them through…
- [Lyo.Job.Client](Lyo.Net/Integration/Job/Lyo.Job.Client/README.md): Typed HTTP client for the Lyo Job API. Wraps `IApiClient` with run lifecycle and worker-instance endpoints from `Lyo.Job.Models.Constants.Rest.Job`.
- [Lyo.Job.Models](Lyo.Net/Integration/Job/Lyo.Job.Models/README.md): Shared DTOs, builders, enums, metrics constants, distributed-tracing helpers, and message-queue contracts for the Lyo job-management subsystem.
- [Lyo.Job.Postgres](Lyo.Net/Integration/Job/Lyo.Job.Postgres/README.md): PostgreSQL persistence and minimal-API host for the Lyo job-management subsystem.
- [Lyo.Job.Scheduler](Lyo.Net/Integration/Job/Lyo.Job.Scheduler/README.md): Hosted `JobScheduler` that polls the Job API for enabled definitions, evaluates schedules (with misfire catch-up, blackout calendars, and per-schedule time zones), creates job runs via `IApiClient`…
- [Lyo.Job.SignalR](Lyo.Net/Integration/Job/Lyo.Job.SignalR/README.md): SignalR **live job dashboard** for the Lyo job stack. `JobEventBroadcaster` subscribes to lifecycle and alert routing keys on the `job.events` exchange and pushes **`JobHubEvent`** records to all…
- [Lyo.Job.Web.Components](Lyo.Net/Integration/Job/Lyo.Job.Web.Components/README.md): Blazor / MudBlazor dashboard for the Lyo job-management stack. Drop `JobManagement` into a host page for Statistics, Definitions, Schedules, Runs (with **progress** and **SLA breach** indicators)…
- [Lyo.Job.Worker](Lyo.Net/Integration/Job/Lyo.Job.Worker/README.md): Worker SDK for the Lyo job system. Subclass `JobWorkerBase` and implement a single `ExecuteAsync(IJobWorkerContext)` method — the base class consumes the priority-enabled worker-type queue…
- [Lyo.Reporting.Client](Lyo.Net/Integration/Reporting/Lyo.Reporting.Client/README.md): Typed HTTP client for the Lyo Reporting API (`netstandard2.0;net10.0`).
- [Lyo.Reporting.Models](Lyo.Net/Integration/Reporting/Lyo.Reporting.Models/README.md): Composition models, fluent builders, API contracts, and generation hooks for Lyo Reporting.
- [Lyo.Reporting.Postgres](Lyo.Net/Integration/Reporting/Lyo.Reporting.Postgres/README.md): PostgreSQL schema (`reporting`), EF migrations, CSV/XLSX/JSON renderers, `ReportService` generation pipeline, and `ReportRetentionService` cleanup.
- [Lyo.Reporting.Web](Lyo.Net/Integration/Reporting/Lyo.Reporting.Web/README.md): Blazor `ReportViewer`, business document templates, and HTML/PDF `IReportRenderer` implementation.
- [Lyo.Reporting.Web.Components](Lyo.Net/Integration/Reporting/Lyo.Reporting.Web.Components/README.md): MudBlazor ops UI for Lyo Reporting: browse definitions, run reports, and view/download generations.
- [Lyo.Typecast.Client](Lyo.Net/Integration/Typecast/Lyo.Typecast.Client/README.md): Typecast API client for text-to-speech and voice management. `TypecastClient` extends `Lyo.Api.Client.ApiClient`, configures the `X-API-KEY` header from `TypecastClientOptions`, and exposes two…
- [Lyo.Web.Automation](Lyo.Net/Integration/Web/Automation/Lyo.Web.Automation/README.md): Shared browser automation models: element locators, JSON automation plans, session abstraction, and plan runners (engine-neutral).
- [Lyo.Web.Automation.Playwright](Lyo.Net/Integration/Web/Automation/Lyo.Web.Automation.Playwright/README.md): Playwright implementation of the engine-agnostic `Lyo.Web.Automation` abstractions: launches Chromium / Firefox / WebKit, manages session-scoped browser contexts, and exposes the same…
- [Lyo.Web.Automation.Selenium](Lyo.Net/Integration/Web/Automation/Lyo.Web.Automation.Selenium/README.md): Selenium WebDriver implementation of the engine-agnostic `Lyo.Web.Automation` abstractions: browser launch (Chrome / Edge / Firefox / Safari + Selenium Grid), session isolation, polling…
- [Lyo.Web.Components](Lyo.Net/Integration/Web/Lyo.Web.Components/README.md): Blazor / MudBlazor component library for the Lyo web UI. Provides the data-grid + query-builder stack, change-tracking form, file upload, rich-text editor, JSON editor, text-diff viewer, identifier…
- [Lyo.Web.Components.Export](Lyo.Net/Integration/Web/Lyo.Web.Components.Export/README.md): Composable export UI for Lyo data grids. Reference this package (plus optional format packages) and add menu items to `BulkExportControls`.
- [Lyo.Web.WebRenderer](Lyo.Net/Integration/Web/Renderer/Lyo.Web.WebRenderer/README.md): Server-side rendering of Razor components and HTML→PDF conversion. Razor rendering uses `Microsoft.AspNetCore.Components.Web.HtmlRenderer`; PDF conversion is driven by **PuppeteerSharp** against a…

### Apps

- [Lyo.Comic.Api](Lyo.Net/Apps/Comic/Lyo.Comic.Api/README.md): ASP.NET Core **Minimal API** composition for the comic domain: **series, volumes, chapters, pages, characters**, plus **tags, ratings, comments, favorites**, **binary file upload/download** with…
- [Lyo.Comic.Api.Client](Lyo.Net/Apps/Comic/Lyo.Comic.Api.Client/README.md): Typed **`HttpClient`** for the `Lyo.Comic.Api` service. Wraps the upload / download / batch / tag endpoints behind **`IComicApiClient`** and a single **`AddComicApiClientFromConfiguration`** DI…
- [Lyo.Comic.Api.Models](Lyo.Net/Apps/Comic/Lyo.Comic.Api.Models/README.md): Request and response DTOs shared between `Lyo.Comic.Api` and `Lyo.Comic.Api.Client`.
- [Lyo.Config.Api](Lyo.Net/Apps/Config/Lyo.Config.Api/README.md): HTTP host for central **app** configuration backed by PostgreSQL and `Lyo.Config`.
- [Lyo.Config.Api.Client](Lyo.Net/Apps/Config/Lyo.Config.Api.Client/README.md): Typed HTTP client for the central `Lyo.Config.Api` — **conditional** app-config reads with **`If-None-Match`** / **`?version`** polling, an optional **`X-Api-Key`** header, and a single DI extension.
- [Lyo.Config.Api.Hosting](Lyo.Net/Apps/Config/Lyo.Config.Api.Hosting/README.md): Bridges **`IConfigApiClient`** (`Lyo.Config.Api.Client`) into **`Microsoft.Extensions.DependencyInjection`** and * *`Microsoft.Extensions.Options`**: a **`BackgroundService`** keeps a shared…
- [Lyo.Config.Api.Models](Lyo.Net/Apps/Config/Lyo.Config.Api.Models/README.md): Thin **contracts** for calling the central Config HTTP API: **`ConfigResolveOutcome`**, **`ConfigResolveConditionalResult`**, and **`HttpStatusDescriptor`**.
- [Portfolio (Lyo.Portfolio)](Lyo.Net/Apps/Portfolio/README.md): Blazor WebAssembly showcase for the in-process subset of Lyo: components that work without any backend (cache, locks, scheduler, encryption + compression, CSV/XLSX, text diff, rich text editor, etc.)…

### Security

- [Lyo.Authentication](Lyo.Net/Security/Authentication/Lyo.Authentication/README.md): Server-side authentication services for Lyo. Two coexisting bearer formats behind a single contract:
- [Lyo.Authentication.AspNetCore](Lyo.Net/Security/Authentication/Lyo.Authentication.AspNetCore/README.md): ASP.NET Core integration for `Lyo.Authentication`. Three schemes coexist behind a single dispatcher:
- [Lyo.Authentication.Client](Lyo.Net/Security/Authentication/Lyo.Authentication.Client/README.md): Consumer-side runtime for the Lyo BFF auth flow. Plugs a web host (typically a Blazor Server gateway or a server-rendered API consumer) into a Lyo authentication API without ever exposing tokens to…
- [Lyo.Authentication.Google](Lyo.Net/Security/Authentication/Lyo.Authentication.Google/README.md): Google profile for `Lyo.Authentication.OpenIdConnect`. Registers `https://accounts.google.com` as a confidential OIDC client in the BFF login flow.
- [Lyo.Authentication.Keycloak](Lyo.Net/Security/Authentication/Lyo.Authentication.Keycloak/README.md): Keycloak profile for `Lyo.Authentication.OpenIdConnect`. Wires one or more Keycloak realms as confidential OIDC clients in the BFF login flow.
- [Lyo.Authentication.Models](Lyo.Net/Security/Authentication/Lyo.Authentication.Models/README.md): Wire-shape data for `Lyo.Authentication` — the half of the auth stack that's safe to ship to anyone, including Blazor WebAssembly clients.
- [Lyo.Authentication.OpenIdConnect](Lyo.Net/Security/Authentication/Lyo.Authentication.OpenIdConnect/README.md): OpenID Connect client base for Lyo. The Lyo API is the OIDC **confidential client** (BFF pattern); the frontend never sees the IdP and never receives tokens by URL fragment.
- [Lyo.Authentication.Postgres](Lyo.Net/Security/Authentication/Lyo.Authentication.Postgres/README.md): PostgreSQL persistence for `Lyo.Authentication`. Replaces the in-memory stores from the base lib with EF Core-backed implementations of `IApiTokenStore`, `IUserStore`, and `IExternalIdentityStore`.
- [Lyo.Authentication.Web.Components](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components/README.md): Host-agnostic Razor / MudBlazor components for Lyo authentication. Ships the **Login**, **Auth Debug**, and **Profile** pages plus the abstractions that the host adapter (…
- [Lyo.Authentication.Web.Components.Server](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components.Server/README.md): Blazor Server host adapter for `Lyo.Authentication.Web.Components`. Plugs the shared login / debug / profile pages into the BFF-cookie auth runtime in `Lyo.Authentication.Client`.
- [Lyo.Authentication.Web.Components.Wasm](Lyo.Net/Security/Authentication/Lyo.Authentication.Web.Components.Wasm/README.md): Blazor WebAssembly host adapter for `Lyo.Authentication.Web.Components`. Implements the same login / debug / profile pages over a **pure-browser** auth flow — no consumer-side server, no HttpOnly…
- [Lyo.ContentThreatScan](Lyo.Net/Security/ContentThreat/Lyo.ContentThreatScan/README.md): Heuristic scanning and numeric disposition scoring for **readable text** payloads (scripts, markup, suspicious SQL-ish patterns).
- [Lyo.ContentThreatScan.Intel](Lyo.Net/Security/ContentThreat/Lyo.ContentThreatScan.Intel/README.md): Optional **`DefaultContentThreatReputationPipeline`** for Malware Bazaar, VirusTotal, and **`clamd` INSTREAM** (TCP).
- [Lyo.Encryption](Lyo.Net/Security/Encryption/Lyo.Encryption/README.md): Production-oriented **authenticated encryption** for .NET: symmetric AEAD (**AES-GCM**, **ChaCha20-Poly1305**, **XChaCha20-Poly1305**, **AES-CCM**, **AES-SIV**), **RSA** and * *AES-GCM + RSA**…
- [Lyo.Encryption.AesCcm](Lyo.Net/Security/Encryption/Lyo.Encryption.AesCcm/README.md): AES-CCM authenticated encryption addon for `Lyo.Encryption`. Provides `AesCcmEncryptionService` (BouncyCastle-backed on all targets) and matching DI extensions.
- [Lyo.Encryption.AesSiv](Lyo.Net/Security/Encryption/Lyo.Encryption.AesSiv/README.md): AES-SIV (RFC 5297) deterministic authenticated encryption addon for `Lyo.Encryption`. Provides `AesSivEncryptionService` backed by `Dorssel.Security.Cryptography.AesExtra` and matching DI extensions.
- [Lyo.Encryption.XChaCha20Poly1305](Lyo.Net/Security/Encryption/Lyo.Encryption.XChaCha20Poly1305/README.md): XChaCha20-Poly1305 (24-byte nonce, 32-byte key) authenticated-encryption addon for `Lyo.Encryption`.
- [Lyo.Hashing](Lyo.Net/Security/Hashing/Lyo.Hashing/README.md): Digests (**SHA-256/384/512**), optional **MD5** (non-security fingerprints only), non-cryptographic checksums (**CRC-32/CRC-32C/CRC-64/Adler-32**), hexadecimal encoding (* *`HexEncoding`**)…
- [Lyo.Keystore](Lyo.Net/Security/Encryption/Lyo.Keystore/README.md): **Key Encryption Key (KEK)** storage and rotation contracts for `Lyo.Encryption`.
- [Lyo.Keystore.Aws](Lyo.Net/Security/Encryption/Lyo.Keystore.Aws/README.md): **`AwsKeyStore`** (an `IAmazonSecretsManager` client + secret-name prefix) implements both **`Lyo.Keystore.IKeyStore`** and **`Lyo.Keystore.IKeyInventoryStore`**, so admin UIs and key-rotation jobs…

### Tools

- [Lyo.Gateway](Lyo.Net/Tools/Lyo.Gateway/README.md): Interactive Blazor Server workbench for the Lyo platform. It hosts ~30 routed test pages (cache, locks, file storage, PDF, comics, etc.) and a thin proxy layer that lets every page run against either…
- [Lyo.Preview](Lyo.Net/Tools/Lyo.Preview/README.md): Cross-platform preview in the system default browser. The default implementation, `BrowserPreview`, spins up an `HttpListener` on `127.0.0.1` (random free port), registers one byte buffer per call…
- [Lyo.TestApi](Lyo.Net/Tools/Lyo.TestApi/README.md): Minimal-API host that backs `Lyo.Gateway` and `Lyo.TestConsole`. It wires the Lyo Postgres stores, RabbitMQ-driven job system, S3 file storage with two-key encryption, and exposes the file storage…
- [Lyo.TestConsole](Lyo.Net/Tools/Lyo.TestConsole/README.md): Ad-hoc scratch host used to exercise Lyo services from a long-lived `Microsoft.Extensions.Hosting` process.
- [Lyo.Tools.Postgres](Lyo.Net/Tools/Lyo.Tools.Postgres/README.md): Interactive Spectre.Console TUI for running and rolling back EF Core migrations against the Lyo Postgres `DbContext`s, plus a couple of Bogus-powered seeders.

<!-- catalog:packages:end -->

### Load testing (k6)

- [k6 framework: Person Query API](k6/framework-person/README.md): k6 workloads and query shapes against `TestApi` persons.
- [K6 benchmark analysis](Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md): latest archived run metrics and comparison to common API stacks (Hasura/PostgREST, typical ORM
  APIs, etc.).

### Performance snapshots (latest archived runs)

| Suite                                                                                                  | Date       | Environment                                           | Headline results                                                                                                                                                                                                                    |
|--------------------------------------------------------------------------------------------------------|------------|-------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Compression** ([summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md))  | 2026-06-28 | .NET 10.0.9, Linux Mint 22.1, Core Ultra 7 155U       | LZ4 fastest compress @ 1 MB (**~128 µs**); Zstd fastest decompress (**~71 µs @ 1 MB**, **~13 ms @ 100 MB**); Zstd streaming compress **~31×** GZip @ 100 MB, **~5×** @ 1 GB                                                         |
| **Encryption** ([summary](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/BENCHMARK_SUMMARY.md)) | 2026-06-30 | .NET 10.0.0, Ubuntu 24.04, Core Ultra 7 155U (AES-NI) | AES-GCM **906 µs / 614 µs** @ 1 MB; ChaCha **1.23 ms / 947 µs**; XChaCha **2.7 / 2.7 ms**; CCM **14 ms**; SIV **20 ms**; stream **~1.2 GB/s** @ 100 MB; hybrid **837 µs** enc @ 1 MB; RSA dec 1 MB **2.6 s**                        |
| **K6 Query API** ([analysis](Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md))                | 2026-07-27 | TestApi + PostgreSQL + k6 on same laptop              | Full 12-suite matrix (Query / QueryProject / root Query × load/stress/spike/soak): root Query fastest (**~31–50 ms p95** load/spike/soak, **~701 ms p95** stress); QueryProject close behind (**~42–65 ms p95**, **~434 ms** stress); full-entity Query has heavier tails (**~103 ms** load, **~1.32 s** stress); status/shape checks **100%** across ~1.35M requests |

---

## Documentation

Project-wide guides live in [`docs/`](docs/README.md); per-package API docs are
the `README.md` beside each library.

| Document                                   | What it covers                                                                                   |
|--------------------------------------------|--------------------------------------------------------------------------------------------------|
| [Documentation index](docs/README.md)      | Entry point for all cross-cutting guides and interactive artifacts.                              |
| [Getting started](docs/getting-started.md) | Prerequisites, consuming a package, a minimal example.                                           |
| [Architecture](docs/architecture.md)       | Area model and dependency law (detail in [`package-layout.md`](Lyo.Net/docs/package-layout.md)). |
| [Configuration](docs/configuration.md)     | Environment variables for the tooling/runner.                                                    |
| [Testing](docs/testing.md)                 | Unit tests, benchmarks, and k6 — local and containerized.                                        |
| [Deployment](docs/deployment.md)           | The container stack and operational notes.                                                       |
| [Publishing](docs/publishing.md)           | Versioning and packing with `build-nuget.sh`.                                                    |
| [Security](docs/security/README.md)        | Security model and crypto design notes ([`SECURITY.md`](SECURITY.md) for reporting).             |
| [Glossary](docs/glossary.md)               | Domain terms and recurring concepts.                                                             |

Interactive (HTML, open locally or via Pages): the
[project graph](docs/Lyo.ProjectGraph.html) and the
[benchmark dashboards](docs/benchmarks/index.html).

## Finding your way

- Start from the **Major capabilities** table for API/query, storage, PDF ([Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md)), encryption, caching, diagnostics, content-threat
  scanning, hashing, and compression.
- For API query behavior and endpoint surface area, the **Lyo.Api** README is the authoritative overview.
- For any other documented package, use **All packages with READMEs** above (complete list as of the last edit).

## Contributing

The license does **not** require users of the library to send changes back—that keeps adoption easy for companies and side projects. We still **welcome** fixes and improvements;
see [`CONTRIBUTING.md`](CONTRIBUTING.md) and the [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md). Security issues should follow [`SECURITY.md`](SECURITY.md).

## License

Licensed under the [Apache License, Version 2.0](LICENSE) ([view on apache.org](https://www.apache.org/licenses/LICENSE-2.0)). You may use Lyo in commercial and closed-source
software; see the license for attribution and redistribution requirements. Replace “The Lyo authors” in [`LICENSE`](LICENSE) if you want a specific copyright line.
