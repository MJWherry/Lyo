using System.Text.Json;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class CurrencyInfoTests
{
    [Fact]
    public void StaticRegistry_ContainsExpectedMetadata()
    {
        var info = CurrencyInfo.Jpy;
        Assert.Equal("JPY", info.Code);
        Assert.Equal(392, info.NumericCode);
        Assert.Equal("Japanese Yen", info.Name);
        Assert.Equal("¥", info.Symbol);
        Assert.Equal("¥", info.CoinSymbol);
        Assert.Equal(0, info.MinorUnits);
    }

    [Fact]
    public void Usd_CoinSymbol_IsCent() => Assert.Equal("¢", CurrencyInfo.Usd.CoinSymbol);

    [Fact]
    public void Gbp_CoinSymbol_IsPenny() => Assert.Equal("p", CurrencyInfo.Gbp.CoinSymbol);

    [Fact]
    public void FromCode_ResolvesKnownCurrencies()
    {
        Assert.Equal(CurrencyInfo.Usd, CurrencyInfo.FromCode("USD"));
        Assert.Equal(CurrencyInfo.Jpy, CurrencyInfo.FromCode("jpy"));
    }

    [Fact]
    public void FromNumericCode_ResolvesKnownCurrencies() => Assert.Equal(CurrencyInfo.Jpy, CurrencyInfo.FromNumericCode(392));

    [Fact]
    public void FromCode_UnknownCode_ReturnsUnknown() => Assert.Equal(CurrencyInfo.Unknown, CurrencyInfo.FromCode("XYZ"));

    [Fact]
    public void FromNumericCode_UnknownNumeric_ReturnsUnknown() => Assert.Equal(CurrencyInfo.Unknown, CurrencyInfo.FromNumericCode(999));

    [Fact]
    public void All_ExcludesUnknown()
    {
        Assert.DoesNotContain(CurrencyInfo.Unknown, CurrencyInfo.All);
        Assert.Contains(CurrencyInfo.Jpy, CurrencyInfo.All);
    }

    [Fact]
    public void JsonConverter_RoundTripsKnownCurrency()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CurrencyInfoJsonConverter());
        var json = JsonSerializer.Serialize(CurrencyInfo.Jpy, options);
        Assert.Equal("\"JPY\"", json);
        var restored = JsonSerializer.Deserialize<CurrencyInfo>(json, options);
        Assert.Equal(CurrencyInfo.Jpy, restored);
    }

    [Fact]
    public void JsonConverter_UnknownWritesNull()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CurrencyInfoJsonConverter());
        var json = JsonSerializer.Serialize(CurrencyInfo.Unknown, options);
        Assert.Equal("null", json);
    }
}