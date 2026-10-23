using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Ledger.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class LedgerPostgresStoreTests(LedgerPostgresFixture fixture)
{
    [Fact]
    public async Task PostJournal_Balanced_IsImmutableAndSumsAr()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILedgerStore>();
        await store.EnsureChartOfAccountsAsync(ct: ct);
        var ar = await store.GetAccountByCodeAsync(LedgerAccountCodes.AccountsReceivable, ct: ct);
        var revenue = await store.GetAccountByCodeAsync(LedgerAccountCodes.Revenue, ct: ct);
        Assert.NotNull(ar);
        Assert.NotNull(revenue);
        var clientId = LyoGuid.CreateV7();
        var posted = await store.PostJournalAsync(
            new() { Memo = "Issue" },
            [
                new() { AccountId = ar.Id, Debit = 1620m, ClientId = clientId }, new() { AccountId = revenue.Id, Credit = 1500m },
                new() { AccountId = (await store.GetAccountByCodeAsync(LedgerAccountCodes.TaxPayable, ct: ct))!.Id, Credit = 120m }
            ], ct: ct);

        Assert.Equal(LedgerJournalStatus.Posted, posted.Status);
        Assert.Equal(1620m, await store.GetClientArBalanceAsync(clientId, ct: ct));
        await Assert.ThrowsAsync<LedgerPostedJournalImmutableException>(() => store.SaveDraftJournalAsync(posted, posted.Lines.ToList(), ct: ct));
    }

    [Fact]
    public async Task PostJournal_Unbalanced_Throws()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILedgerStore>();
        await store.EnsureChartOfAccountsAsync(ct: ct);
        var ar = await store.GetAccountByCodeAsync(LedgerAccountCodes.AccountsReceivable, ct: ct);
        var revenue = await store.GetAccountByCodeAsync(LedgerAccountCodes.Revenue, ct: ct);
        await Assert.ThrowsAsync<LedgerUnbalancedJournalException>(() => store.PostJournalAsync(
            new() { Memo = "Bad" }, [new() { AccountId = ar!.Id, Debit = 100m }, new() { AccountId = revenue!.Id, Credit = 90m }], ct: ct));
    }

    [Fact]
    public async Task VoidJournal_ReversesAr()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILedgerStore>();
        await store.EnsureChartOfAccountsAsync(ct: ct);
        var ar = await store.GetAccountByCodeAsync(LedgerAccountCodes.AccountsReceivable, ct: ct);
        var cash = await store.GetAccountByCodeAsync(LedgerAccountCodes.Cash, ct: ct);
        var clientId = LyoGuid.CreateV7();
        var posted = await store.PostJournalAsync(
            new() { Memo = "AR" }, [new() { AccountId = ar!.Id, Debit = 50m, ClientId = clientId }, new() { AccountId = cash!.Id, Credit = 50m }], ct: ct);

        Assert.Equal(50m, await store.GetClientArBalanceAsync(clientId, ct: ct));
        var reversal = await store.VoidJournalAsync(posted.Id, ct: ct);
        Assert.Equal(LedgerJournalStatus.Posted, reversal.Status);
        Assert.Equal(posted.Id, reversal.VoidOfJournalId);
        var original = await store.GetJournalByIdAsync(posted.Id, ct: ct);
        Assert.Equal(LedgerJournalStatus.Voided, original!.Status);
        Assert.Equal(0m, await store.GetClientArBalanceAsync(clientId, ct: ct));
    }

    [Fact]
    public async Task PostJournal_AccountFromOtherTenant_Throws()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILedgerStore>();
        var tenantA = LyoGuid.CreateV7();
        var tenantB = LyoGuid.CreateV7();
        await store.EnsureChartOfAccountsAsync(tenantA, ct);
        await store.EnsureChartOfAccountsAsync(tenantB, ct);
        var arB = await store.GetAccountByCodeAsync(LedgerAccountCodes.AccountsReceivable, tenantB, ct);
        var cashB = await store.GetAccountByCodeAsync(LedgerAccountCodes.Cash, tenantB, ct);
        await Assert.ThrowsAsync<LedgerAccountTenantException>(() => store.PostJournalAsync(
            new() { Memo = "Cross" }, [new() { AccountId = arB!.Id, Debit = 10m }, new() { AccountId = cashB!.Id, Credit = 10m }], tenantA, ct));
    }
}