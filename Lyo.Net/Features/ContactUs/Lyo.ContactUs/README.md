# Lyo.ContactUs

Core library for contact form submissions with support for multiple storage providers.

## Features

- **Provider-agnostic** – Implement `IContactUsService` for any storage (Postgres, custom)
- **Validation** – Built-in validation for name, email, subject, message
- **Dependency injection** – First-class DI support with plug-and-play extensions

## Quick Start

```csharp
using Lyo.ContactUs;
using Lyo.ContactUs.Models;
using Lyo.ContactUs.Postgres;

// Register with PostgreSQL (plug and play)
services.AddContactUsPostgres(new PostgresContactUsOptions {
    ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass",
    EnableAutoMigrations = true
});

// Or from configuration
services.AddContactUsPostgres(configuration);

// Submit a contact form
var request = new ContactUsRequest {
    Name = "John Doe",
    Email = "john@example.com",
    Subject = "Question",
    Message = "I have a question about your product."
};

var result = await contactUsService.SubmitAsync(request, cancellationToken);

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

## API

- `SubmitAsync(request)` – Submit a contact form
- `TestConnectionAsync()` – Test connection to the storage provider

### ContactUsRequest

- `Name` (required) – Sender's name
- `Email` (required) – Sender's email
- `Subject` (required) – Message subject
- `Message` (required) – Message body
- `Phone` (optional) – Phone number
- `Company` (optional) – Company name

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.ContactUs.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*5*). Nested types and file-scoped namespaces may omit some entries.

- `ContactUsErrorCodes`
- `ContactUsServiceBase`
- `ContactUsServiceOptions`
- `Extensions`
- `IContactUsService`

<!-- LYO_README_SYNC:END -->

