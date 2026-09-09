using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Astronomy;

/// <summary>Reference stars, moons, and astronomical distance constants consumed by astronomy helpers.</summary>
/// <remarks>Values are approximate popularizations; verify sources before mission-critical astrometry.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Star
{
    /// <summary>Common English name, for example <c>Sun</c>.</summary>
    public string Name { get; init; }

    /// <summary>Estimated stellar mass.</summary>
    public Mass Mass { get; init; }

    /// <summary>Photospheric or otherwise representative stellar radius.</summary>
    public Length Radius { get; init; }

    /// <summary>Bolometric luminosity of the star.</summary>
    public Power Luminosity { get; init; }

    /// <summary>Effective temperature of the stellar surface.</summary>
    public Temperature SurfaceTemperature { get; init; }

    /// <summary>Mints a star row after checking <paramref name="name" />.</summary>
    public Star(string name, Mass mass, Length radius, Power luminosity, Temperature surfaceTemperature)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        Name = name;
        Mass = mass;
        Radius = radius;
        Luminosity = luminosity;
        SurfaceTemperature = surfaceTemperature;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}, M={Mass}, R={Radius}, L={Luminosity}, T={SurfaceTemperature}";
}

/// <summary>Natural-satellite row tied to a <see cref="PlanetaryBody" /> parent.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Moon
{
    /// <summary>Satellite name, for example <c>Moon</c>.</summary>
    public string Name { get; init; }

    /// <summary>Planet that this moon orbits.</summary>
    public PlanetaryBody ParentBody { get; init; }

    /// <summary>Estimated satellite mass.</summary>
    public Mass Mass { get; init; }

    /// <summary>Mean physical radius of the satellite.</summary>
    public Length MeanRadius { get; init; }

    /// <summary>Semi-major axis of the orbit about <see cref="ParentBody" />.</summary>
    public Length SemiMajorAxis { get; init; }

    /// <summary>Sidereal period of the orbit about the parent.</summary>
    public TimeInterval OrbitalPeriod { get; init; }

    /// <summary>Mints a moon row after checking <paramref name="name" />.</summary>
    public Moon(string name, PlanetaryBody parentBody, Mass mass, Length meanRadius, Length semiMajorAxis, TimeInterval orbitalPeriod)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        Name = name;
        ParentBody = parentBody;
        Mass = mass;
        MeanRadius = meanRadius;
        SemiMajorAxis = semiMajorAxis;
        OrbitalPeriod = orbitalPeriod;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}, parent={ParentBody.Name}, M={Mass}, R={MeanRadius}, a={SemiMajorAxis}, T={OrbitalPeriod}";
}

/// <summary>Named astronomical distances and solar reference constants.</summary>
public static class AstronomyReferenceValues
{
    /// <summary>IAU astronomical unit, exact meters per definition.</summary>
    public static readonly Length AstronomicalUnit = Length.FromMeters(149_597_870_700d);

    /// <summary>Light-year in meters, using the IAU Julian-year convention.</summary>
    public static readonly Length LightYear = Length.FromMeters(9.4607304725808e15d);

    /// <summary>Parsec expressed as meters.</summary>
    public static readonly Length Parsec = Length.FromMeters(3.08567758149137e16d);

    /// <summary>Reference solar mass in kilograms.</summary>
    public static readonly Mass SolarMass = Mass.FromKilograms(1.98847e30d);

    /// <summary>Reference solar photospheric radius.</summary>
    public static readonly Length SolarRadius = Length.FromMeters(695_700_000d);
}

/// <summary>Curated list of <see cref="Star" /> catalog rows.</summary>
public static class StellarBodies
{
    /// <summary>Currently the Sun row used by sample exoplanet data.</summary>
    public static IReadOnlyList<Star> All { get; } = [
        new("Sun", AstronomyReferenceValues.SolarMass, AstronomyReferenceValues.SolarRadius, Power.FromWatts(3.828e26d), Temperature.FromKelvin(5772d))
    ];
}

/// <summary>Hand-picked list of notable natural satellites.</summary>
public static class NaturalSatellites
{
    /// <summary>Earth’s Moon row, referencing <see cref="PlanetaryBodies" />.</summary>
    public static IReadOnlyList<Moon> All { get; } = [
        new("Moon", PlanetaryBodies.All[2], Mass.FromKilograms(7.342e22d), Length.FromMeters(1_737_400d), Length.FromMeters(384_399_000d), TimeInterval.FromSeconds(2_360_591.5d))
    ];
}