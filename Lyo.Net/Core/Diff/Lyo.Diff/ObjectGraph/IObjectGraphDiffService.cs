namespace Lyo.Diff.ObjectGraph;

/// <summary>Compares two object graphs and reports leaf differences as dotted paths.</summary>
public interface IObjectGraphDiffService
{
    /// <summary>Finds property-level differences between <paramref name="left" /> and <paramref name="right" />.</summary>
    IReadOnlyList<ObjectGraphDifference> GetDifferences(object? left, object? right, ObjectGraphDiffOptions? options = null);
}