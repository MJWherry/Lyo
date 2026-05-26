# Lyo.ContactUs

Core abstractions for contact-form submission. The interface (`IContactUsService`) and a `ContactUsServiceBase` that handles validation, error-code mapping, and logging live
here; concrete storage implementations live in sibling packages such as [`Lyo.ContactUs.Postgres`](../Lyo.ContactUs.Postgres/README.md).

## Surface

### `IContactUsService`

- `Task<ContactUsSubmitResult> SubmitAsync(ContactUsRequest request, CancellationToken ct = default)`
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`

### `ContactUsServiceBase`

Abstract base implementation that subclasses (`PostgresContactUsService`, custom) only need to override:

- `ValidateRequest(ContactUsRequest)` (virtual; default checks `Name` / `Email` (≤ 320 chars) / `Subject` / `Message` and clamps message length to
  `Options.MinMessageLength` … `Options.MaxMessageLength`).
- `SubmitCoreAsync(ContactUsRequest, CancellationToken)` (abstract; actually persist the row).
- `TestConnectionAsync(CancellationToken)` (abstract; probe the backend).

`SubmitAsync` wraps the storage call in a try/catch and returns:

- `ContactUsSubmitResult.FromError(...)` with `CONTACT_US_VALIDATION_FAILED` when validation fails.
- `ContactUsSubmitResult.FromError(...)` with `CONTACT_US_OPERATION_CANCELLED` on `OperationCanceledException`.
- `ContactUsSubmitResult.FromException(...)` with `CONTACT_US_SUBMIT_FAILED` for any other exception (also logged).

Error codes are defined in `ContactUsErrorCodes` (`SubmitFailed`, `ValidationFailed`, `OperationCancelled`).

### Request / result models (`Lyo.ContactUs.Models`)

- `ContactUsRequest` *(record)*: `Name` (req, ≤ 200), `Email` (req, ≤ 320, `[EmailAddress]`), `Subject` (req, ≤ 500), `Message` (req, ≤ 10 000), optional `Phone` (≤ 50),
  optional `Company` (≤ 200).
- `ContactUsServiceOptions`: `SectionName = "ContactUsOptions"`, `MaxMessageLength` (default `10000`), `MinMessageLength` (default `10`), `EnableMetrics` (default `false`).
- `ContactUsSubmitResult` *(derives from `Lyo.Result.Result<Guid?>`)*: `SubmissionId`, `Message`, plus `FromSuccess` / `FromException` / `FromError` factories.

### DI helpers (`Extensions`)

- `services.AddContactUsService<TService, TOptions>(Action<TOptions>? configure = null)` — generic registration of a custom `IContactUsService` + options type.
- `services.AddContactUsService<TService>(ContactUsServiceOptions options)` — same with a pre-built options instance.
- `services.AddContactUsFromConfiguration(IConfiguration configuration, string sectionName = ContactUsServiceOptions.SectionName)` — binds only the shared
  `ContactUsServiceOptions` (idempotent — skips if already registered). Pair this with a storage registration such as `AddContactUsPostgres(...)` from `Lyo.ContactUs.Postgres`
  to get a working service.

## Quick start (with the Postgres storage)

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

## Configuration

Example `appsettings.json`:

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

## Dependencies

*(Synchronized from `Lyo.ContactUs.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |
| `Microsoft.Extensions.Options`                          | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions`  | `[10,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — `ContactUsSubmitResult` derives from `Result<Guid?>`.