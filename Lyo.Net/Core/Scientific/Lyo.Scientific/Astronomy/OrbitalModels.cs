using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Astronomy;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record OrbitalElements
{

    public OrbitalElements(
    Length semiMajorAxis,
    double eccentricity,
    Angle inclination,
    Angle longitudeOfAscendingNode,
    Angle argumentOfPeriapsis,
    Angle meanAnomalyAtEpoch,
    DateTime epochUtc)

    {

        eccentricity = eccentricity is < 0d or >= 1d ? throw new ArgumentOutOfRangeException(nameof(eccentricity)) : eccentricity;

        Eccentricity = eccentricity;
        SemiMajorAxis = semiMajorAxis;
        Inclination = inclination;
        LongitudeOfAscendingNode = longitudeOfAscendingNode;
        ArgumentOfPeriapsis = argumentOfPeriapsis;
        MeanAnomalyAtEpoch = meanAnomalyAtEpoch;
        EpochUtc = epochUtc;
}


    public double Eccentricity { get;  init; }

    public Length SemiMajorAxis { get; init; }
    public Angle Inclination { get; init; }
    public Angle LongitudeOfAscendingNode { get; init; }
    public Angle ArgumentOfPeriapsis { get; init; }
    public Angle MeanAnomalyAtEpoch { get; init; }
    public DateTime EpochUtc { get; init; }
    public override string ToString()
        => $"a={SemiMajorAxis}, e={Eccentricity}, i={Inclination}, Ω={LongitudeOfAscendingNode}, ω={ArgumentOfPeriapsis}, M0={MeanAnomalyAtEpoch}, epoch={EpochUtc:u}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Exoplanet
{

    public Exoplanet(string name, Star hostStar, OrbitalElements orbitalElements, Mass? mass = null, Length? radius = null, bool potentiallyHabitable = false)

    {

        name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)) : name;

        Name = name;
        HostStar = hostStar;
        OrbitalElements = orbitalElements;
        Mass = mass;
        Radius = radius;
        PotentiallyHabitable = potentiallyHabitable;
}


    public string Name { get;  init; }

    public Star HostStar { get; init; }
    public OrbitalElements OrbitalElements { get; init; }
    public Mass? Mass { get; init; }
    public Length? Radius { get; init; }
    public bool PotentiallyHabitable { get; init; }
    public override string ToString()
        => $"{Name}, host={HostStar.Name}, habitable={PotentiallyHabitable}, M={ScientificModelDisplay.NullProp(Mass, static m => m.ToString())}, R={ScientificModelDisplay.NullProp(Radius, static r => r.ToString())}, orbit={OrbitalElements}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Asteroid
{

    public Asteroid(string name, OrbitalElements orbitalElements, Length meanRadius, double absoluteMagnitude)

    {

        name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)) : name;

        Name = name;
        OrbitalElements = orbitalElements;
        MeanRadius = meanRadius;
        AbsoluteMagnitude = absoluteMagnitude;
}


    public string Name { get;  init; }

    public OrbitalElements OrbitalElements { get; init; }
    public Length MeanRadius { get; init; }
    public double AbsoluteMagnitude { get; init; }
    public override string ToString() => $"{Name}, R={MeanRadius}, H={AbsoluteMagnitude}, orbit={OrbitalElements}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Comet
{

    public Comet(string name, OrbitalElements orbitalElements, TimeInterval orbitalPeriod, double absoluteMagnitude)

    {

        name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name)) : name;

        Name = name;
        OrbitalElements = orbitalElements;
        OrbitalPeriod = orbitalPeriod;
        AbsoluteMagnitude = absoluteMagnitude;
}


    public string Name { get;  init; }

    public OrbitalElements OrbitalElements { get; init; }
    public TimeInterval OrbitalPeriod { get; init; }
    public double AbsoluteMagnitude { get; init; }
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