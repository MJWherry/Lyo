# LYO — **Library for Your Organization.**

This is a continual work-in-progress personal development workspace; it also serves as my portfolio for .NET libraries and related tooling.

This repository is a .NET-focused toolkit of libraries and apps for business data: APIs with a rich query model, durable file handling, document parsing, and cross-cutting infrastructure (security, compression, observability, and more). Most code lives under [`Lyo.Net/`](Lyo.Net/).

**Note:** Generative AI tools were used to help build and maintain parts of this codebase where scale made that practical—notably complex numerical packages such as **Mathematics** and **Scientific** (including their function libraries), **documentation** (including long-form package READMEs), **test** projects and libraries, and **some JavaScript** (for example in load-testing scripts, Blazor companion scripts, or other web-related assets). Human review still applies; treat those areas with the same scrutiny you would for any large or subtle code.

---

## Major capabilities

These are the areas that tend to anchor product work; each links to deeper docs where they exist in-tree.

| Area | What it is | Documentation |
|------|----------------|---------------|
| **API & query** | Minimal APIs and CRUD built on EF Core, plus a **Query** engine (filters, includes, **projection** with optional **computed columns**, **`entityTypes`** metadata on projected responses, bulk operations, exports). | [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md): RESTful minimal APIs, CRUD, query and projection. |
| **Query client UI** | Blazor components (e.g. data grid) that speak the same query shapes as the API. | Covered in the Lyo.Api README under *Blazor Components*; [Lyo.Web.Components](Lyo.Net/Integration/Web/Lyo.Web.Components/): Blazor UI aligned with API query shapes. |
| **File storage** | Pluggable storage with optional compression and encryption, metadata, streaming, and duplicate handling. | [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md): abstractions, metadata, compression, encryption. |
| **Cloud blob backends** | AWS S3–compatible and Azure Blob implementations for the file storage abstractions. | [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md): S3-compatible storage · [Lyo.FileStorage.Azure](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Azure/README.md): Azure Blob |
| **PDF** | Load PDFs and extract text via **`IPdfService`** (`PdfService`): words/lines, bounding boxes, key–value and table-style extraction, merges. Models in `Lyo.Pdf.Models`; optional Blazor annotator for drawing regions by ID. | [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md): PDF service, loading, and extraction. See also [Lyo.Pdf.Annotator](Lyo.Net/Data/Pdf/Lyo.Pdf.Annotator/README.md) for browser-based bounding-box mapping. |
| **Encryption** | Authenticated encryption, envelope/two-key patterns, keystore integration. | [Lyo.Encryption](Lyo.Net/Security/Encryption/README.md): authenticated encryption, envelope/two-key, key management. |
| **Compression** | Multiple algorithms, streams/files, size limits and bomb protections. | [Lyo.Compression](Lyo.Net/Data/Compression/Lyo.Compression/README.md): algorithms, streams/files, size limits and bomb guards. |

---

## Repository layout (high level)

| Path | Comment |
|------|--------|
| [`Lyo.Net/`](Lyo.Net/) | Main .NET solution root: shared props, solution file, and libraries grouped by the subfolders below. |
| [`Lyo.Net/Core/`](Lyo.Net/Core/) | Cross-cutting primitives: validation, metrics, resilience, exceptions, common types, math/science, people models, geolocation, webhooks, locks, scheduling, streams, date/time, audit, change tracking, health—domain-agnostic building blocks for the rest of the stack. |
| [`Lyo.Net/Data/`](Lyo.Net/Data/) | Data handling and persistence helpers: file storage (local/S3/Azure), compression, CSV/XLSX/PDF, images, Postgres migration helpers, QR codes, file-system watching, temporary IO, and related parsers/processors. |
| [`Lyo.Net/Features/`](Lyo.Net/Features/) | Composable product features (often EF-backed): comments, notes, ratings, tags, typed config, contact forms, profanity filter, short URLs—meant to plug into host apps alongside Core and Data. |
| [`Lyo.Net/Integration/`](Lyo.Net/Integration/) | Application-facing integration: minimal APIs and query (`Lyo.Api`), Blazor web components and reporting, background jobs, Discord bot—wires Core/Data/Features into runnable surfaces. |
| [`Lyo.Net/Security/`](Lyo.Net/Security/) | Cryptography (`Lyo.Encryption`) and encryption benchmarks. |
| [`Lyo.Net/Communication/`](Lyo.Net/Communication/) | Messaging and media delivery: SMTP email, SMS (including Twilio), and text-to-speech providers. |
| [`Lyo.Net/Tools/`](Lyo.Net/Tools/) | Host apps and utilities (e.g. gateway, test API/console) for trying components end-to-end. |
| [`k6/`](k6/) | Load-testing scripts; see [k6 framework: Person Query API](k6/framework-person/README.md). |

Individual projects are mostly **one folder per NuGet-style package** (e.g. `Lyo.Something`). The sections below list **every** in-repo `README.md` beside a library, grouped by top-level area.

---

## All packages with READMEs

### Communication

- [Lyo.Email](Lyo.Net/Communication/Email/Lyo.Email/README.md): SMTP email via MailKit, fluent builder, bulk send, attachments, DI.
- [Lyo.Sms](Lyo.Net/Communication/Sms/Lyo.Sms/README.md): SMS abstraction, E.164 validation, bulk messaging, pluggable providers.
- [Lyo.Sms.Twilio](Lyo.Net/Communication/Sms/Lyo.Sms.Twilio/README.md): Twilio SMS/MMS implementation for Lyo.Sms.
- [Lyo.Tts.AwsPolly](Lyo.Net/Communication/Speech/Lyo.Tts.AwsPolly/README.md): Amazon Polly text-to-speech.
- [Lyo.Tts.Typecast](Lyo.Net/Communication/Speech/Lyo.Tts.Typecast/README.md): Typecast API text-to-speech.
- [Lyo.Tts.WindowsSpeech](Lyo.Net/Communication/Speech/Lyo.Tts.WindowsSpeech/README.md): Windows SAPI speech synthesis.

### Core

- [Lyo.Audit.Postgres](Lyo.Net/Core/Audit/Lyo.Audit.Postgres/README.md): EF Core audit persistence to PostgreSQL (JSONB).
- [Lyo.Audit](Lyo.Net/Core/Audit/Lyo.Audit/README.md): audit changes and events with pluggable `IAuditRecorder`.
- [Lyo.ChangeTracker.Postgres](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker.Postgres/README.md): PostgreSQL change history for `Lyo.ChangeTracker`.
- [Lyo.ChangeTracker](Lyo.Net/Core/ChangeTracker/Lyo.ChangeTracker/README.md): entity-scoped change history via `EntityRef`.
- [Lyo.Common](Lyo.Net/Core/Common/Lyo.Common/README.md): results, errors, JSON helpers, shared primitives.
- [Lyo.DateAndTime](Lyo.Net/Core/DateAndTime/Lyo.DateAndTime/README.md): time zones, US-state mapping, scheduling utilities.
- [Lyo.Geolocation.Google](Lyo.Net/Core/Geolocation/Lyo.Geolocation.Google/README.md): Google Maps geocoding / reverse geocoding service.
- [Lyo.Health](Lyo.Net/Core/Health/Lyo.Health/README.md): `IHealth` and `HealthResult` for service health checks.
- [Lyo.Lock](Lyo.Net/Core/Lock/Lyo.Lock/README.md): in-process locks and keyed semaphores.
- [Lyo.Lock.Redis](Lyo.Net/Core/Lock/Lyo.Lock.Redis/README.md): Redis distributed locks for `Lyo.Lock`.
- [Lyo.Exceptions](Lyo.Net/Core/Lyo.Exceptions/README.md): shared exception types and argument validation helpers.
- [Lyo.Mathematics.Functions](Lyo.Net/Core/Mathematics/Lyo.Mathematics.Functions/README.md): F# math implementations (algebra, stats, numerics).
- [Lyo.Mathematics](Lyo.Net/Core/Mathematics/Lyo.Mathematics/README.md): C# math contracts, units, vectors, matrices, registries.
- [Lyo.Metrics.OpenTelemetry](Lyo.Net/Core/Metrics/Lyo.Metrics.OpenTelemetry/README.md): OpenTelemetry backend for `IMetrics`.
- [Lyo.Metrics](Lyo.Net/Core/Metrics/Lyo.Metrics/README.md): counters, gauges, histograms, timings; pluggable backends.
- [Lyo.People.Models](Lyo.Net/Core/People/Lyo.People.Models/README.md): people, contacts, relationships domain models.
- [Lyo.People.Postgres](Lyo.Net/Core/People/Lyo.People.Postgres/README.md): EF Core PostgreSQL persistence for people data.
- [Lyo.Resilience](Lyo.Net/Core/Resilience/Lyo.Resilience/README.md): Polly pipelines from configuration with logging.
- [Lyo.Scheduler](Lyo.Net/Core/Scheduler/Lyo.Scheduler/README.md): in-process scheduled jobs with optional state store.
- [Lyo.Scientific.Functions](Lyo.Net/Core/Scientific/Lyo.Scientific.Functions/README.md): F# scientific/engineering implementations.
- [Lyo.Scientific](Lyo.Net/Core/Scientific/Lyo.Scientific/README.md): scientific domain models on top of `Lyo.Mathematics`.
- [Lyo.Streams](Lyo.Net/Core/Streams/Lyo.Streams/README.md): hashing, tee, counting, progress, concatenated streams.
- [Lyo.Validation](Lyo.Net/Core/Validation/Lyo.Validation/README.md): fluent validators and structured `Result` failures.
- [Lyo.Webhook.Twilio](Lyo.Net/Core/Webhook/Lyo.Webhook.Twilio/README.md): Twilio webhook signature validation for `Lyo.Webhook`.
- [Lyo.Webhook](Lyo.Net/Core/Webhook/Lyo.Webhook/README.md): ASP.NET Core webhook verification pipeline and HMAC helpers.

### Data

- [Lyo.Compression](Lyo.Net/Data/Compression/Lyo.Compression/README.md): multi-algorithm compression, streams, atomic file ops.
- [Lyo.Csv](Lyo.Net/Data/Csv/Lyo.Csv/README.md): CSV read/write via CsvHelper.
- [Lyo.FileStorage.Azure](Lyo.Net/Data/FileStorage/Lyo.FileStorage.Azure/README.md): Azure Blob backend for file storage.
- [Lyo.FileStorage](Lyo.Net/Data/FileStorage/Lyo.FileStorage/README.md): file storage abstractions, metadata, compression, encryption.
- [Lyo.FileStorage.S3](Lyo.Net/Data/FileStorage/Lyo.FileStorage.S3/README.md): S3-compatible storage (AWS, B2, MinIO).
- [Lyo.FileSystemWatcher](Lyo.Net/Data/FileSystemWatcher/Lyo.FileSystemWatcher/README.md) ([overview](Lyo.Net/Data/FileSystemWatcher/README.md)): snapshot-based file watching, debounce, move detection.
- [Lyo.Images](Lyo.Net/Data/Images/Lyo.Images/README.md): image ops with ImageSharp (resize, crop, watermark).
- [Lyo.Images.Skia](Lyo.Net/Data/Images/Lyo.Images.Skia/README.md): SkiaSharp implementation of `IImageService`.
- [Lyo.IO.Temp](Lyo.Net/Data/IOTemp/Lyo.IO.Temp/README.md): temp files/dirs with sessions and naming strategies.
- [Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md): `IPdfService` / `PdfService` — load PDFs, extract text, regions, key–value and tables.
- [Lyo.Pdf.Annotator](Lyo.Net/Data/Pdf/Lyo.Pdf.Annotator/README.md): browser PDF annotator returning bounding boxes by ID (pairs with `GetLinesInBoundingBox`).
- [Lyo.Postgres](Lyo.Net/Data/Postgres/Lyo.Postgres/README.md): shared EF Core auto-migration hosted service helpers.
- [Lyo.QRCode](Lyo.Net/Data/QRCode/Lyo.QRCode/README.md): QR generation (QRCoder), formats, error correction, icons.
- [Lyo.Xlsx](Lyo.Net/Data/Xlsx/Lyo.Xlsx/README.md): Excel import/export with ClosedXML / ExcelDataReader.

### Features

- [Lyo.Comment.Postgres](Lyo.Net/Features/Comment/Lyo.Comment.Postgres/README.md): PostgreSQL comments with threading and reactions.
- [Lyo.Config.Postgres](Lyo.Net/Features/Config/Lyo.Config.Postgres/README.md): PostgreSQL storage for typed per-entity config.
- [Lyo.Config](Lyo.Net/Features/Config/Lyo.Config/README.md): typed per-entity configuration definitions and bindings.
- [Lyo.ContactUs](Lyo.Net/Features/ContactUs/Lyo.ContactUs/README.md): contact form submissions with pluggable storage.
- [Lyo.Note.Postgres](Lyo.Net/Features/Note/Lyo.Note.Postgres/README.md): PostgreSQL notes keyed by entity references.
- [Lyo.Profanity](Lyo.Net/Features/Profanity/Lyo.Profanity/README.md): file-based profanity filter, multi-language, strategies.
- [Lyo.Rating.Postgres](Lyo.Net/Features/Rating/Lyo.Rating.Postgres/README.md): PostgreSQL ratings, subjects, reactions.
- [Lyo.ShortUrl](Lyo.Net/Features/ShortUrl/Lyo.ShortUrl/README.md): URL shortening abstraction and fluent builders.
- [Lyo.Tag.Postgres](Lyo.Net/Features/Tag/Lyo.Tag.Postgres/README.md): PostgreSQL tagging by entity reference.

### Integration

- [Lyo.Api](Lyo.Net/Integration/Api/Lyo.Api/README.md): minimal APIs, CRUD, query engine, endpoint builder.
- [Lyo.Discord.Bot](Lyo.Net/Integration/Discord/Lyo.Discord.Bot/README.md): DSharpPlus bot syncing guilds to your Lyo API.
- [Lyo.Job.Postgres](Lyo.Net/Integration/Job/Lyo.Job.Postgres/README.md): EF Core job schema and migrations for job management.
- [Lyo.Job.Scheduler](Lyo.Net/Integration/Job/Lyo.Job.Scheduler/README.md): polls job API, evaluates schedules, RabbitMQ triggers.
- [Lyo.Web.Reporting.Postgres](Lyo.Net/Integration/Web/WebReporting/Lyo.Web.Reporting.Postgres/README.md): store and load prebuilt reports in PostgreSQL.
- [Lyo.Web.Reporting](Lyo.Net/Integration/Web/WebReporting/Lyo.Web.Reporting/README.md): fluent Blazor report builder and rendering.

### Security

- [Lyo.Encryption.Benchmarks](Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks/README.md): BenchmarkDotNet suite for `Lyo.Encryption`.
- [Lyo.Encryption](Lyo.Net/Security/Encryption/README.md): authenticated encryption, envelope/two-key patterns, keystore hooks.

### Tools

- [Lyo.Preview](Lyo.Net/Tools/Lyo.Preview/README.md): open PDFs, images, HTML, and more in the default browser.

### Load testing (k6)

- [k6 framework: Person Query API](k6/framework-person/README.md): k6 workloads and query shapes against `TestApi` persons.

---

## Finding your way

- Start from the **Major capabilities** table for API/query, storage, PDF ([Lyo.Pdf](Lyo.Net/Data/Pdf/Lyo.Pdf/README.md)), encryption, and compression.
- For API query behavior and endpoint surface area, the **Lyo.Api** README is the authoritative overview.
- For any other documented package, use **All packages with READMEs** above (complete list as of the last edit).

## Contributing

The license does **not** require users of the library to send changes back—that keeps adoption easy for companies and side projects. We still **welcome** fixes and improvements; see [`CONTRIBUTING.md`](CONTRIBUTING.md).

## License

Licensed under the [Apache License, Version 2.0](LICENSE) ([view on apache.org](https://www.apache.org/licenses/LICENSE-2.0)). You may use Lyo in commercial and closed-source software; see the license for attribution and redistribution requirements. Replace “The Lyo authors” in [`LICENSE`](LICENSE) if you want a specific copyright line.
