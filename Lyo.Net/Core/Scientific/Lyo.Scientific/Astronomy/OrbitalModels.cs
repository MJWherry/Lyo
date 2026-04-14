using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Astronomy;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record OrbitalElements(
    Length SemiMajorAxis,
    double Eccentricity,
    Angle Inclination,
    Angle LongitudeOfAscendingNode,
    Angle ArgumentOfPeriapsis,
    Angle MeanAnomalyAtEpoch,
    DateTime EpochUtc)
{
    public double Eccentricity { get; init; } = Eccentricity is < 0d or >= 1d ? throw new ArgumentOutOfRangeException(nameof(Eccentricity)) : Eccentricity;

    public override string ToString()
        => $"a={SemiMajorAxis}, e={Eccentricity}, i={Inclination}, Ω={LongitudeOfAscendingNode}, ω={ArgumentOfPeriapsis}, M0={MeanAnomalyAtEpoch}, epoch={EpochUtc:u}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Exoplanet(string Name, Star HostStar, OrbitalElements OrbitalElements, Mass? Mass = null, Length? Radius = null, bool PotentiallyHabitable = false)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString()
        => $"{Name}, host={HostStar.Name}, habitable={PotentiallyHabitable}, M={ScientificModelDisplay.NullProp(Mass, static m => m.ToString())}, R={ScientificModelDisplay.NullProp(Radius, static r => r.ToString())}, orbit={OrbitalElements}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Asteroid(string Name, OrbitalElements OrbitalElements, Length MeanRadius, double AbsoluteMagnitude)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString() => $"{Name}, R={MeanRadius}, H={AbsoluteMagnitude}, orbit={OrbitalElements}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Comet(string Name, OrbitalElements OrbitalElements, TimeInterval OrbitalPeriod, double AbsoluteMagnitude)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public override string ToString() => $"{Name}, P={OrbitalPeriod}, H={AbsoluteMagnitude}, orbit={OrbitalElements}";
}

public static class Exoplanets
{
    public static IReadOnlyList<Exoplanet> All { get; } = [
        new(
            "Proxima Centauri b", new("Proxima Centauri", Mass.FromKilograms(2.428e29), Length.FromMeters(1.07e8), Power.FromWatts(6.5e23), Temperature.FromKelvin(3042)),
            new(Length.FromMeters(7.48e9), 0.0, Angle.FromDegrees(0), Angle.FromDegrees(0), Angle.FromDegrees(0), Angle.FromDegrees(0), new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Mass.FromKilograms(7.6e24), null, true)
    ];
}

public static class SmallBodies
{
    public static IReadOnlyList<Asteroid> Asteroids { get; } = [
        new(
            "Ceres",
            new(
                Length.FromMeters(4.14e11), 0.0758, Angle.FromDegrees(10.6), Angle.FromDegrees(80.3), Angle.FromDegrees(73.6), Angle.FromDegrees(95.9),
                new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)), Length.FromMeters(4.73e5), 3.34)
    ];

    public static IReadOnlyList<Comet> Comets { get; } = [
        new(
            "1P/Halley",
            new(
                Length.FromMeters(2.66795e12), 0.96714, Angle.FromDegrees(162.26), Angle.FromDegrees(58.42), Angle.FromDegrees(111.33), Angle.FromDegrees(38.38),
                new(1986, 2, 9, 0, 0, 0, DateTimeKind.Utc)), TimeInterval.FromSeconds(2.37e9), 5.5)
    ];
}