using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class CurrencyDenominationInfoTests
{
    [Fact]
    public void CoinsAndBills_LookupBySlug()
    {
        Assert.Equal(0.05m, CurrencyInfo.Usd.Coins["nickel"].Value);
        Assert.Equal(CurrencyDenominationKind.Coin, CurrencyInfo.Usd.Coins["nickel"].Kind);
        Assert.Equal(20m, CurrencyInfo.Usd.Bills["20"].Value);
        Assert.Equal(CurrencyDenominationKind.Bill, CurrencyInfo.Usd.Bills["20"].Kind);
    }

    [Fact]
    public void GetDenominations_Usd_ReturnsSortedCatalogRows()
    {
        var denominations = CurrencyInfo.Usd.GetDenominations();
        Assert.NotEmpty(denominations);
        Assert.Contains(denominations, d => d.Kind == CurrencyDenominationKind.Coin && d.Value == 0.05m);
        Assert.Contains(denominations, d => d.Kind == CurrencyDenominationKind.Bill && d.Value == 20m);
        Assert.True(IsSortedDescending(denominations.Select(d => d.Value)));
    }

    [Fact]
    public void FromCode_GetDenominations_MatchesUsd() => Assert.Equal(CurrencyInfo.Usd.GetDenominations(), CurrencyInfo.FromCode("USD").GetDenominations());

    [Fact]
    public void UnknownCurrency_GetDenominations_IsEmpty()
    {
        Assert.False(CurrencyInfo.Unknown.IsDenominationCataloged);
        Assert.Empty(CurrencyInfo.Unknown.GetDenominations());
        Assert.Empty(CurrencyInfo.Unknown.Coins);
        Assert.Empty(CurrencyInfo.Unknown.Bills);
    }

    [Fact]
    public void TryGetDenomination_ByKeyAndValueKind_Succeeds()
    {
        Assert.True(CurrencyInfo.Usd.TryGetDenomination("nickel", out var byKey));
        Assert.Equal(0.05m, byKey.Value);
        Assert.Equal(CurrencyDenominationKind.Coin, byKey.Kind);
        Assert.True(CurrencyInfo.Usd.TryGetDenomination(0.05m, CurrencyDenominationKind.Coin, out var byValue));
        Assert.Equal(byKey, byValue);
    }

    [Fact]
    public void Jpy_Includes500CoinAnd10000Bill()
    {
        Assert.Equal(500m, CurrencyInfo.Jpy.Coins["500"].Value);
        Assert.Equal(10000m, CurrencyInfo.Jpy.Bills["10000"].Value);
    }

    [Fact]
    public void Hkd_Coins_10cent()
    {
        Assert.Equal(0.10m, CurrencyInfo.Hkd.Coins["10cent"].Value);
        Assert.Equal(CurrencyDenominationKind.Coin, CurrencyInfo.Hkd.Coins["10cent"].Kind);
    }

    private static bool IsSortedDescending(IEnumerable<decimal> values)
    {
        decimal? previous = null;
        foreach (var value in values) {
            if (previous is { } p && value > p)
                return false;

            previous = value;
        }

        return true;
    }
}