using Lyo.Web.Components;
using Lyo.Web.Components.LyoType;
using Lyo.Web.Primitives;

namespace Lyo.Formatter.Tests;

/// <summary>
/// <see cref="LyoSampleShape" /> stands in for an object that does not exist yet, so what matters is the shape it produces: every readable property present, nesting
/// cut off at the depth the token dropdown shows, and no infinite walk through a self-referencing type.
/// </summary>
public class LyoSampleShapeTests
{
    private sealed class Leaf
    {
        public string City { get; set; } = "";

        public int Population { get; set; }
    }

    private sealed class Branch
    {
        public string Name { get; set; } = "";

        public Leaf? Location { get; set; }
    }

    private sealed class Root
    {
        public Guid Id { get; set; }

        public DateTime? StartedTimestamp { get; set; }

        public Branch? Branch { get; set; }

        public IReadOnlyList<Leaf>? Leaves { get; set; }

        public Dictionary<string, string>? Tags { get; set; }
    }

    private sealed class SelfReferencing
    {
        public string Name { get; set; } = "";

        public SelfReferencing? Parent { get; set; }
    }

    [Fact]
    public void FromType_IncludesEveryReadableProperty()
    {
        var shape = LyoSampleShape.FromType(typeof(Root));

        string[] expected = [nameof(Root.Branch), nameof(Root.Id), nameof(Root.Leaves), nameof(Root.StartedTimestamp), nameof(Root.Tags)];
        Assert.Equal(expected, shape.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void FromType_NestsObjectsAndKeepsLookupsCaseInsensitive()
    {
        var shape = LyoSampleShape.FromType(typeof(Root));

        var branch = Assert.IsType<Dictionary<string, object?>>(shape["branch"]);
        var location = Assert.IsType<Dictionary<string, object?>>(branch["location"]);
        Assert.Equal("city", location["City"]);
    }

    /// <summary>Collections stay empty rather than carrying a sample element, so autocomplete never offers a path a real payload might lack.</summary>
    [Fact]
    public void FromType_LeavesCollectionsAndDictionariesEmpty()
    {
        var shape = LyoSampleShape.FromType(typeof(Root));

        Assert.Empty(Assert.IsType<List<object?>>(shape[nameof(Root.Leaves)]));
        Assert.Empty(Assert.IsType<Dictionary<string, object?>>(shape[nameof(Root.Tags)]));
    }

    [Fact]
    public void FromType_StopsExpandingAtMaxDepth()
    {
        var shape = LyoSampleShape.FromType(typeof(Root), 2);

        var branch = Assert.IsType<Dictionary<string, object?>>(shape[nameof(Root.Branch)]);
        Assert.Equal(nameof(Leaf), branch[nameof(Branch.Location)]);
    }

    [Fact]
    public void FromType_DepthBelowOne_StillExpandsTheRoot()
    {
        var shape = LyoSampleShape.FromType(typeof(Branch), 0);

        Assert.Equal("name", shape[nameof(Branch.Name)]);
        Assert.Equal(nameof(Leaf), shape[nameof(Branch.Location)]);
    }

    [Fact]
    public void FromType_SelfReferencingType_TerminatesAtTheCycle()
    {
        var shape = LyoSampleShape.FromType(typeof(SelfReferencing));

        var parent = Assert.IsType<Dictionary<string, object?>>(shape[nameof(SelfReferencing.Parent)]);
        Assert.Equal(nameof(SelfReferencing), parent[nameof(SelfReferencing.Parent)]);
    }
}
