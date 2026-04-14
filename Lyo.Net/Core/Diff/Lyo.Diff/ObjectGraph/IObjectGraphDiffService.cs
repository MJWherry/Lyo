namespace Lyo.Diff.ObjectGraph;

/// <summary>Compares two object graphs and reports leaf differences with dotted paths.</summary>
public interface IObjectGraphDiffService
{
    /// <summary>Computes property-level differences between <paramref name="left" /> and <paramref name="right" />.</summary>
    IReadOnlyList<ObjectGraphDifference> GetDifferences(object? left, object? right, ObjectGraphDiffOptions? options = null);
}