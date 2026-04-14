namespace Lyo.Common.Records;

/// <summary>Represents a difference between two object property values.</summary>
/// <param name="Name">The property name.</param>
/// <param name="OldValue">The value from the first object.</param>
/// <param name="NewValue">The value from the second object.</param>
public sealed record PropertyDifference(string Name, object? OldValue, object? NewValue);