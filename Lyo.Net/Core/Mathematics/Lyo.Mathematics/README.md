# Lyo.Mathematics

`Lyo.Mathematics` contains the C# contracts, value types, units, vectors, matrices, and typed inputs/results used by the mathematical stack.

## What lives here

- Quantity/value objects such as `Mass`, `Length`, `Velocity`, `Pressure`, and engineering quantities.
- Vector and matrix types for 2D and 3D work.
- Typed formula models for statistics, physics, calculus, optimization, and numerical analysis.
- Formula discovery via `MathematicsFormulaRegistry`.
- Consumer-side helpers such as `MathematicsWorkflowExtensions`.

## Typical usage

```csharp
using Lyo.Mathematics;

var velocity = Velocity.FromMetersPerSecond(12.5);
var angle = Angle.FromDegrees(45);
var vector = new Vector3D(3, 4, 0);

if (vector.TryNormalize(out var normalized))
{
    Console.WriteLine(normalized);
}
```

## Related projects

- [`Lyo.Mathematics.Functions`](../Lyo.Mathematics.Functions/README.md): F# implementations for formulas and algorithms.
- `Lyo.Mathematics.Examples`: runnable usage samples.
- `Lyo.Mathematics.Benchmarks`: lightweight performance harness.


## Dependencies

*(Synchronized from `Lyo.Mathematics.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- [`Lyo.Common`](../../Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)