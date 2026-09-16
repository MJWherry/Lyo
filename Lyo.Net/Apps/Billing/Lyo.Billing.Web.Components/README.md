# Lyo.Billing.Web.Components

`BillingDashboard` is a product UI (not a workbench) hosted by TestGateway at `/billing`. Grids reuse `LyoDataGridProjected`, chips, dialogs, and forms. Compose a draft (manual lines or unbilled charges), issue, Internal-pay, void, and open the invoice PDF via `IBillingClient.InvoicePdfHref`. Invoice view, void, PDF, and draft delete open from the grid in a Large dialog — not a tab page-swap. Bulk PDF, void, and delete-drafts live on the invoice grid. `BillingWorkbench` remains in the package unused.

## Features

- Section nav: Overview KPIs (`GET /Billing/Summary`), Clients, Charges, Invoices (composer + detail dialogs), Payments, Ledger.
- New widgets only where Lyo has no primitive: invoice line editor, composer, money helper, client picker, KPI strip.
- Pay/void/delete-draft use `LyoDialog` + confirm; Internal collection stays on the host via `AddInternalPaymentCollector()`.
- Invoice grid: Large-dialog view (not a page swap), row + bulk PDF/void/delete drafts. Generic QueryProject BulkDelete/BulkPatch stay off because invoices are ReadOnly.
- PDF link uses `IBillingClient.InvoicePdfHref` (`GET /Reporting/Invoices/{id}/pdf` on TestApi). Draft and Void are rejected.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Billing` (direct, lyo)
- `Lyo.Billing.Client` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Ledger` (direct, lyo)
- `Lyo.Payments` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Toml` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `Lyo.Web.Components.Export.Xml` (direct, lyo)
- `Lyo.Web.Components.Export.Yaml` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)