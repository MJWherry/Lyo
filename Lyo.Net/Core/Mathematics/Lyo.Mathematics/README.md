# Lyo.Mathematics

C# **contracts** for the Lyo math stack: physical quantities as structs, 2D/3D vectors and small matrices, typed inputs/results for formulas, and a small **registry** for discoverability. **Numerical implementations** live in [`Lyo.Mathematics.Functions`](../Lyo.Mathematics.Functions/README.md) (F#).

**Target frameworks:** `netstandard2.0`, `net10.0`

## Dependencies

- [`Lyo.Common`](../../Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)

---

## Namespaces and folders

| Area | Namespace | Purpose |
|------|-----------|---------|
| Quantities | `Lyo.Mathematics.Quantities` | Strongly typed SI-oriented values (length, mass, angles, …) with factory methods and operators. |
| Vectors | `Lyo.Mathematics.Vectors` | `Vector2D`, `Vector3D` — magnitude, normalize, dot/cross (3D), projections, angles. |
| Matrices | `Lyo.Mathematics.Matrices` | `Matrix2x2`, `Matrix3x3` — element storage for linear algebra functions. |
| Models | `Lyo.Mathematics.Models` | DTO-style `record`/`struct` inputs and results for statistics, calculus, physics, finance, etc. |
| Units (enums) | `Lyo.Mathematics.Units` | `LengthUnit`, `MassUnit`, `AngleUnit`, `TimeUnit` for display or conversions where used. |
| Registry | `Lyo.Mathematics.Registry` | `MathematicsFormulaRegistry`, `FormulaDescriptor` — curated ids for UIs or docs. |
| (root) | `Lyo.Mathematics` | `MathematicsWorkflowExtensions` — a few C# convenience extensions. |

Internal helpers (`MathematicsDisplayFormat`, `MathValueGuards`) support consistent `ToString()` and finite/non-negative validation on quantity constructors.

---

## Quantities (`Lyo.Mathematics.Quantities`)

Most quantities are **`readonly record struct`** types holding a **canonical SI scalar** (e.g. meters, kilograms) plus `From*` factories, common derived accessors, and arithmetic where meaningful. Non-finite or out-of-range values throw via `ArgumentHelpers` / `MathValueGuards` as documented per type.

| Type | SI core (conceptual) |
|------|----------------------|
| `Length` | meters |
| `Mass` | kilograms |
| `TimeInterval` | seconds |
| `Angle` | radians (with degree helpers) |
| `Area` | square meters |
| `Volume` | cubic meters |
| `Velocity` | m/s |
| `Acceleration` | m/s² |
| `Force` | newtons |
| `Momentum` | kg·m/s |
| `Energy` | joules |
| `Power` | watts |
| `Pressure` | pascals |
| `Density` | kg/m³ |
| `Temperature` | kelvin (with Celsius helpers) |
| `Frequency` | hertz |
| `ElectricCurrent` | amperes |
| `Voltage` | volts |
| `Resistance` | ohms |
| `Capacitance` | farads |
| `Torque` | N·m |
| `AngularVelocity` | rad/s |
| `AngularAcceleration` | rad/s² |
| `AngularMomentum` | kg·m²/s |
| `MomentOfInertia` | kg·m² |
| `SpringConstant` | N/m |
| `VolumetricFlowRate` | m³/s |
| `MassFlowRate` | kg/s |
| `DynamicViscosity` | Pa·s |
| `KinematicViscosity` | m²/s |
| `ThermalConductivity` | W/(m·K) |
| `SpecificHeatCapacity` | J/(kg·K) |
| `ThermalExpansionCoefficient` | 1/K |
| `HeatTransferCoefficient` | W/(m²·K) |
| `ModulusOfElasticity` | Pa |
| `Entropy` | J/K |
| `AreaMomentOfInertia` | m⁴ |
| `FractureToughness` | Pa·m^(1/2) |

---

## Vectors and matrices

- **`Vector2D` / `Vector3D`** — components, `Magnitude`, `Normalize()`, `Dot`, `Cross` (3D), `AngleBetween`, `Project`, scalar multiply, add/subtract.
- **`Matrix2x2` / `Matrix3x3`** — `M11`…`M33`, `Identity` (2×2); used by `LinearAlgebraFunctions` in the F# assembly.

---

## Models (`Lyo.Mathematics.Models`)

Typed inputs/results (partial list by concern):

- **Algebra / polynomials:** `PolynomialInput`, `QuadraticEquationInput`, `QuadraticEquationResult`, `LinearSystem2x2Input`, `LinearSystem2x2Result`, `Eigen2x2Result`, `ComplexNumber`.
- **Calculus / numerics:** `NumericalIntegrationInput`, `AdaptiveIntegrationInput`, `DifferentiationInput`, `OdeInput`, `OdeStepResult`, `VectorFunctionInput`, `ScalarMultivariateFunctionInput`, `GradientDescentInput`, `OptimizationResult`, `InterpolationInput`.
- **Statistics:** `WeightedValuesInput`, `LinearRegressionInput`, `LinearRegressionResult`, `DescriptiveStatisticsResult`, `QuartilesResult`, `WeightedStatisticsResult`, `CovarianceCorrelationResult`, `ConfidenceIntervalResult`, distribution parameter records (`NormalDistributionParameters`, `BinomialDistributionParameters`, `PoissonDistributionParameters`, `ExponentialDistributionParameters`, `UniformDistributionParameters`, `GeometricDistributionParameters`, `NegativeBinomialDistributionParameters`), `DistributionSummaryResult`.
- **Geometry:** `CircleMeasurementInput`, `RectangleMeasurementInput`, `RightTriangleInput`, `TriangleInput`.
- **Physics / engineering:** `MomentumInput`, `ForceInput`, `KineticEnergyInput`, `AverageVelocityInput`, `ProjectileMotionInput`, `ProjectileMotionResult`, `AngularMotionInput`, `TorqueInput`, `PowerInput`, `ImpulseInput`, `SpringForceInput`, `WaveInput`, `Collision1DInput`, `Collision1DResult`, `GravitationalForceInput`, `IdealGasLawInput`, `BodyMassIndexInput`, `DensityInput`, `PressureInput`, `OhmsLawInput`.
- **Finance:** `LoanPaymentInput`, `CashFlowSeriesInput`.
- **Linear algebra:** `QrDecompositionResult`, `RootFindingResult`.

Use XML doc on each type in source for field-level detail.

---

## Registry

- **`FormulaDescriptor`** — `record` with `Id`, `Category`, `Library`, `Name`, `Description`, `Signature` (human-oriented hint string).
- **`MathematicsFormulaRegistry.All`** — small curated list of representative capabilities (FFT, QR, adaptive integration, law of cosines, descriptive statistics, arithmetic mean, etc.). **Not** an exhaustive index of every function; for that, see the Functions README and IDE metadata on `*Functions` types.

---

## `MathematicsWorkflowExtensions`

| Extension | Description |
|-----------|-------------|
| `ToMagnitudes(this ComplexNumber[] samples)` | `double[]` of `Magnitude` per element. |
| `TryNormalize(this Vector3D vector, out Vector3D normalized)` | `false` and `(0,0,0)` if zero-length; else unit vector. |

---

## Related projects

- [`Lyo.Mathematics.Functions`](../Lyo.Mathematics.Functions/README.md) — F# implementations.
- `Lyo.Mathematics.Examples` / `Lyo.Mathematics.Benchmarks` — samples and perf harnesses (if present in your solution).

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)
