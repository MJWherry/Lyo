# Lyo.Portfolio.Api

Dedicated API host for the public Next.js portfolio (`apps/portfolio`). Keeps `Lyo.TestApi` as a separate kitchen-sink host. A portfolio Gateway (Test Gateway-style) can point here later. Single Postgres (schemas for people/config/job/reporting/filestore/auth), local disk file storage, RabbitMQ for job events, and Google OIDC via Lyo authentication.

## Features

- People CRUD + root `POST /Query`
- Config API under `/api/config` (`AddConfigApi` / `MapConfigApiEndpoints`)
- `BuildJobGroup` + optional RabbitMQ job event publisher
- Reporting API with generation hooks into local file storage
- File storage workbench at `Workbench/FileStorage` (local disk + Postgres metadata)
- Lyo JWT auth + Google provider when `GoogleAuth:ClientId` is set
- `GET /health` → `{ service: "Lyo.Portfolio.Api" }` on port 5251

## Examples

### Google OAuth (local)

```json
{
  "GoogleAuth": {
    "ClientId": "…",
    "ClientSecret": "…",
    "RedirectUri": "http://localhost:5251/auth/callback/google"
  },
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "http://localhost:3100", "http://localhost:5138" ]
  }
}
```

## Google Cloud setup

Create an OAuth client on your Google Cloud console. Authorized redirect URI must match `GoogleAuth:RedirectUri` exactly (local: `http://localhost:5251/auth/callback/google`). Add production API callback when deploying. Whitelist portfolio and Gateway origins in `LyoOidcBff:AllowedReturnOrigins`. Seed domain data yourself via the HTTP APIs after deploy.

## Not included (stay on TestApi)

- Endato, Discord, Twilio, Comic, OCR and other kitchen-sink integrations

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` — (direct, lyo)
- `Lyo.Api.Export` — (direct, lyo)
- `Lyo.Api.Export.Csv` — (direct, lyo)
- `Lyo.Api.Export.Xlsx` — (direct, lyo)
- `Lyo.Api.Reporting` — (direct, lyo)
- `Lyo.Authentication.AspNetCore` — (direct, lyo)
- `Lyo.Authentication.Google` — (direct, lyo)
- `Lyo.Authentication.OpenIdConnect` — (direct, lyo)
- `Lyo.Cache` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.Config.Api` — (direct, lyo)
- `Lyo.Csv` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` — (direct, lyo)
- `Lyo.FileStorage` — (direct, lyo)
- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.Job.Postgres` — (direct, lyo)
- `Lyo.KeyStore` — (direct, lyo)
- `Lyo.Lock` — (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` — (direct, lyo)
- `Lyo.People.Postgres` — (direct, lyo)
- `Lyo.Reporting.Postgres` — (direct, lyo)
- `Lyo.Xlsx` — (direct, lyo)
- `Mapster` `10.0.10` — (direct, third-party)
- `Mapster.DependencyInjection` `10.0.10` — (direct, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` — (direct, microsoft)
- `Scalar.AspNetCore` `2.16.11` — (direct, third-party)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Audit` — (transitive, lyo)
- `Lyo.Authentication` — (transitive, lyo)
- `Lyo.Authentication.Keycloak` — (transitive, lyo)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Authentication.Postgres` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Config` — (transitive, lyo)
- `Lyo.Config.Postgres` — (transitive, lyo)
- `Lyo.ContentThreatScan` — (transitive, lyo)
- `Lyo.Csv.Models` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` — (transitive, lyo)
- `Lyo.Diff` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.EntityReference.Postgres` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.FileMetadataStore` — (transitive, lyo)
- `Lyo.Formatter` — (transitive, lyo)
- `Lyo.Geolocation.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.Job.Models` — (transitive, lyo)
- `Lyo.MessageQueue` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.People.Models` — (transitive, lyo)
- `Lyo.Postgres` — (transitive, lyo)
- `Lyo.Query` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Reporting.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Schedule.Models` — (transitive, lyo)
- `Lyo.Scheduler` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Lyo.Xlsx.Models` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` — (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` — (transitive, third-party)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `ExcelDataReader` `3.9.0` — (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` — (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` — (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `RabbitMQ.Client` `7.2.1` — (transitive, third-party)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` — (transitive, microsoft)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)