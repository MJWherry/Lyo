using Lyo.HomeInventory.Models;

namespace Lyo.HomeInventory.Web.Components.Tests;

public sealed class HomeProductKeyEntryTests
{
    [Fact]
    public void Serialize_ThenDeserialize_KeepsDuplicateKindsAndNotes()
    {
        List<HomeProductKeyEntry> entries = [
            new() { Kind = HomeProductKeys.WifiMac, Value = "AA:BB:CC:DD:EE:01", Note = "AP radio" },
            new() { Kind = HomeProductKeys.WifiMac, Value = "AA:BB:CC:DD:EE:02", Note = "client NIC" },
            new() { Kind = HomeProductKeys.Sku, Value = "KIT-1" }
        ];

        var json = HomeProductKeyJson.Serialize(entries);
        Assert.Contains("\"kind\":\"WifiMac\"", json, StringComparison.Ordinal);
        Assert.Contains("\"note\":\"AP radio\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ethernet", json, StringComparison.OrdinalIgnoreCase);

        var roundTripped = HomeProductKeyJson.Deserialize(json);
        Assert.Equal(3, roundTripped.Count);
        Assert.Equal(HomeProductKeys.WifiMac, roundTripped[0].Kind);
        Assert.Equal("AA:BB:CC:DD:EE:01", roundTripped[0].Value);
        Assert.Equal("AP radio", roundTripped[0].Note);
        Assert.Equal(HomeProductKeys.WifiMac, roundTripped[1].Kind);
        Assert.Equal("AA:BB:CC:DD:EE:02", roundTripped[1].Value);
        Assert.Equal("client NIC", roundTripped[1].Note);
        Assert.Equal(HomeProductKeys.Sku, roundTripped[2].Kind);
        Assert.Null(roundTripped[2].Note);
    }

    [Fact]
    public void Deserialize_EmptyOrInvalid_ReturnsEmptyList()
    {
        Assert.Empty(HomeProductKeyJson.Deserialize(null));
        Assert.Empty(HomeProductKeyJson.Deserialize(""));
        Assert.Empty(HomeProductKeyJson.Deserialize("not-json"));
    }
}
