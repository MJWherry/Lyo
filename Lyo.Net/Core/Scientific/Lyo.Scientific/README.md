# Lyo.Scientific

`Lyo.Scientific` contains the C# scientific domain models, datasets, units, and registries that sit on top of `Lyo.Mathematics`.

## What lives here

- Scientific constants and shared reference values.
- Chemistry models, isotopes, atomic masses, compounds, and reaction records.
- Astronomy models for stars, planets, moons, orbital elements, exoplanets, asteroids, and comets.
- Engineering records for thermodynamics, fluids, compressible flow, solid mechanics, beam catalogs, and fatigue inputs.
- Unit infrastructure for derived units, dimensional compatibility, and SI prefix handling.
- Formula discovery via `ScientificFormulaRegistry`.

## Typical usage

```csharp
using Lyo.Scientific;

var earth = PlanetaryBodies.All.Single(body => body.Name == "Earth");
var compound = new ChemicalCompound("H2O", []);
var pressureUnit = DerivedUnits.BySymbol["Pa"];
var valueInSi = UnitConversion.Convert(1.0, pressureUnit, pressureUnit);
```

## Related projects

- `Lyo.Scientific.Functions`: F# implementations for scientific formulas.
- `Lyo.Mathematics`: shared quantities and typed numerical models.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Scientific.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Mathematics`

## Public API (generated)

Top-level `public` types in `*.cs` (*18*). Nested types and file-scoped namespaces may omit some entries.

- `AstronomyReferenceValues`
- `BeamSectionCatalog`
- `DerivedUnits`
- `ElementAtomicMasses`
- `EngineeringMaterials`
- `Exoplanets`
- `Isotopes`
- `NaturalSatellites`
- `PeriodicTable`
- `PlanetaryBodies`
- `PlanetaryBodyKind`
- `ScientificConstants`
- `ScientificFormulaRegistry`
- `ScientificUnitPrefixes`
- `ScientificWorkflowExtensions`
- `SmallBodies`
- `StellarBodies`
- `UnitConversion`

<!-- LYO_README_SYNC:END -->

