using Lyo.Common.Core;

namespace Lyo.Common.Tests;

public class ContextBagTests
{
    [Fact]
    public void TryGetValue_MissingKey_ReturnsFalse()
    {
        var bag = new ContextBag();
        Assert.False(bag.TryGetValue("Atk", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_SetValue_ReturnsStoredValue()
    {
        var bag = new ContextBag();
        bag.Set("Atk", 10);
        Assert.True(bag.TryGetValue("Atk", out var value));
        Assert.Equal(10, value);
    }

    [Fact]
    public void Indexer_MissingKey_ReturnsNull() => Assert.Null(new ContextBag()["Atk"]);

    [Fact]
    public void Set_DifferentCase_ReplacesSameEntry()
    {
        var bag = new ContextBag();
        bag.Set("Atk", 1);
        bag.Set("atk", 2);
        Assert.Single(bag);
        Assert.Equal(2, bag["ATK"]);
    }

    [Fact]
    public void Set_CaseSensitive_KeepsAtkAndAtkApart()
    {
        var bag = new ContextBag(true);
        bag.Set("Atk", 1);
        bag.Set("atk", 2);
        Assert.Equal(2, bag.Count);
        Assert.Equal(1, bag["Atk"]);
        Assert.Equal(2, bag["atk"]);
    }

    [Fact]
    public void Set_IntKey_RoundTrips()
    {
        var bag = new ContextBag<int>();
        bag.Set(7, "seven");
        Assert.True(bag.TryGetValue(7, out var value));
        Assert.Equal("seven", value);
        Assert.False(bag.TryGetValue(8, out var _));
    }

    [Fact]
    public void TryGetAs_MissingKey_ReturnsFalse()
    {
        var bag = new ContextBag();
        Assert.False(bag.TryGetAs<int>("Atk", out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetAs_NullStored_Int_ReturnsFalse()
    {
        var bag = new ContextBag();
        bag.Set("Atk", (object?)null);
        Assert.False(bag.TryGetAs<int>("Atk", out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetAs_NullStored_NullableInt_ReturnsTrueWithNull()
    {
        var bag = new ContextBag();
        bag.Set("Atk", (object?)null);
        Assert.True(bag.TryGetAs<int?>("Atk", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void TryGetAs_NullStored_String_ReturnsTrueWithNull()
    {
        var bag = new ContextBag();
        bag.Set("Name", (object?)null);
        Assert.True(bag.TryGetAs<string?>("Name", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void TryGetAs_StringNumber_ReturnsInt()
    {
        var bag = new ContextBag();
        bag.Set("Atk", "42");
        Assert.True(bag.TryGetAs<int>("Atk", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetAs_IncompatibleValue_ReturnsFalse()
    {
        var bag = new ContextBag();
        bag.Set("Atk", "nope");
        Assert.False(bag.TryGetAs<int>("Atk", out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Set_Factory_DoesNotRunUntilRead()
    {
        var bag = new ContextBag();
        var calls = 0;
        bag.Set(
            "Atk", () => {
                calls++;
                return 10;
            });

        Assert.Equal(0, calls);
        Assert.True(bag.ContainsKey("Atk"));
        Assert.Equal(0, calls);
        Assert.Equal(10, bag["Atk"]);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void TryGetValue_Factory_RunsOnceThenReturnsStoredResult()
    {
        var bag = new ContextBag();
        var calls = 0;
        bag.Set(
            "Atk", () => {
                calls++;
                return 10;
            });

        Assert.Equal(10, bag["Atk"]);
        Assert.Equal(10, bag["atk"]);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Set_FactoryThrows_LeavesKeyUnset()
    {
        var bag = new ContextBag();
        bag.Set("Atk", () => throw new InvalidOperationException("boom"));
        Assert.Throws<InvalidOperationException>(() => bag.TryGetValue("Atk", out var _));
        Assert.False(bag.ContainsKey("Atk"));
        Assert.Empty(bag);
    }

    [Fact]
    public void GetOrAdd_ExistingKey_ReturnsStoredWithoutRunningFactory()
    {
        var bag = new ContextBag();
        bag.Set("Atk", 1);
        var calls = 0;
        var value = bag.GetOrAdd(
            "atk", () => {
                calls++;
                return 9;
            });

        Assert.Equal(1, value);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void GetOrAdd_MissingKey_StoresAndReturnsFactoryResult()
    {
        var bag = new ContextBag();
        var calls = 0;
        var first = bag.GetOrAdd(
            "Atk", () => {
                calls++;
                return 4;
            });

        var second = bag.GetOrAdd(
            "Atk", () => {
                calls++;
                return 8;
            });

        Assert.Equal(4, first);
        Assert.Equal(4, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Copy_PendingFactory_DoesNotRunAndRemainsPendingOnBoth()
    {
        var bag = new ContextBag();
        var calls = 0;
        bag.Set(
            "Atk", () => {
                calls++;
                return 3;
            });

        var copy = bag.Copy();
        Assert.Equal(0, calls);
        Assert.Equal(3, copy["Atk"]);
        Assert.Equal(1, calls);
        Assert.Equal(3, bag["Atk"]);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Copy_CaseSensitive_PreservesSensitivity()
    {
        var bag = new ContextBag(true);
        bag.Set("Atk", 1);
        var copy = bag.Copy();
        copy.Set("atk", 2);
        Assert.Equal(1, bag["Atk"]);
        Assert.Null(bag["atk"]);
        Assert.Equal(1, copy["Atk"]);
        Assert.Equal(2, copy["atk"]);
    }

    [Fact]
    public void Import_IgnoreCaseDestination_FoldsKeys()
    {
        var source = new ContextBag(true);
        source.Set("Atk", 5);
        var destination = new ContextBag();
        destination.Set("atk", 1);
        destination.Import(source);
        Assert.Equal(5, destination["ATK"]);
        Assert.Single(destination);
    }

    [Fact]
    public void Set_NullKey_Throws()
    {
        var bag = new ContextBag<string>(StringComparer.Ordinal);
        Assert.Throws<ArgumentNullException>(() => bag.Set(null!, 1));
    }

    [Fact]
    public void Set_EmptyStringKey_Throws()
    {
        var bag = new ContextBag();
        Assert.Throws<ArgumentException>(() => bag.Set("", 1));
    }

    [Fact]
    public void CreateScope_ChildShadowsParent_ParentKeepsItsValue()
    {
        var parent = new ContextBag();
        parent.Set("Atk", 1);
        var child = parent.CreateScope();
        Assert.Equal(1, child["Atk"]);
        child.Set("Atk", 2);
        Assert.Equal(2, child["Atk"]);
        Assert.Equal(1, parent["Atk"]);
    }

    [Fact]
    public void Set_BagFactory_ReadsSiblingOnce()
    {
        var bag = new ContextBag();
        bag.Set("Atk", 10);
        var calls = 0;
        bag.Set(
            "Damage", current => {
                calls++;
                return current.GetRequired<int>("Atk") * 2;
            });

        Assert.Equal(0, calls);
        Assert.Equal(20, bag["Damage"]);
        Assert.Equal(20, bag["Damage"]);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Set_BagFactory_Cycle_ThrowsAndLeavesKeyUnset()
    {
        var bag = new ContextBag();
        bag.Set("A", current => current.GetRequired<int>("A"));
        var error = Assert.Throws<ContextBagException>(() => bag.TryGetValue("A", out var _));
        Assert.Contains("A", error.Message);
        Assert.False(bag.ContainsKey("A"));
    }

    [Fact]
    public void TryGetPath_NestedBag_ReturnsLeaf()
    {
        var ship = new ContextBag();
        ship.Set("City", "Austin");
        var order = new ContextBag();
        order.Set("ShipTo", ship);
        var bag = new ContextBag();
        bag.Set("Order", order);
        Assert.True(bag.TryGetPath("Order.ShipTo.City", out var city));
        Assert.Equal("Austin", city);
    }

    [Fact]
    public void TryGetPath_Dictionary_ReturnsLeaf()
    {
        var bag = new ContextBag();
        bag.Set("Order", new Dictionary<string, object?> { ["City"] = "Austin" });
        Assert.True(bag.TryGetPath("Order.City", out var city));
        Assert.Equal("Austin", city);
        Assert.False(bag.TryGetPath("Order.Missing", out var _));
    }

    [Fact]
    public void TryGetPath_PlainObject_DoesNotReflect()
    {
        var bag = new ContextBag();
        bag.Set("Player", new SamplePlayer(3));
        Assert.False(bag.TryGetPath("Player.Atk", out var _));
    }

    [Fact]
    public void Prefix_ReadAndWrite_UsesDottedKeyOnRoot()
    {
        var bag = new ContextBag();
        var player = bag.Prefix("player");
        player.Set("Atk", 10);
        Assert.Equal(10, bag["player.Atk"]);
        Assert.Equal(10, player["Atk"]);
    }

    [Fact]
    public void Freeze_CopiesPendingFactory_AndRejectsWrites()
    {
        var bag = new ContextBag();
        var calls = 0;
        bag.Set(
            "Atk", () => {
                calls++;
                return 4;
            });

        var frozen = bag.Freeze();
        Assert.Equal(0, calls);
        Assert.Throws<ContextBagException>(() => frozen.Set("Def", 1));
        Assert.Equal(4, frozen["Atk"]);
        Assert.Equal(1, calls);
        Assert.Equal(4, bag["Atk"]);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Freeze_UnreadParentKey_SeesLaterParentSet()
    {
        var parent = new ContextBag();
        parent.Set("Atk", 1);
        var frozen = parent.CreateScope().Freeze();
        parent.Set("Atk", 9);
        Assert.Equal(9, frozen["Atk"]);
    }

    [Fact]
    public void GetRequired_MissingKey_ThrowsNamingKey()
    {
        var bag = new ContextBag();
        var error = Assert.Throws<ContextBagException>(() => bag.GetRequired<int>("Atk"));
        Assert.Contains("Atk", error.Message);
    }

    [Fact]
    public void GetRequired_NullAndConversion_FollowTryGetAs()
    {
        var bag = new ContextBag();
        bag.Set("Atk", (object?)null);
        Assert.Throws<ContextBagException>(() => bag.GetRequired<int>("Atk"));
        Assert.Null(bag.GetRequired<int?>("Atk"));
        bag.Set("Atk", "nope");
        Assert.Throws<ContextBagException>(() => bag.GetRequired<int>("Atk"));
        bag.Set("Atk", "15");
        Assert.Equal(15, bag.GetRequired<int>("Atk"));
    }

    private sealed record SamplePlayer(int Atk);
}