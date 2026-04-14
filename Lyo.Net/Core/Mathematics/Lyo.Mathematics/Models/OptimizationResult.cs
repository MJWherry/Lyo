using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct OptimizationResult(double Value, int Iterations)
{
    public override string ToString() => $"Value={Value}, Iterations={Iterations}";
}