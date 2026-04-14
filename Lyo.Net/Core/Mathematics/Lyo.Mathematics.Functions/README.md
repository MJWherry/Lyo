# Lyo.Mathematics.Functions

`Lyo.Mathematics.Functions` is the F# implementation layer for the reusable mathematics library.

## Included domains

- Arithmetic, algebra, geometry, trigonometry, and linear algebra.
- Statistics, distributions, and financial math.
- Physics, calculus, interpolation, optimization, and signal helpers.
- Numerical tools including FFT, QR decomposition, eigenvalue helpers, adaptive integration, Jacobian estimation, and Hessian estimation.

## Example entry points

```csharp
using Lyo.Mathematics;
using Lyo.Mathematics.Functions;

var fft = TransformFunctions.FastFourierTransform(
[
    new ComplexNumber(1, 0),
    new ComplexNumber(0, 0),
    new ComplexNumber(-1, 0),
    new ComplexNumber(0, 0)
]);

var qr = LinearAlgebraFunctions.QrDecomposition(new double[,]
{
    { 12, -51 },
    { 6, 167 },
    { -4, 24 }
});
```

Use `MathematicsFormulaRegistry.All` when you want a searchable list of the higher-value production formulas exposed by the library.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Mathematics.Functions.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Mathematics`
- `Lyo.Scientific`

## Public API (generated)

Top-level `public` types in `*.cs` (*16*). Nested types and file-scoped namespaces may omit some entries.

- `AlgebraFunctions`
- `ArithmeticFunctions`
- `CalculusFunctions`
- `ComplexFunctions`
- `DistributionsFunctions`
- `FinancialFunctions`
- `GeometryFunctions`
- `internal`
- `InterpolationFunctions`
- `LinearAlgebraFunctions`
- `OptimizationFunctions`
- `PhysicsFunctions`
- `SignalFunctions`
- `StatisticsFunctions`
- `TransformFunctions`
- `TrigonometryFunctions`

<!-- LYO_README_SYNC:END -->

