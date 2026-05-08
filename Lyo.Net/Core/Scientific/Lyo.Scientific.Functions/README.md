# Lyo.Scientific.Functions

F# **formulas** for chemistry, orbital mechanics, thermodynamics, fluid and compressible flow, rigid-body style mechanics, and solid mechanics — built on [`Lyo.Scientific`](../Lyo.Scientific/README.md) models and [`Lyo.Mathematics`](../Mathematics/Lyo.Mathematics/README.md) quantities.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Dependencies

- [`Lyo.Scientific`](../Lyo.Scientific/README.md)
- [`Lyo.Mathematics`](../Mathematics/Lyo.Mathematics/README.md)

---

## Calling from C#

```csharp
using Lyo.Mathematics;
using Lyo.Scientific.Engineering;
using Lyo.Scientific.Functions;

var balanced = ChemistryFunctions.BalanceReaction(new[] { "CH4", "O2" }, new[] { "CO2", "H2O" });
var shock = CompressibleFlowFunctions.ObliqueShock(new ObliqueShockInput(2.0, Angle.FromDegrees(40), 1.4));
var life = SolidMechanicsFunctions.FatigueLifeCycles(
    new SNCurveInput(Pressure.FromPascals(250e6), 900e6, -0.12));
```

Discovery: `ScientificFormulaRegistry.All` in `Lyo.Scientific`.

---

## `ChemistryFunctions`

| Member | Summary |
|--------|---------|
| `AllElements` / `AllIsotopes` | Arrays from `PeriodicTable` / `Isotopes.Common`. |
| `GetElementByAtomicNumber` / `GetElementBySymbol` / `GetElementByName` | Lookup or throw. |
| `GetIsotopesBySymbol` | Filtered isotope array. |
| `ParseFormula` | Parses formula string (incl. parentheses) to structured parts. |
| `MolarMass` | Grams per mole from formula string. |
| `MolesFromMass` / `MassFromMoles` | Stoichiometry helpers. |
| `StoichiometricMassRatio` | Mass ratio between formulas given balanced stoichiometry assumption. |
| `BalanceReaction(string[] reactants, string[] products)` | Integer coefficients; string array overload. |
| `BalanceReaction(ChemicalReaction)` | Record overload. |
| `StoichiometricProductMass` | Product mass from reactant mass + balanced result. |

---

## `AstronomyFunctions`

| Member | Summary |
|--------|---------|
| `AllPlanetaryBodies` | `PlanetaryBodies.All` as array. |
| `GetPlanetaryBodyByName` | Case-insensitive. |
| `SurfaceGravity` / `EscapeVelocity` / `OrbitalCircumference` | From `PlanetaryBody`. |
| `OrbitalVelocity` / `OrbitalPeriod` | `Mass` + `Length` (circular-orbit style helpers). |
| `SurfaceFlux` / `EquilibriumTemperature` | Radiation sketches from luminosity, distance, albedo. |
| `DistanceInAstronomicalUnits` / `DistanceFromAstronomicalUnits` | Length ↔ AU. |
| `ApparentFluxFromLuminosity` | Inverse-square flux scaling. |
| `GetStarByName` / `GetMoonByName` / `GetExoplanetByName` / `GetAsteroidByName` / `GetCometByName` | Catalog lookups. |
| `PeriapsisDistance` / `ApoapsisDistance` | From `OrbitalElements`. |
| `MeanMotion` | From central mass + elements. |
| `SemiMajorAxisFromPeriod` | Kepler-related conversion. |
| `LuminosityRatioFromMagnitudeDifference` / `MagnitudeDifferenceFromLuminosityRatio` | Log flux ratios. |
| `AbsoluteMagnitudeFromLuminosity` / `ApparentMagnitudeFromFlux` | Magnitude conventions. |

---

## `ThermodynamicsFunctions`

| Member | Summary |
|--------|---------|
| `HeatEnergy` | `HeatTransferInput`. |
| `ConductionRate` | `ConductionInput`. |
| `ThermalExpansion` | `ThermalExpansionInput`. |
| `CarnotEfficiency` | Two reservoir temperatures. |
| `EntropyChange` | \(Q/T\) style helper. |
| `InternalEnergyChange` | Idealized degrees-of-freedom model. |
| `SpeedOfSoundIdealGas` | From \(T\), \(\gamma\), molar mass. |
| `ConvectiveHeatTransferRate` | `ConvectiveHeatTransferInput`. |
| `RadiativeHeatTransferRate` | `RadiativeHeatTransferInput`. |
| `HeatExchanger` | `HeatExchangerInput` effectiveness-NTU style helper. |
| `PrandtlNumber` | From \(c_p\), \(\mu\), \(k\). |
| `GrashofNumber` | `NaturalConvectionInput`. |
| `NusseltDittusBoelter` / `HeatTransferCoefficientFromNusselt` | `ConvectionCorrelationInput`. |
| `RadiationExchangeRate` | `RadiationExchangeInput`. |

---

## `FluidDynamicsFunctions`

| Member | Summary |
|--------|---------|
| `ReynoldsNumber` | `ReynoldsNumberInput`. |
| `DynamicPressure` | \(\frac{1}{2}\rho v^2\). |
| `VolumetricFlowRate` / `VelocityFromFlowRate` | Duct relations. |
| `DragForce` | `DragForceInput`. |
| `BuoyantForce` | `BuoyancyInput`. |
| `DarcyWeisbachPressureDrop` | `PipeFlowInput`. |
| `BernoulliTotalPressure` | Static + dynamic head. |
| `MachNumber` | Velocity / speed of sound. |

---

## `CompressibleFlowFunctions`

| Member | Summary |
|--------|---------|
| `StaticTemperature` / `StaticPressure` | `CompressibleFlowInput` isentropic relations. |
| `ChokedMassFlowRate` | `NozzleFlowInput` + throat `Area`. |
| `NozzleExitVelocity` | Exit speed from nozzle input. |
| `NozzleFlow` | Mass flow + exit velocity tuple-style result. |
| `IsentropicAreaMachRatio` | Area–Mach relation for given \(\gamma\). |
| `SolveMachFromAreaRatio` | Subsonic vs supersonic branch flag. |
| `DownstreamMachNormalShock` / `PressureRatioNormalShock` / `TemperatureRatioNormalShock` | `NormalShockInput`. |
| `ObliqueShock` | `ObliqueShockInput` → `ObliqueShockResult`. |

---

## `MechanicsFunctions`

| Member | Summary |
|--------|---------|
| `MomentOfInertiaSolidCylinder` / `MomentOfInertiaSolidSphere` | `RotationalInertiaInput`. |
| `MomentOfInertiaRodAboutCenter` | Thin rod, center axis. |
| `RotationalKineticEnergy` | `RotationalEnergyInput`. |
| `AngularMomentum` | `AngularMomentumInput`. |
| `SpringOscillationPeriod` | `SpringOscillatorInput`. |
| `PendulumPeriod` | Small-angle style `PendulumInput`. |
| `MechanicalAdvantage` | Force ratio. |

---

## `SolidMechanicsFunctions`

| Member | Summary |
|--------|---------|
| `NormalStress` / `NormalStrain` / `YoungsModulus` | `StressStrainInput`. |
| `HookeExtension` | Axial deformation from force/geometry/modulus. |
| `CantileverEndDeflection` | `BeamBendingInput` (simple cantilever model). |
| `BeamBendingStress` | `BeamBendingInput` and overload with `BeamSectionProfile`. |
| `CriticalFractureStress` | `FractureInput`. |
| `FactorOfSafety` | Allowable vs applied stress. |
| `RectangularAreaMomentOfInertia` / `CircularAreaMomentOfInertia` | Section properties. |
| `RectangularSectionModulus` | Elastic section modulus. |
| `GoodmanFactorOfSafety` | `FatigueInput` (Goodman criterion). |
| `FatigueLifeCycles` | `SNCurveInput` (Basquin-style exponent model). |
| `FatigueDamageFraction` | Miner-style damage ratio from cycles. |

---

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)
