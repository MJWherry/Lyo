namespace Lyo.Common;

/// <summary>Helpers for composing deterministic hash codes from one or more values.</summary>
public static class HashCodeHelpers
{
    /// <summary>Combines hash codes for the provided values using a stable unchecked algorithm.</summary>
    /// <param name="values">Values to include in the hash calculation.</param>
    /// <returns>A combined hash code.</returns>
    public static int Combine(params object?[] values)
    {
        unchecked {
            return values.Aggregate(17, (current, value) 
                => (current * 397) ^ (value?.GetHashCode() ?? 0));
        }
    }
}
