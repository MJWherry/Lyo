using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class GeographicInfoTests
{
    [Theory]
    [InlineData(USState.AA, "Armed Forces Americas")]
    [InlineData(USState.AE, "Armed Forces Europe")]
    [InlineData(USState.AP, "Armed Forces Pacific")]
    public void FromState_MilitaryMailCode_ResolvesUnitedStatesRow(USState state, string name)
    {
        var info = GeographicInfo.FromState(state);
        Assert.Equal(state, info.State);
        Assert.Equal(CountryCode.US, info.Country);
        Assert.Equal(name, info.StateOrProvinceName);
        Assert.Null(info.TimeZoneId);
    }
}