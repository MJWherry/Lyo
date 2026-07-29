# Lyo.ContactUs

Core abstractions for contact-form submission. The interface (`IContactUsService`) and a `ContactUsServiceBase` that handles validation, error-code mapping, and logging live
here; concrete storage implementations live in sibling packages such as [`Lyo.ContactUs.Postgres`](../Lyo.ContactUs.Postgres/README.md).

## Examples

### Quick start (with the Postgres storage)

```csharp
using Lyo.ContactUs;
using Lyo.ContactUs.Models;
using Lyo.ContactUs.Postgres;

services.AddContactUsPostgres(new PostgresContactUsOptions {
    ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass",
    EnableAutoMigrations = true,
});

var result = await contactUsService.SubmitAsync(new ContactUsRequest {
    Name = "John Doe",
    Email = "john@example.com",
    Subject = "Question",
    Message = "I have a question about your product.",
}, ct);

if (result.IsSuccess)
    Console.WriteLine($"Submitted! ID: {result.SubmissionId}");
else
    Console.WriteLine($"Failed: {result.Errors?[0].Message}");
```

### Configuration

```json
{
  "PostgresContactUs": {
    "ConnectionString": "Host=localhost;Database=myapp;Username=user;Password=pass",
    "EnableAutoMigrations": true
  },
  "ContactUsOptions": {
    "MaxMessageLength": 10000,
    "MinMessageLength": 10,
    "EnableMetrics": false
  }
}
```

## Surface — `IContactUsService`

- `Task<ContactUsSubmitResult> SubmitAsync(ContactUsRequest request, CancellationToken ct = default)`
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`

## Surface — `ContactUsServiceBase`

- `ValidateRequest(ContactUsRequest)` (virtual; default checks `Name` / `Email` (≤ 320 chars) / `Subject` / `Message` and clamps message length to `Options.MinMessageLength` … `Options.MaxMessageLength`).
- `SubmitCoreAsync(ContactUsRequest, CancellationToken)` (abstract; actually persist the row).
- `TestConnectionAsync(CancellationToken)` (abstract; probe the backend).

## Surface — Request / result models (`Lyo.ContactUs.Models`)

- `ContactUsRequest` *(record)*: `Name` (req, ≤ 200), `Email` (req, ≤ 320, `[EmailAddress]`), `Subject` (req, ≤ 500), `Message` (req, ≤ 10 000), optional `Phone` (≤ 50), optional `Company` (≤ 200).
- `ContactUsServiceOptions`: `SectionName = "ContactUsOptions"`, `MaxMessageLength` (default `10000`), `MinMessageLength` (default `10`), `EnableMetrics` (default `false`).
- `ContactUsSubmitResult` *(derives from `Lyo.Result.Result<Guid?>`)*: `SubmissionId`, `Message`, plus `FromSuccess` / `FromException` / `FromError` factories.

## Surface — DI helpers (`Extensions`)

- `services.AddContactUsService<TService, TOptions>(Action<TOptions>? configure = null)` — generic registration of a custom `IContactUsService` + options type.
- `services.AddContactUsService<TService>(ContactUsServiceOptions options)` — same with a pre-built options instance.
- `services.AddContactUsFromConfiguration(IConfiguration configuration, string sectionName = ContactUsServiceOptions.SectionName)` — binds only the shared `ContactUsServiceOptions` (idempotent — skips if already registered). Pair this with a storage registration such as `AddContactUsPostgres(...)` from `Lyo.ContactUs.Postgres` to get a working service.

## Configuration

Example `appsettings.json`:

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (direct, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)