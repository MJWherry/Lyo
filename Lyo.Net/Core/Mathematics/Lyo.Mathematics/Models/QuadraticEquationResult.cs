using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct QuadraticEquationResult(double Discriminant, double? Root1, double? Root2, bool HasRealRoots)
{
    public override string ToString() => $"Discriminant={Discriminant}, Root1={Root1}, Root2={Root2}, HasRealRoots={HasRealRoots}";
}