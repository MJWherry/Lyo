using Lyo.Web.Primitives;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoToolbarLayoutTests
{
    [Theory]
    [InlineData(false, "lyo-toolbar")]
    [InlineData(true, "lyo-toolbar lyo-toolbar-dense")]
    public void Root_Dense_AppendsDenseClass(bool dense, string expected) => Assert.Equal(expected, LyoToolbarLayout.Root(dense));

    [Fact]
    public void Group_Default_IsBareGroup() => Assert.Equal("lyo-toolbar-group", LyoToolbarLayout.Group(false, false));

    [Fact]
    public void Group_GrowAndEnd_CombinesClasses() => Assert.Equal("lyo-toolbar-group lyo-toolbar-grow lyo-toolbar-end", LyoToolbarLayout.Group(true, true));

    [Theory]
    [InlineData(false, "lyo-toolbar-row")]
    [InlineData(true, "lyo-toolbar-row lyo-toolbar-row-collapsed")]
    public void Row_Collapsed_AppendsHiddenClass(bool collapsed, string expected) => Assert.Equal(expected, LyoToolbarLayout.Row(collapsed));

    [Theory]
    [InlineData(false, "lyo-toolbar-field")]
    [InlineData(true, "lyo-toolbar-field lyo-toolbar-field-grow")]
    public void Field_Grow_AppendsGrowClass(bool grow, string expected) => Assert.Equal(expected, LyoToolbarLayout.Field(grow));

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void IsDisabled_ORsControlAndOwner(bool disabled, bool ownerDisabled, bool expected)
        => Assert.Equal(expected, LyoToolbarLayout.IsDisabled(disabled, ownerDisabled));
}
