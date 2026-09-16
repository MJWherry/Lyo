# Lyo.Payments

`IPaymentCollector` collects funds and returns a reference. Hosts call Billing `RecordPaymentAsync` on success so the ledger is posted there. `InternalPaymentCollector` always succeeds and is meant for TestGateway / local workbenches.

## Features

- `AddInternalPaymentCollector` registers `InternalPaymentCollector` as `IPaymentCollector`.
- No Stripe, card network, or plugin PDF references.

## Examples

### Collect then record

```csharp
services.AddInternalPaymentCollector();
var collected = await collector.CollectAsync(new PaymentCollectionRequest { InvoiceId = invoiceId, Amount = 1620m }, ct);
if (collected.Succeeded)
    await billing.RecordPaymentAsync(invoiceId, 1620m, "USD", PaymentMethod.Internal, collected.Reference, tenantId, ct);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)