using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Astronomy;

/// <summary>Keplerian two-body elements together with a UTC epoch.</summary>
/// <remarks>This catalog allows only elliptical eccentricity in <c>[0, 1)</c>.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record OrbitalElements
{
    /// <summary>Orbital eccentricity: 0 is circular, values approaching 1 are highly elongated.</summary>
    public double Eccentricity { get; init; }

    /// <summary>Length of the semi-major axis.</summary>
    public Length SemiMajorAxis { get; init; }

    /// <summary>Tilt of the orbital plane relative to the reference plane.</summary>
    public Angle Inclination { get; init; }

    /// <summary>Longitude of the ascending node (Ω).</summary>
    public Angle LongitudeOfAscendingNode { get; init; }

    /// <summary>Argument of periapsis (ω).</summary>
    public Angle ArgumentOfPeriapsis { get; init; }

    /// <summary>Mean anomaly evaluated at <see cref="EpochUtc" />.</summary>
    public Angle MeanAnomalyAtEpoch { get; init; }

    /// <summary>UTC epoch that belongs with the supplied mean anomaly.</summary>
    public DateTime EpochUtc { get; init; }

    /// <summary>Mints orbital elements after checking eccentricity.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="eccentricity" /> is outside <c>[0, 1)</c>.</exception>
    public OrbitalElements(
        Length semiMajorAxis,
        double eccentricity,
        Angle inclination,
        Angle longitudeOfAscendingNode,
        Angle argumentOfPeriapsis,
        Angle meanAnomalyAtEpoch,
        DateTime epochUtc)
    {
        ArgumentHelpers.ThrowIfNotInRange(eccentricity, 0d, 1d);
        Eccentricity = eccentricity;
        SemiMajorAxis = semiMajorAxis;
        Inclination = inclination;
        LongitudeOfAscendingNode = longitudeOfAscendingNode;
        ArgumentOfPeriapsis = argumentOfPeriapsis;
        MeanAnomalyAtEpoch = meanAnomalyAtEpoch;
        EpochUtc = epochUtc;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"a={SemiMajorAxis}, e={Eccentricity}, i={Inclination}, Ω={LongitudeOfAscendingNode}, ω={ArgumentOfPeriapsis}, M0={MeanAnomalyAtEpoch}, epoch={EpochUtc:u}";
}

/// <summary>Catalog row for an exoplanet: host star plus optional bulk parameters.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Exoplanet
{
    /// <summary>Planet designation or catalog name.</summary>
    public string Name { get; init; }

    /// <summary>Host star used for illumination and mass estimates.</summary>
    public Star HostStar { get; init; }

    /// <summary>Orbital geometry about the host.</summary>
    public OrbitalElements OrbitalElements { get; init; }

    /// <summary>Optional estimate of the planet mass.</summary>
    public Mass? Mass { get; init; }

    /// <summary>Optional estimate of the planet radius.</summary>
    public Length? Radius { get; init; }

    /// <summary>Heuristic habitability flag for demos — not an astrobiology determination.</summary>
    public bool PotentiallyHabitable { get; init; }

    /// <summary>Mints an exoplanet row after checking <paramref name="name" />.</summary>
    public Exoplanet(string name, Star hostStar, OrbitalElements orbitalElements, Mass? mass = null, Length? radius = null, bool potentiallyHabitable = false)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        Name = name;
        HostStar = hostStar;
        OrbitalElements = orbitalElements;
        Mass = mass;
        Radius = radius;
        PotentiallyHabitable = potentiallyHabitable;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Name}, host={HostStar.Name}, habitable={PotentiallyHabitable}, M={ScientificModelDisplay.NullProp(Mass, static m => m.ToString())}, R={ScientificModelDisplay.NullProp(Radius, static r => r.ToString())}, orbit={OrbitalElements}";
}

/// <summary>Asteroid-style small body (main belt or other) with orbit and size hints.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Asteroid
{
    /// <summary>Name of the small body.</summary>
    public string Name { get; init; }

    /// <summary>Orbit expressed as classical elements.</summary>
    public OrbitalElements OrbitalElements { get; init; }

    /// <summary>Estimated mean physical radius.</summary>
    public Length MeanRadius { get; init; }

    /// <summary>Absolute magnitude H on the asteroid photometric scale.</summary>
    public double AbsoluteMagnitude { get; init; }

    /// <summary>Mints an asteroid row after checking <paramref name="name" />.</summary>
    public Asteroid(string name, OrbitalElements orbitalElements, Length meanRadius, double absoluteMagnitude)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        Name = name;
        OrbitalElements = orbitalElements;
        MeanRadius = meanRadius;
        AbsoluteMagnitude = absoluteMagnitude;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}, R={MeanRadius}, H={AbsoluteMagnitude}, orbit={OrbitalElements}";
}

/// <summary>Comet catalog row with orbital period and a brightness proxy.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Comet
{
    /// <summary>Comet designation (for example a periodic-comet id).</summary>
    public string Name { get; init; }

    /// <summary>Orbit expressed as classical elements.</summary>
    public OrbitalElements OrbitalElements { get; init; }

    /// <summary>Sidereal period of the orbit.</summary>
    public TimeInterval OrbitalPeriod { get; init; }

    /// <summary>Absolute magnitude H for the comet.</summary>
    public double AbsoluteMagnitude { get; init; }

    /// <summary>Mints a comet row after checking <paramref name="name" />.</summary>
    public Comet(string name, OrbitalElements orbitalElements, TimeInterval orbitalPeriod, double absoluteMagnitude)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        Name = name;
        OrbitalElements = orbitalElements;
        OrbitalPeriod = orbitalPeriod;
        AbsoluteMagnitude = absoluteMagnitude;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}, P={OrbitalPeriod}, H={AbsoluteMagnitude}, orbit={OrbitalElements}";
}

/// <summary>Sample catalog of exoplanets.</summary>
public static class Exoplanets
{
    /// <summary>Curated rows; currently Proxima Centauri b.</summary>
    public static IReadOnlyList<Exoplanet> All { get; } = [
        new(
            "Proxima Centauri b", new("Proxima Centauri", Mass.FromKilograms(2.428e29), Length.FromMeters(1.07e8), Power.FromWatts(6.5e23), Temperature.FromKelvin(3042)),
            new(Length.FromMeters(7.48e9), 0.0, Angle.FromDegrees(0), Angle.FromDegrees(0), Angle.FromDegrees(0), Angle.FromDegrees(0), new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Mass.FromKilograms(7.6e24), null, true)
    ];
}

/// <summary>Small solar-system bodies (asteroids and comets) for demos.</summary>
public static class SmallBodies
{
    /// <summary>Representative asteroid rows.</summary>
    public static IReadOnlyList<Asteroid> Asteroids { get; } = [
        new(
            "Ceres",
            new(
                Length.FromMeters(4.14e11), 0.0758, Angle.FromDegrees(10.6), Angle.FromDegrees(80.3), Angle.FromDegrees(73.6), Angle.FromDegrees(95.9),
                new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)), Length.FromMeters(4.73e5), 3.34)
    ];

    /// <summary>Representative comet rows.</summary>
    public static IReadOnlyList<Comet> Comets { get; } = [
        new(
            "1P/Halley",
            new(
                Length.FromMeters(2.66795e12), 0.96714, Angle.FromDegrees(162.26), Angle.FromDegrees(58.42), Angle.FromDegrees(111.33), Angle.FromDegrees(38.38),
                new(1986, 2, 9, 0, 0, 0, DateTimeKind.Utc)), TimeInterval.FromSeconds(2.37e9), 5.5)
    ];
}