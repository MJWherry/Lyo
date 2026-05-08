# Lyo.Scientific

Scientific **domain models**, **reference datasets**, **SI-oriented unit helpers**, and **formula discovery** built on [`Lyo.Mathematics`](../Mathematics/Lyo.Mathematics/README.md). Numerical formulas that operate on these types are in [`Lyo.Scientific.Functions`](../Lyo.Scientific.Functions/README.md) (F#).

**Target frameworks:** `netstandard2.0`, `net10.0`

## Dependencies

- [`Lyo.Common`](../../Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Mathematics`](../Mathematics/Lyo.Mathematics/README.md)

---

## `ScientificConstants`

Named **CODATA-style** double constants (SI): gravitational constant, standard gravity, speed of light, universal gas constant, vacuum permittivity, Planck constant, Avogadro, Boltzmann, elementary charge, plus `Pi` and `Tau`. Use for physics formulas (see `PhysicsFunctions` in `Lyo.Mathematics.Functions`).

---

## Units (`Lyo.Scientific.Units`)

| Type | Role |
|------|------|
| `QuantityDimension` | Exponents for M, L, T, I, Θ, N, J (luminous intensity); `Dimensionless`. |
| `DerivedUnitDefinition` | Name, symbol, `QuantityDimension`, `ToSiFactor` (multiply user value → SI). |
| `DerivedUnits.BySymbol` | Built-in map (e.g. `N`, `J`, `W`, `Pa`, `Hz`, `C`, `V`, `ohm`). |
| `DimensionedValue` | `ValueSi` + `Dimension` (validated finite). |
| `UnitConversion.Convert(value, from, to)` | Same-dimension conversion. |
| `UnitConversion.EnsureCompatible` | Throws if dimensions differ. |
| `UnitConversion.Add` | Adds SI values after dimension check. |
| `UnitConversion.ApplyPrefix` | Scales by metric prefix multiplier. |
| `ScientificUnitPrefix` / `ScientificUnitPrefixes.Metric` | T, G, M, k, h, da, d, c, m, μ, n, p prefixes. |

---

## Astronomy (`Lyo.Scientific.Astronomy`)

| Type / static | Description |
|-----------------|---------------|
| `PlanetaryBodyKind` | `Planet`, `DwarfPlanet`. |
| `PlanetaryBody` | Name, mass, radii, semi-major axis, sidereal orbit/rotation, mean surface temperature, moon count, rings. |
| `PlanetaryBodies.All` | Mercury … Pluto catalog. |
| `Star` | Name, mass, radius, luminosity, surface temperature. |
| `StellarBodies.All` | Sun entry. |
| `Moon` | Name, parent planet, mass, radius, semi-major axis, orbital period. |
| `NaturalSatellites.All` | Moon (Earth). |
| `AstronomyReferenceValues` | `AstronomicalUnit`, `LightYear`, `Parsec`, `SolarMass`, `SolarRadius`. |
| `OrbitalElements` | Classical elements + `EpochUtc` (eccentricity in [0,1)). |
| `Exoplanet` | Name, host `Star`, `OrbitalElements`, optional mass/radius, habitability flag. |
| `Exoplanets.All` | Sample catalog (e.g. Proxima Centauri b). |
| `Asteroid`, `Comet` | Small-body records with `OrbitalElements`, physical parameters. |
| `SmallBodies.Asteroids` / `SmallBodies.Comets` | Example entries (Ceres, 1P/Halley). |

---

## Chemistry (`Lyo.Scientific.Chemistry`)

| Type / static | Description |
|-----------------|---------------|
| `ChemicalElement` | Atomic number, symbol, name, optional atomic mass. |
| `PeriodicTable.All` | H … Og (118 elements). |
| `ChemicalFormulaPart` | Element + stoichiometric count. |
| `ChemicalCompound` | Display formula + parsed parts. |
| `Isotope` | Symbol, mass number, atomic mass, optional natural abundance %. |
| `Isotopes.Common` | Curated isotope list (`ChemistryReferenceData`). |
| `ElementAtomicMasses.BySymbol` | Relative atomic weights for molar mass estimates. |
| `ChemicalReactionComponent` | Formula string + moles. |
| `ChemicalReaction` | Reactants + products lists. |
| `BalancedReactionComponent` | Formula + integer coefficient. |
| `BalancedReactionResult` | Balanced reaction sides. |
| `StoichiometryResult` | Product moles + mass (grams). |

---

## Engineering (`Lyo.Scientific.Engineering`)

Strongly typed inputs for thermodynamics, convection, radiation, pipe flow, compressible flow, beams, fracture, fatigue — all using `Lyo.Mathematics.Quantities`.

**Representative types:**

- **Materials / catalogs:** `MaterialProperty` (density, specific heat, conductivity, optional viscosity, expansion, Young’s modulus, yield, fracture toughness); `EngineeringMaterials.Common` (Air, Water, Steel, Aluminum, Copper).
- **Thermo / heat / flow:** `ThermodynamicState`, `HeatTransferInput`, `ConductionInput`, `ThermalExpansionInput`, `ConvectiveHeatTransferInput`, `RadiativeHeatTransferInput`, `HeatExchangerInput`, `HeatExchangerResult`, `NaturalConvectionInput`, `ConvectionCorrelationInput`, `RadiationExchangeInput`, `FluidFlowState`, `ReynoldsNumberInput`, `DragForceInput`, `BuoyancyInput`, `PipeFlowInput`, `NozzleFlowInput`, `NozzleFlowResult`, `CompressibleFlowInput`, `ObliqueShockInput`, `ObliqueShockResult`, `NormalShockInput`, …
- **Mechanics / solids:** `RotationalInertiaInput`, `RotationalEnergyInput`, `AngularMomentumInput`, `SpringOscillatorInput`, `PendulumInput`, `StressStrainInput`, `BeamBendingInput`, `FractureInput`, `RectangularSectionInput`, `CircularSectionInput`, `FatigueInput`, `SNCurveInput`, `BeamSectionProfile`, `BeamSectionCatalog.Common`.

See source files for the full set of records and validation rules.

---

## Discovery and workflow helpers

| API | Description |
|-----|-------------|
| `ScientificFormulaRegistry.All` | Curated `FormulaDescriptor` entries pointing at `Lyo.Scientific.Functions` (molar mass, reaction balance, orbital period, oblique shock, fatigue life, …). |
| `ScientificWorkflowExtensions` | `MolarMassEstimate(this ChemicalCompound)` (element weights × counts), `InAstronomicalUnits(this PlanetaryBody)`, `GetMaterial(this string name)` (case-insensitive match in `EngineeringMaterials.Common`). |

---

## Related projects

- [`Lyo.Scientific.Functions`](../Lyo.Scientific.Functions/README.md)
- [`Lyo.Mathematics`](../Mathematics/Lyo.Mathematics/README.md)

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)
