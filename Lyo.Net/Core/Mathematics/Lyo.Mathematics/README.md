# Lyo.Mathematics

C# contracts for the Lyo math stack: physical quantities as structs, 2D/3D vectors and small matrices, typed inputs/results for formulas, and a small registry so they can be discovered. Numerical implementations live in [`Lyo.Mathematics.Functions`](../Lyo.Mathematics.Functions/README.md) (F#).

Target frameworks: `netstandard2.0`, `net10.0`

## Folders and namespaces

| Area | Namespace | Purpose |
| ---------- | ---------------------------- | ----------------------------------------------------------------------------------------------- |
| Quantities | `Lyo.Mathematics.Quantities` | Strongly typed SI-oriented values (mass, length, angles, …) with operators and factory methods. |
| Vectors | `Lyo.Mathematics.Vectors` | `Vector3D`, `Vector2D`. Normalize, magnitude, cross/dot (3D), angles, projections. |
| Matrices | `Lyo.Mathematics.Matrices` | `Matrix3x3`, `Matrix2x2`. Element storage for linear algebra functions. |
| Models | `Lyo.Mathematics.Models` | DTO-style `struct`/`record` inputs and results for calculus, statistics, finance, physics, etc. |
| Unit enums | `Lyo.Mathematics.Units` | `MassUnit`, `LengthUnit`, `TimeUnit`, `AngleUnit` for conversions or display where used. |
| Registry | `Lyo.Mathematics.Registry` | `FormulaDescriptor`, `MathematicsFormulaRegistry`. Curated ids for docs or UIs. |
| (root) | `Lyo.Mathematics` | `MathematicsWorkflowExtensions`. A few C# convenience extensions. |

Internal helpers (`MathValueGuards`, `MathematicsDisplayFormat`) support consistent `ToString()` and finite/non-negative validation on quantity constructors.

## Quantities (`Lyo.Mathematics.Quantities`)

Most quantities are `readonly record struct` types holding a canonical SI scalar (e.g. kilograms, meters) plus `From*` factories, common derived accessors, and arithmetic
where meaningful. Non-finite or out-of-range values throw via `MathValueGuards` / `ArgumentHelpers` as documented per type.

| Type | SI core (conceptual) |
| ----------------------------- | --------------------------------- |
| `Length` | meters |
| `Mass` | kilograms |
| `TimeInterval` | seconds |
| `Angle` | radians (degree helpers included) |
| `Area` | area in square meters |
| `Volume` | volume in cubic meters |
| `Velocity` | m/s |
| `Acceleration` | m/s² |
| `Force` | newtons |
| `Momentum` | kg·m/s |
| `Energy` | joules |
| `Power` | watts |
| `Pressure` | pascals |
| `Density` | kg/m³ |
| `Temperature` | kelvin (Celsius helpers included) |
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

## Matrices and vectors

- **Vector3D / Vector2D.** Components, `Magnitude`, `Normalize()`, `Cross` (3D), `Dot`, `Project`, `AngleBetween`, scalar multiply, add/subtract.
- **Matrix3x3 / Matrix2x2.** `M11`…`M33`, `Identity` (2×2). Used by `LinearAlgebraFunctions` in the F# assembly.

## Models (`Lyo.Mathematics.Models`)

- **Algebra / polynomials.** `QuadraticEquationInput`, `PolynomialInput`, `QuadraticEquationResult`, `LinearSystem2x2Result`, `LinearSystem2x2Input`, `ComplexNumber`, `Eigen2x2Result`.
- **Calculus / numerics.** `AdaptiveIntegrationInput`, `NumericalIntegrationInput`, `OdeInput`, `DifferentiationInput`, `VectorFunctionInput`, `OdeStepResult`, `GradientDescentInput`, `ScalarMultivariateFunctionInput`, `InterpolationInput`, `OptimizationResult`.
- **Statistics.** `LinearRegressionInput`, `WeightedValuesInput`, `DescriptiveStatisticsResult`, `LinearRegressionResult`, `WeightedStatisticsResult`, `QuartilesResult`, `ConfidenceIntervalResult`, `CovarianceCorrelationResult`, distribution parameter records (`BinomialDistributionParameters`, `NormalDistributionParameters`, `ExponentialDistributionParameters`, `PoissonDistributionParameters`, `GeometricDistributionParameters`, `UniformDistributionParameters`, `NegativeBinomialDistributionParameters`), `DistributionSummaryResult`.
- **Geometry.** `RectangleMeasurementInput`, `CircleMeasurementInput`, `TriangleInput`, `RightTriangleInput`.
- **Physics / engineering.** `ForceInput`, `MomentumInput`, `AverageVelocityInput`, `KineticEnergyInput`, `ProjectileMotionResult`, `ProjectileMotionInput`, `TorqueInput`, `AngularMotionInput`, `ImpulseInput`, `PowerInput`, `WaveInput`, `SpringForceInput`, `Collision1DResult`, `Collision1DInput`, `IdealGasLawInput`, `GravitationalForceInput`, `DensityInput`, `BodyMassIndexInput`, `OhmsLawInput`, `PressureInput`.
- **Finance.** `CashFlowSeriesInput`, `LoanPaymentInput`.
- **Linear algebra.** `RootFindingResult`, `QrDecompositionResult`.

## Registry

- **FormulaDescriptor.** `record` with `Category`, `Id`, `Name`, `Library`, `Description`, `Signature` (human-oriented hint string).
- **MathematicsFormulaRegistry.All.** Small curated list of representative capabilities (QR, FFT, law of cosines, adaptive integration, arithmetic mean, descriptive statistics, and similar). Not an exhaustive index of every function. For that, see the Functions README and IDE metadata on `*Functions` types.

## `MathematicsWorkflowExtensions`

| Extension | Description |
| ------------------------------------------------------------- | ------------------------------------------------------- |
| `ToMagnitudes(this ComplexNumber[] samples)` | `double[]` of `Magnitude` per element. |
| `TryNormalize(this Vector3D vector, out Vector3D normalized)` | `false` and `(0,0,0)` if zero-length; else unit vector. |

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)