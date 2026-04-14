using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Eigen2x2Result(ComplexNumber Eigenvalue1, ComplexNumber Eigenvalue2)
{
    public override string ToString() => $"Eigenvalue1={Eigenvalue1}, Eigenvalue2={Eigenvalue2}";
}