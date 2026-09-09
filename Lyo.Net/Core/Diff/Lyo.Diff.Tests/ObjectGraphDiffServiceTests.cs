using Lyo.Diff.ObjectGraph;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Lyo.Diff.Tests;

public sealed class ObjectGraphDiffServiceTests
{
    private readonly IObjectGraphDiffService _diff = new ObjectGraphDiffService();

    [Fact]
    public void Nested_Property_Differ()
    {
        var a = new Person { Name = "A", Address = new() { City = "X" } };
        var b = new Person { Name = "A", Address = new() { City = "Y" } };
        var d = _diff.GetDifferences(a, b);
        Assert.Contains(d, x => x.Path == "Address.City" && Equals(x.OldValue, "X") && Equals(x.NewValue, "Y"));
    }

    [Fact]
    public void ExcludePath_Skips_Branch()
    {
        var a = new Person { Name = "A", Address = new() { City = "X" } };
        var b = new Person { Name = "A", Address = new() { City = "Y" } };
        var d = _diff.GetDifferences(a, b, new() { ExcludePath = p => p.StartsWith("Address", StringComparison.Ordinal) });
        Assert.DoesNotContain(d, x => x.Path.StartsWith("Address", StringComparison.Ordinal));
    }

    [Fact]
    public void Cycle_On_Same_Reference_No_Infinite_Loop()
    {
        var n = new Node();
        n.Self = n;
        var d = _diff.GetDifferences(n, n);
        Assert.Empty(d);
    }

    [Fact]
    public void CustomEquals_Treats_As_Equal()
    {
        var a = new Person { Name = "x", Address = null };
        var b = new Person { Name = "y", Address = null };
        var d = _diff.GetDifferences(a, b, new() { CustomEquals = ctx => ctx.Path == "Name" });
        Assert.Empty(d);
    }

    private sealed class Person
    {
        public string Name { get; set; } = "";

        public Address? Address { get; set; }
    }

    private sealed class Address
    {
        public string City { get; set; } = "";
    }

    private sealed class Node
    {
        public Node? Self { get; set; }
    }
}