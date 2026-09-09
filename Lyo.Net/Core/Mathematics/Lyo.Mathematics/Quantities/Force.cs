using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

/// <summary>Force stored in newtons.</summary>
/// <remarks>May be negative under the sign convention; must be finite.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Force
{
    /// <summary>SI storage scalar in Newtons (canonical unit for this Force).</summary>
    public double Newtons { get; }

    public Force(double newtons) => Newtons = MathValueGuards.Finite(newtons, nameof(newtons));

    public static Force FromNewtons(double newtons) => new(newtons);

    public override string ToString() => $"{Newtons:0.###} N";
}