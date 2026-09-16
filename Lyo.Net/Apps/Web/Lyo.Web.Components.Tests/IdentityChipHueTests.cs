using Lyo.Api.Models.Enums;
using MudBlazor;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.Cache;
using Lyo.Web.Components.LyoType;
using Lyo.Web.Primitives;

namespace Lyo.Web.Components.Tests;

public sealed class IdentityChipHueTests
{
    [Fact]
    public void CacheForType_KeyIsBlue_TagIsAmber_UnknownIsNull()
    {
        Assert.Equal(LyoChipHue.Blue, CacheColorHelper.ForType(CacheItemTypeEnum.Key));
        Assert.Equal(LyoChipHue.Amber, CacheColorHelper.ForType(CacheItemTypeEnum.Tag));
        Assert.Null(CacheColorHelper.ForType("nope"));
    }

    [Fact]
    public void LyoType_Integer_IsTeal_RegexStaysWarning_UnknownHasNoHue()
    {
        var integer = LyoTypeUi.ForType(LyoTypeInfo.Int.FullName);
        Assert.Equal(LyoChipHue.Teal.Style, integer.Style);
        Assert.Equal(Color.Default, integer.Color);

        var regex = LyoTypeUi.ForType(LyoTypeInfo.Regex.FullName);
        Assert.Equal(Color.Warning, regex.Color);
        Assert.Null(regex.Style);

        var unknown = LyoTypeUi.ForType("Not.A.Type");
        Assert.Null(unknown.Style);
        Assert.Equal(Color.Default, unknown.Color);
    }
}
