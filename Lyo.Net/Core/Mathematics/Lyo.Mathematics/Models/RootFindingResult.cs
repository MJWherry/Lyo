using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RootFindingResult(double Root, int Iterations, bool Converged)
{
    public override string ToString() => $"Root={Root}, Iterations={Iterations}, Converged={Converged}";
}