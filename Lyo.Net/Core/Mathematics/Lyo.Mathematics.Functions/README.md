# Lyo.Mathematics.Functions

F# **implementation** layer for [`Lyo.Mathematics`](../Lyo.Mathematics/README.md): static `*Functions` classes with `static member` entry points callable from C# or F#. Uses **`Lyo.Scientific.ScientificConstants`** for universal physical constants where needed (gravitation, ideal gas).

**Target frameworks:** `netstandard2.0`, `net10.0`

## Dependencies

- [`Lyo.Mathematics`](../Lyo.Mathematics/README.md)
- [`Lyo.Scientific`](../../Scientific/Lyo.Scientific/README.md) — constants only (no circular dependency on `Lyo.Scientific.Functions`)

---

## Calling from C#

```csharp
using Lyo.Mathematics;
using Lyo.Mathematics.Functions;

var mean = StatisticsFunctions.Mean(new[] { 1.0, 2.0, 3.0 });
var fft = TransformFunctions.FastFourierTransform(new[]
{
    new ComplexNumber(1, 0), new ComplexNumber(0, 0),
    new ComplexNumber(-1, 0), new ComplexNumber(0, 0)
});
```

Curated discovery: `MathematicsFormulaRegistry.All` in `Lyo.Mathematics`.

---

## `ArithmeticFunctions`

| Member | Summary |
|--------|---------|
| `Clamp(value, minimum, maximum)` | Finite bounds; throws if `minimum > maximum`. |
| `PercentageChange(originalValue, newValue)` | Percent change vs non-zero original. |
| `GrowthRate(initialValue, finalValue, periods)` | CAGR-style % over positive periods. |
| `CompoundInterest(principal, annualRate, compoundsPerYear, years)` | Discrete compounding. |
| `RatePerSecond(value, elapsedTime)` | `value / elapsedTime.Seconds` (positive duration). |

---

## `AlgebraFunctions`

| Member | Summary |
|--------|---------|
| `EvaluatePolynomial(coefficients, x)` / `EvaluatePolynomial(PolynomialInput)` | Horner-style evaluation. |
| `EvaluatePolynomialDerivative(coefficients, x)` | Derivative at `x`. |
| `SolveLinear(a, b)` | Root of `ax + b = 0`. |
| `SolveQuadratic(QuadraticEquationInput)` | `QuadraticEquationResult` (roots discriminant-aware). |

---

## `GeometryFunctions`

| Member | Summary |
|--------|---------|
| `CircleArea` / `CircleCircumference` | From `CircleMeasurementInput`. |
| `RectangleArea` / `RectanglePerimeter` / `RectangleDiagonal` | From `RectangleMeasurementInput`. |
| `RightTriangleHypotenuse` | `RightTriangleInput`. |
| `DistanceBetween(Vector2D, Vector2D)` | Euclidean distance. |
| `DegreesToRadians` / `RadiansToDegrees` | `double` helpers. |

---

## `TrigonometryFunctions`

| Member | Summary |
|--------|---------|
| `Sin` / `Cos` / `Tan` | `Angle` → `double`. |
| `Asin` / `Acos` / `Atan` | Inverses to angles. |
| `Sinh` / `Cosh` / `Tanh` | Hyperbolic. |
| `LawOfCosinesForSide` | Two sides + included angle → third side. |
| `LawOfCosinesForAngle` | `TriangleInput` → included angle. |
| `LawOfSinesForSide` | Known side/angle pair + target angle. |

---

## `LinearAlgebraFunctions`

| Member | Summary |
|--------|---------|
| `Determinant` | `Matrix2x2` / `Matrix3x3`. |
| `Transpose` | `Matrix2x2` / `Matrix3x3`. |
| `Multiply` | Matrix×matrix, matrix×vector. |
| `FrobeniusNorm` | 2×2 and 3×3. |
| `Inverse` | 2×2 and 3×3 (throws if singular). |
| `Solve3x3` / `Solve2x2` | Linear systems. |
| `QrDecomposition(double[,])` | Full QR for tall matrices; returns `QrDecompositionResult`. |
| `Eigenvalues(Matrix2x2)` | `Eigen2x2Result`. |

---

## `StatisticsFunctions`

| Member | Summary |
|--------|---------|
| `Mode`, `Range`, `Mean`, `Median` | Basic summaries on `double[]`. |
| `Variance` / `StandardDeviation` | Population or sample (`sample` flag). |
| `Describe` | Count, mean, spread, min, max. |
| `MovingAverage`, `ExponentialMovingAverage` | Smoothing. |
| `Percentile`, `Quartiles`, `InterquartileRange` | Order-statistics. |
| `RollingMedian` / `RollingMinimum` / `RollingMaximum` / `RollingStandardDeviation` | Windowed. |
| `ZScore`, `ZScores`, `LatestZScore`, `IsAnomalyByZScore` | Z-score anomaly sketch. |
| `MedianAbsoluteDeviation`, `IsAnomalyByMad` | Robust alternative. |
| `Skewness`, `Kurtosis` | Shape. |
| `Covariance`, `PearsonCorrelation`, `SpearmanCorrelation`, `CovarianceCorrelation` | Bivariate. |
| `WeightedMean`, `WeightedVariance`, `WeightedStatistics` | `WeightedValuesInput`. |
| `MeanConfidenceInterval` | CI for the mean. |
| `LinearRegression` | `LinearRegressionInput` → `LinearRegressionResult`. |

---

## `DistributionsFunctions`

PDF/CDF/summary helpers for: **Normal**, **Binomial**, **Poisson**, **Exponential** (incl. inverse CDF + summary), **Uniform**, **Geometric**, **Negative binomial** (PMF). See parameter types in `Lyo.Mathematics.Models`.

---

## `PhysicsFunctions`

Classical mechanics, waves, circuits, ideal gas, BMI — inputs are `*Input` types or quantities from `Lyo.Mathematics.Quantities`. Includes: momentum, force, kinetic energy, average velocity, projectile motion, angular velocity/acceleration, torque, power, impulse, elastic / perfectly inelastic 1D collisions, gravitational force and potential, spring force and potential, pressure from force/area, density, wave speed and frequency/wavelength relations, Ohm’s-law trio, DC power, series/parallel resistance, series/parallel capacitance, ideal gas `P`/`V`/`T`, `BodyMassIndex`.

---

## `CalculusFunctions`

| Member | Summary |
|--------|---------|
| `TrapezoidalIntegration` / `SimpsonsRule` | `NumericalIntegrationInput`. |
| `Differentiate` | `DifferentiationInput` (finite difference). |
| `Bisection` / `NewtonRaphson` / `Secant` | Root finding on `Func<double,double>`. |
| `EulerSolve` / `RungeKutta4Solve` | `OdeInput` first-order IVP. |
| `LinearInterpolation` (doubles) | Parameter `t`. |
| `AdaptiveIntegration` | `AdaptiveIntegrationInput` (Simpson refinement). |
| `Jacobian` | `VectorFunctionInput`. |
| `Hessian` | `ScalarMultivariateFunctionInput`. |

---

## `InterpolationFunctions`

`Linear`, `InverseLinear`, `Linear(InterpolationInput)`, `PiecewiseLinear`.

---

## `FinancialFunctions`

`FutureValue`, `PresentValue`, `LoanPayment`, `NetPresentValue`.

---

## `SignalFunctions`

`Convolution`, `MovingSum`, `NormalizeMinMax`.

---

## `TransformFunctions`

| Member | Summary |
|--------|---------|
| `FastFourierTransform` | Cooley–Tukey FFT; **length must be power of two**. |
| `InverseFastFourierTransform` | Inverse FFT. |

---

## `OptimizationFunctions`

`GradientDescent(GradientDescentInput)` → `OptimizationResult`.

---

## `ComplexFunctions`

`Conjugate`, `Multiply`, `Divide`, `ToPolar`, `FromPolar`.

---

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)
