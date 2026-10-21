# Lyo.Ledger

Double-entry books used by Billing. `ILedgerStore` posts balanced journals and voids by writing a reversing journal. Posted rows are immutable. `GetClientArBalance` reads posted AR
lines (account 1100) and excludes reversing journals, not a column on the client.

## Features

- Seeded CoA codes on `LedgerAccountCodes`: 1000 Cash, 1100 AR, 2200 Tax payable, 4000 Revenue.
- `PostJournalAsync` requires debit total = credit total, then freezes the journal.
- `VoidJournalAsync` posts a reversing journal. Posted rows are never deleted.

## Examples

### Post a balanced journal

```csharp
await ledger.EnsureChartOfAccountsAsync(tenantId, ct);
var ar = await ledger.GetAccountByCodeAsync(LedgerAccountCodes.AccountsReceivable, tenantId, ct);
var revenue = await ledger.GetAccountByCodeAsync(LedgerAccountCodes.Revenue, tenantId, ct);
await ledger.PostJournalAsync(new LedgerJournalRecord { Memo = "Issue INV-1" }, [
    new() { AccountId = ar.Id, Debit = 1500m, ClientId = clientId },
    new() { AccountId = revenue.Id, Credit = 1500m }
], tenantId, ct);
```

## See also

- [`Lyo.Ledger.Postgres`](../Lyo.Ledger.Postgres/README.md). EF Core schema (`ledger`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)