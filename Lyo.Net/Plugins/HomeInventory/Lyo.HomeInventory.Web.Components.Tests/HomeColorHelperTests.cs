using Lyo.Common.Core.Identifiers;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.HomeInventory.Web.Components.Tests;

public sealed class HomeColorHelperTests
{
    [Fact]
    public void ForStatus_Active_IsSuccess() => Assert.Equal(Color.Success, HomeColorHelper.ForStatus(HomeItemStatus.Active));

    [Fact]
    public void ForStatus_String_Parses() => Assert.Equal(Color.Warning, HomeColorHelper.ForStatus("Discontinued"));

    [Fact]
    public void ForCondition_Damaged_IsError() => Assert.Equal(Color.Error, HomeColorHelper.ForCondition(HomeItemCondition.Damaged));

    [Fact]
    public void ForRole_Photo_IsRose_AndUnknown_IsNull()
    {
        Assert.Equal(LyoChipHue.Rose, HomeColorHelper.ForRole(HomeAttachmentRole.Photo));
        Assert.Null(HomeColorHelper.ForRole((HomeAttachmentRole)999));
    }
}

public sealed class HomeInventoryFilePathsTests
{
    [Fact]
    public void ForItem_UsesStablePrefix()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal("home-inventory/items/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", HomeInventoryFilePaths.ForItem(id));
    }

    [Fact]
    public void ForEntity_Location_MatchesForLocation()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.Equal(HomeInventoryFilePaths.ForLocation(id), HomeInventoryFilePaths.ForEntity(HomeInventoryEntityTypes.Location, id));
    }

    [Fact]
    public void ForEntity_Unknown_Throws() => Assert.Throws<ArgumentException>(() => HomeInventoryFilePaths.ForEntity("Nope", LyoGuid.CreateV7()));
}