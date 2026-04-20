# Lyo.Web.Reporting.Postgres

PostgreSQL implementation of Lyo.Web.Reporting for storing and loading prebuilt reports using Entity Framework Core.

## Features

- ✅ **Entity Framework Core** - Full EF Core integration with PostgreSQL
- ✅ **Report Storage** - Save and load prebuilt reports with metadata
- ✅ **Type-Safe Loading** - Generic type support for report parameters
- ✅ **Soft Deletes** - Reports are soft-deleted (marked inactive) rather than hard-deleted
- ✅ **Auto Migrations** - Optional automatic database migrations on startup
- ✅ **Schema Support** - Uses dedicated 'report' schema for organization
- ✅ **Indexed Queries** - Optimized database indexes for performance

## Quick Start

```csharp
using Lyo.Web.Reporting.Postgres;
using Lyo.Web.Reporting.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add PostgreSQL reporting service
services.AddReportingDbContextFactory(new PostgresReportingOptions
{
    ConnectionString = "Host=localhost;Database=reports;Username=postgres;Password=password",
    EnableAutoMigrations = true
});

var serviceProvider = services.BuildServiceProvider();
var reportingService = serviceProvider.GetRequiredService<PostgresReportingService>();

// Build a report
var report = ReportBuilder<string>.New()
    .SetTitle("My Report")
    .SetSubtitle("Generated Report")
    .AddSection("Summary", section => section
        .AddColumn("Total Items", 150)
        .AddColumn("Status", "Active")
    )
    .Build();

// Save the report
var reportId = await reportingService.SaveReportAsync(
    "My Saved Report",
    report,
    description: "A sample report",
    tags: "sample,test");

Console.WriteLine($"Saved report with ID: {reportId}");

// Load the report
var loadedReport = await reportingService.LoadReportAsync<string>(reportId);
if (loadedReport != null)
{
    Console.WriteLine($"Loaded report: {loadedReport.Title}");
}

// List all reports
var allReports = await reportingService.ListReportsAsync();
foreach (var metadata in allReports)
{
    Console.WriteLine($"- {metadata.Name} (ID: {metadata.Id})");
}
```

## Database Schema

The implementation creates a table in the 'report' schema:

- **reports** - Stores report data with metadata (name, description, tags, creation/modification dates)

## API Reference

### SaveReportAsync<T>

Saves a report to the database.

```csharp
Guid SaveReportAsync<T>(
    string name,
    Report<T> report,
    string? description = null,
    string? tags = null,
    CancellationToken ct = default)
```

### UpdateReportAsync<T>

Updates an existing report.

```csharp
bool UpdateReportAsync<T>(
    Guid id,
    Report<T> report,
    string? name = null,
    string? description = null,
    string? tags = null,
    CancellationToken ct = default)
```

### LoadReportAsync<T>

Loads a report by ID.

```csharp
Report<T>? LoadReportAsync<T>(
    Guid id,
    CancellationToken ct = default)
```

### LoadReportByNameAsync<T>

Loads a report by name.

```csharp
Report<T>? LoadReportByNameAsync<T>(
    string name,
    CancellationToken ct = default)
```

### ListReportsAsync

Lists all active reports.

```csharp
List<ReportMetadata> ListReportsAsync(
    CancellationToken ct = default)
```

### ListReportsByTypeAsync<T>

Lists reports filtered by parameter type.

```csharp
List<ReportMetadata> ListReportsByTypeAsync<T>(
    CancellationToken ct = default)
```

### DeleteReportAsync

Soft-deletes a report.

```csharp
bool DeleteReportAsync(
    Guid id,
    CancellationToken ct = default)
```

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Web.Reporting.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Metrics`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*7*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `InitialCreate`
- `PostgresReportingOptions`
- `ReportEntity`
- `ReportEntityConfiguration`
- `ReportingDbContext`
- `ReportingDbContextFactory`

<!-- LYO_README_SYNC:END -->

