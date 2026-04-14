# Lyo.Scientific.Functions

`Lyo.Scientific.Functions` is the F# implementation layer for chemistry, astronomy, thermodynamics, fluid dynamics, compressible flow, and solid mechanics.

## Included capabilities

- Chemistry parsing with parentheses support, molar mass, isotope lookup, reaction balancing, and stoichiometric mass estimation.
- Astronomy helpers for gravity, orbital velocity/period, Kepler-style conversions, exoplanet lookup, and magnitude/luminosity transforms.
- Engineering helpers for heat transfer, convection, radiation, Reynolds number, nozzle flow, shocks, section properties, beam stress, fracture, and fatigue-life estimation.

## Example entry points

```csharp
using Lyo.Mathematics;
using Lyo.Scientific.Functions;

var balanced = ChemistryFunctions.BalanceReaction(["CH4", "O2"], ["CO2", "H2O"]);
var obliqueShock = CompressibleFlowFunctions.ObliqueShock(new ObliqueShockInput(2.0, Angle.FromDegrees(40), 1.4));
var life = SolidMechanicsFunctions.FatigueLifeCycles(new SNCurveInput(Pressure.FromPascals(250e6), 900e6, -0.12));
```

Use `ScientificFormulaRegistry.All` when you want to expose a curated set of available production formulas in a UI, workflow, or documentation generator.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Scientific.Functions.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Mathematics`
- `Lyo.Scientific`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `AstronomyFunctions`
- `ChemistryFunctions`
- `CompressibleFlowFunctions`
- `FluidDynamicsFunctions`
- `internal`
- `MechanicsFunctions`
- `SolidMechanicsFunctions`
- `ThermodynamicsFunctions`

<!-- LYO_README_SYNC:END -->

