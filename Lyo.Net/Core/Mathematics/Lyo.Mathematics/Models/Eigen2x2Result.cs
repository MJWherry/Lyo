using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Result values returned from mathematics routines (<c>Eigen2x2Result</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize alongside the originating computation metadata.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
// ReSharper disable once InconsistentNaming
public readonly record struct Eigen2x2Result(ComplexNumber Eigenvalue1, ComplexNumber Eigenvalue2)
{
    public override string ToString() => $"Eigenvalue1={Eigenvalue1}, Eigenvalue2={Eigenvalue2}";
}