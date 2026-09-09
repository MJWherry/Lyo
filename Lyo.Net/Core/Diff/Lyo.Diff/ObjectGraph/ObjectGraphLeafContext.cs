namespace Lyo.Diff.ObjectGraph;

/// <summary>Context passed to optional custom leaf equality.</summary>
public readonly record struct ObjectGraphLeafContext(string Path, Type LeftType, Type RightType, object? OldValue, object? NewValue);