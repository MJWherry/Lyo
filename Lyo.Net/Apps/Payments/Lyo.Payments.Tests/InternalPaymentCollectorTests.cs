using Lyo.Payments;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Payments.Tests;

[Trait("Category", "Fast")]
public sealed class InternalPaymentCollectorTests
{
    [Fact]
    public async Task Collect_SucceedsWithReference()
    {
        var collector = new InternalPaymentCollector();
        var invoiceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var result = await collector.CollectAsync(new PaymentCollectionRequest { InvoiceId = invoiceId, Amount = 10m }, TestCt);
        Assert.True(result.Succeeded);
        Assert.StartsWith("INT-", result.Reference);
    }

    [Fact]
    public async Task Collect_ZeroAmount_Throws()
    {
        var collector = new InternalPaymentCollector();
        await Assert.ThrowsAsync<ArgumentException>(
            () => collector.CollectAsync(new PaymentCollectionRequest { InvoiceId = LyoGuid.CreateV7(), Amount = 0m }, TestCt));
    }
}
