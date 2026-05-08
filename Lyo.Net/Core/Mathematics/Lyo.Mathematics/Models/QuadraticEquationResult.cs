using System.Diagnostics;

namespace Lyo.Mathematics.Models;

/// <summary>Result values returned from mathematics routines (<c>QuadraticEquationResult</c>).</summary>
/// <remarks>Immutable contract; safe to cache or serialize alongside the originating computation metadata.</remarks>

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuadraticEquationResult(double Discriminant, double? Root1, double? Root2, bool HasRealRoots)
{
    public override string ToString() => $"Discriminant={Discriminant}, Root1={Root1}, Root2={Root2}, HasRealRoots={HasRealRoots}";
}