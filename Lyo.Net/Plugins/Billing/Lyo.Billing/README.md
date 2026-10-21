# Lyo.Billing

Billing documents in schema `billing`, including bill-to clients (`billing.client`). `IBillingStore.IssueInvoiceAsync` snapshots bill-to strings (encrypted tax id) and posts Dr
AR / Cr Revenue / Cr Tax. `RecordPaymentAsync` posts Dr Cash / Cr AR. Void writes a reversing journal. This plugin does not render PDFs.

## Features

- `SaveClientAsync` / `GetClientByIdAsync` own bill-to clients in schema `billing`.
- `CreateInvoiceFromChargesAsync` builds a draft from unbilled charges.
- Issue assigns `{prefix}-{yyyy}-{seq}` and posts to `ILedgerStore`.
- Copied tax ids and biller bank/remittance use `encrypted_*` bytea columns.

## Examples

### Invoice a client and record payment

```csharp
var invoice = await billing.CreateInvoiceFromChargesAsync(clientId, chargeIds, taxRate: 0.08m, tenantId, ct);
await billing.IssueInvoiceAsync(invoice.Id, tenantId, ct);
await billing.RecordPaymentAsync(invoice.Id, 1620m, "USD", PaymentMethod.Wire, "WIRE-4411", tenantId, ct);
```

## See also

- [`Lyo.Billing.Postgres`](../Lyo.Billing.Postgres/README.md). EF Core schema (`billing`).
- [`Lyo.Ledger`](../../Ledger/Lyo.Ledger/README.md). Journals posted at issue and pay.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Ledger` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)