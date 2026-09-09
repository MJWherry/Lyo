namespace Lyo.Diff.ObjectGraph;

/// <summary>One differing leaf value at a dotted path (for example <c>Address.City</c>).</summary>
public readonly record struct ObjectGraphDifference(string Path, object? OldValue, object? NewValue);