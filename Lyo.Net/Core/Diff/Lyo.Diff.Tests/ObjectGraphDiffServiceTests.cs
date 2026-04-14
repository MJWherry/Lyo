using Lyo.Diff.ObjectGraph;

namespace Lyo.Diff.Tests;

public sealed class ObjectGraphDiffServiceTests
{
    private readonly IObjectGraphDiffService _diff = new ObjectGraphDiffService();

    [Fact]
    public void Nested_property_differ()
    {
        var a = new Person { Name = "A", Address = new() { City = "X" } };
        var b = new Person { Name = "A", Address = new() { City = "Y" } };
        var d = _diff.GetDifferences(a, b);
        Assert.Contains(d, x => x.Path == "Address.City" && Equals(x.OldValue, "X") && Equals(x.NewValue, "Y"));
    }

    [Fact]
    public void ExcludePath_skips_branch()
    {
        var a = new Person { Name = "A", Address = new() { City = "X" } };
        var b = new Person { Name = "A", Address = new() { City = "Y" } };
        var d = _diff.GetDifferences(a, b, new() { ExcludePath = p => p.StartsWith("Address", StringComparison.Ordinal) });
        Assert.DoesNotContain(d, x => x.Path.StartsWith("Address", StringComparison.Ordinal));
    }

    [Fact]
    public void Cycle_on_same_reference_no_infinite_loop()
    {
        var n = new Node();
        n.Self = n;
        var d = _diff.GetDifferences(n, n);
        Assert.Empty(d);
    }

    [Fact]
    public void CustomEquals_treats_as_equal()
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