using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Astronomy;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Star(string Name, Mass Mass, Length Radius, Power Luminosity, Temperature SurfaceTemperature)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString() => $"{Name}, M={Mass}, R={Radius}, L={Luminosity}, T={SurfaceTemperature}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Moon(string Name, PlanetaryBody ParentBody, Mass Mass, Length MeanRadius, Length SemiMajorAxis, TimeInterval OrbitalPeriod)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString() => $"{Name}, parent={ParentBody.Name}, M={Mass}, R={MeanRadius}, a={SemiMajorAxis}, T={OrbitalPeriod}";
}

public static class AstronomyReferenceValues
{
    public static readonly Length AstronomicalUnit = Length.FromMeters(149_597_870_700d);
    public static readonly Length LightYear = Length.FromMeters(9.4607304725808e15d);
    public static readonly Length Parsec = Length.FromMeters(3.08567758149137e16d);
    public static readonly Mass SolarMass = Mass.FromKilograms(1.98847e30d);
    public static readonly Length SolarRadius = Length.FromMeters(695_700_000d);
}

public static class StellarBodies
{
    public static IReadOnlyList<Star> All { get; } = [
        new("Sun", AstronomyReferenceValues.SolarMass, AstronomyReferenceValues.SolarRadius, Power.FromWatts(3.828e26d), Temperature.FromKelvin(5772d))
    ];
}

public static class NaturalSatellites
{
    public static IReadOnlyList<Moon> All { get; } = [
        new("Moon", PlanetaryBodies.All[2], Mass.FromKilograms(7.342e22d), Length.FromMeters(1_737_400d), Length.FromMeters(384_399_000d), TimeInterval.FromSeconds(2_360_591.5d))
    ];
}