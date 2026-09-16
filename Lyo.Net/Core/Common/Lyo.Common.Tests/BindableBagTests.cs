using Lyo.Common.Core;

namespace Lyo.Common.Tests;

public class BindableBagTests
{
    [Fact]
    public void Bind_DoesNotCopyUpFront_ReadsLiveProperty()
    {
        var hero = new Hero { Atk = 1 };
        var bag = new BindableBag();
        bag.Bind(hero);

        Assert.Empty(bag);
        hero.Atk = 5;

        Assert.Equal(5, bag["atk"]);
    }

    [Fact]
    public void Bind_NullSource_IsIgnored()
    {
        var bag = new BindableBag();
        bag.Bind(null);

        Assert.Null(bag["Atk"]);
    }

    [Fact]
    public void Set_ShadowsBoundProperty()
    {
        var hero = new Hero { Atk = 1 };
        var bag = new BindableBag();
        bag.Bind(hero);
        bag.Set("Atk", 9);
        hero.Atk = 4;

        Assert.Equal(9, bag["Atk"]);
    }

    [Fact]
    public void Bind_LaterSourceWins()
    {
        var bag = new BindableBag();
        bag.Bind(new Hero { Atk = 1 });
        bag.Bind(new Hero { Atk = 4 });

        Assert.Equal(4, bag["Atk"]);
    }

    [Fact]
    public void GetBindingType_NullProperty_ReturnsPropertyType()
    {
        var bag = new BindableBag();
        bag.Bind(new Hero { Name = null });

        Assert.Equal(typeof(string), bag.GetBindingType("Name"));
    }

    [Fact]
    public void TryGetPath_BoundObject_ReadsNestedProperty()
    {
        var bag = new BindableBag();
        bag.Bind(new Hero { Gear = new Gear { City = "Austin" } });

        Assert.True(bag.TryGetPath("Gear.City", out var city));
        Assert.Equal("Austin", city);
    }

    [Fact]
    public void CreateScope_SeesParentBinding_AndChildBindShadows()
    {
        var hero = new Hero { Atk = 1 };
        var parent = new BindableBag();
        parent.Bind(hero);
        var child = parent.CreateScope();
        child.Bind(new Hero { Atk = 7 });

        Assert.Equal(7, child["Atk"]);
        Assert.Equal(1, parent["Atk"]);
    }

    [Fact]
    public void Freeze_RejectsBind_AndStillReadsSource()
    {
        var hero = new Hero { Atk = 1 };
        var bag = new BindableBag();
        bag.Bind(hero);
        var frozen = bag.Freeze();
        hero.Atk = 6;

        Assert.Equal(6, frozen["Atk"]);
        Assert.Throws<ContextBagException>(() => frozen.Bind(new Hero()));
    }

    private sealed class Hero
    {
        public int Atk { get; set; }

        public string? Name { get; set; }

        public Gear? Gear { get; set; }
    }

    private sealed class Gear
    {
        public string? City { get; set; }
    }
}
