# Lyo.Scientific

Scientific domain models, reference datasets, SI-oriented unit helpers, and formula discovery on top of [ `Lyo.Mathematics`](../../Mathematics/Lyo.Mathematics/README.md). Numerical formulas that operate on these types are in [ `Lyo.Scientific.Functions`](../Lyo.Scientific.Functions/README.md) (F#).

Target frameworks: `netstandard2.0`, `net10.0`

## `ScientificConstants`

Named CODATA-style double constants (SI): standard gravity, gravitational constant, speed of light, vacuum permittivity, universal gas constant, Planck constant, Boltzmann, Avogadro, elementary charge, plus `Tau` and `Pi`. Use for physics formulas (see `PhysicsFunctions` in `Lyo.Mathematics.Functions`).

## Units (`Lyo.Scientific.Units`)

| Type | Role |
| -------------------------------------------------------- | --------------------------------------------------------------------------- |
| `QuantityDimension` | Exponents for L, M, T, I, Θ, N, J (luminous intensity); `Dimensionless`. |
| `DerivedUnitDefinition` | Symbol, name, `QuantityDimension`, `ToSiFactor` (multiply user value → SI). |
| `DerivedUnits.BySymbol` | Built-in map (e.g. `J`, `N`, `Pa`, `W`, `Hz`, `V`, `C`, `ohm`). |
| `DimensionedValue` | `Dimension` + `ValueSi` (validated finite). |
| `UnitConversion.Convert(value, from, to)` | Conversion when dimensions match. |
| `UnitConversion.EnsureCompatible` | Throws when dimensions differ. |
| `UnitConversion.Add` | Adds SI values after the dimension check. |
| `UnitConversion.ApplyPrefix` | Scales by a metric prefix multiplier. |
| `ScientificUnitPrefix` / `ScientificUnitPrefixes.Metric` | G, T, k, M, da, h, c, d, μ, m, p, n prefixes. |

## Astronomy (`Lyo.Scientific.Astronomy`)

| Type / static | Description |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `PlanetaryBodyKind` | `Planet`, `DwarfPlanet`. |
| `PlanetaryBody` | Mass, name, radii, sidereal orbit/rotation, semi-major axis, mean surface temperature, rings, moon count. |
| `PlanetaryBodies.All` | Mercury … Pluto catalog. |
| `Star` | Mass, name, luminosity, radius, surface temperature. |
| `StellarBodies.All` | Sun entry. |
| `Moon` | Parent planet, name, radius, mass, orbital period, semi-major axis. |
| `NaturalSatellites.All` | Moon (Earth). |
| `AstronomyReferenceValues` | `AstronomicalUnit`, `LightYear`, `Parsec`, `SolarMass`, `SolarRadius`. |
| `OrbitalElements` | Classical elements + `EpochUtc` (eccentricity in [0,1)). |
| `Exoplanet` | Host `Star`, name, `OrbitalElements`, optional radius/mass, habitability flag. |
| `Exoplanets.All` | Sample catalog (e.g. Proxima Centauri b). |
| `Asteroid`, `Comet` | Small-body records with physical parameters, `OrbitalElements`. |
| `SmallBodies.Asteroids` / `SmallBodies.Comets` | Example entries (1P/Halley, Ceres). |

## Chemistry (`Lyo.Scientific.Chemistry`)

| Type / static | Description |
| ------------------------------ | --------------------------------------------------------------- |
| `ChemicalElement` | Symbol, atomic number, name, optional atomic mass. |
| `PeriodicTable.All` | H … Og (118 elements). |
| `ChemicalFormulaPart` | Stoichiometric count + element. |
| `ChemicalCompound` | Parsed parts + display formula. |
| `Isotope` | Mass number, symbol, atomic mass, optional natural abundance %. |
| `Isotopes.Common` | Curated isotope list (`ChemistryReferenceData`). |
| `ElementAtomicMasses.BySymbol` | Relative atomic weights for molar mass estimates. |
| `ChemicalReactionComponent` | Moles + formula string. |
| `ChemicalReaction` | Products + reactants lists. |
| `BalancedReactionComponent` | Integer coefficient + formula. |
| `BalancedReactionResult` | Balanced reaction sides. |
| `StoichiometryResult` | Mass (grams) + product moles. |

## Engineering (`Lyo.Scientific.Engineering`)

- **Materials / catalogs.** `MaterialProperty` (specific heat, density, conductivity, optional viscosity, expansion, Young's modulus, yield, fracture toughness). `EngineeringMaterials.Common` (Water, Air, Aluminum, Steel, Copper).
- **Thermo / heat / flow.** `HeatTransferInput`, `ThermodynamicState`, `ThermalExpansionInput`, `ConductionInput`, `RadiativeHeatTransferInput`, `ConvectiveHeatTransferInput`, `HeatExchangerResult`, `HeatExchangerInput`, `ConvectionCorrelationInput`, `NaturalConvectionInput`, `RadiationExchangeInput`, `ReynoldsNumberInput`, `FluidFlowState`, `BuoyancyInput`, `DragForceInput`, `NozzleFlowResult`, `PipeFlowInput`, `NozzleFlowInput`, `ObliqueShockInput`, `CompressibleFlowInput`, `ObliqueShockResult`, `NormalShockInput`, and similar.
- **Mechanics / solids.** `RotationalEnergyInput`, `RotationalInertiaInput`, `SpringOscillatorInput`, `AngularMomentumInput`, `StressStrainInput`, `PendulumInput`, `FractureInput`, `BeamBendingInput`, `CircularSectionInput`, `RectangularSectionInput`, `SNCurveInput`, `FatigueInput`, `BeamSectionCatalog.Common`, `BeamSectionProfile`.

## Workflow and discovery helpers

| API | Description |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ScientificFormulaRegistry.All` | Curated `FormulaDescriptor` entries pointing at `Lyo.Scientific.Functions` (reaction balance, molar mass, orbital period, fatigue life, oblique shock, …). |
| `ScientificWorkflowExtensions` | `InAstronomicalUnits(this PlanetaryBody)`, `MolarMassEstimate(this ChemicalCompound)` (element weights × counts), `GetMaterial(this string name)` (case-insensitive match in `EngineeringMaterials.Common`). |

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Mathematics` (direct, lyo)