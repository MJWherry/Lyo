namespace Lyo.Common.Core;

/// <summary>Combines one or more values into a deterministic hash code.</summary>
public static class HashCodeHelpers
{
    /// <summary>Folds hash codes from <paramref name="values" /> with a stable unchecked mix.</summary>
    /// <param name="values">Values folded into the hash.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine(params object?[] values)
    {
        unchecked {
            return values.Aggregate(17, (current, value) => (current * 397) ^ (value?.GetHashCode() ?? 0));
        }
    }
}