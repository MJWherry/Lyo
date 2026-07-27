# Lyo.Reporting.Web

Blazor `ReportViewer`, business document templates, and HTML/PDF `IReportRenderer` implementation.

## DI

```csharp
services.AddWebRenderer(...); // Lyo.Web.WebRenderer
services.AddReportingWebRenderer();
```

Depends on `Lyo.Reporting.Models` for composition types. Does not reference Postgres/EF.
