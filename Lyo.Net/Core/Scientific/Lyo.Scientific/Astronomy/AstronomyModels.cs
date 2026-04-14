using System.Diagnostics;
using Lyo.Mathematics.Quantities;

namespace Lyo.Scientific.Astronomy;

public enum PlanetaryBodyKind
{
    Planet,
    DwarfPlanet
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlanetaryBody(
    string Name,
    PlanetaryBodyKind Kind,
    Mass Mass,
    Length MeanRadius,
    Length SemiMajorAxis,
    TimeInterval SiderealOrbit,
    TimeInterval SiderealRotation,
    Temperature MeanSurfaceTemperature,
    int NaturalSatelliteCount,
    bool HasRingSystem)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public int NaturalSatelliteCount { get; init; } = NaturalSatelliteCount < 0 ? throw new ArgumentOutOfRangeException(nameof(NaturalSatelliteCount)) : NaturalSatelliteCount;

    public override string ToString() => $"{Name} ({Kind}), moons={NaturalSatelliteCount}, rings={HasRingSystem}, M={Mass}, R={MeanRadius}";
}

public static class PlanetaryBodies
{
    public static IReadOnlyList<PlanetaryBody> All { get; } = [
        new(
            "Mercury", PlanetaryBodyKind.Planet, Mass.FromKilograms(3.3011e23), Length.FromMeters(2_439_700d), Length.FromMeters(57_909_227_000d),
            TimeInterval.FromSeconds(7_600_544d), TimeInterval.FromSeconds(5_067_030d), Temperature.FromKelvin(440d), 0, false),
        new(
            "Venus", PlanetaryBodyKind.Planet, Mass.FromKilograms(4.8675e24), Length.FromMeters(6_051_800d), Length.FromMeters(108_209_475_000d),
            TimeInterval.FromSeconds(19_414_149d), TimeInterval.FromSeconds(20_996_640d), Temperature.FromKelvin(737d), 0, false),
        new(
            "Earth", PlanetaryBodyKind.Planet, Mass.FromKilograms(5.97237e24), Length.FromMeters(6_371_000d), Length.FromMeters(149_598_023_000d),
            TimeInterval.FromSeconds(31_558_149.8d), TimeInterval.FromSeconds(86_164.1d), Temperature.FromKelvin(288d), 1, false),
        new(
            "Mars", PlanetaryBodyKind.Planet, Mass.FromKilograms(6.4171e23), Length.FromMeters(3_389_500d), Length.FromMeters(227_943_824_000d),
            TimeInterval.FromSeconds(59_354_032d), TimeInterval.FromSeconds(88_642.7d), Temperature.FromKelvin(210d), 2, false),
        new(
            "Jupiter", PlanetaryBodyKind.Planet, Mass.FromKilograms(1.8982e27), Length.FromMeters(69_911_000d), Length.FromMeters(778_340_821_000d),
            TimeInterval.FromSeconds(374_335_776d), TimeInterval.FromSeconds(35_729.8d), Temperature.FromKelvin(165d), 95, true),
        new(
            "Saturn", PlanetaryBodyKind.Planet, Mass.FromKilograms(5.6834e26), Length.FromMeters(58_232_000d), Length.FromMeters(1_426_666_422_000d),
            TimeInterval.FromSeconds(929_596_608d), TimeInterval.FromSeconds(38_362.4d), Temperature.FromKelvin(134d), 146, true),
        new(
            "Uranus", PlanetaryBodyKind.Planet, Mass.FromKilograms(8.6810e25), Length.FromMeters(25_362_000d), Length.FromMeters(2_870_658_186_000d),
            TimeInterval.FromSeconds(2_651_370_019d), TimeInterval.FromSeconds(62_063.7d), Temperature.FromKelvin(76d), 28, true),
        new(
            "Neptune", PlanetaryBodyKind.Planet, Mass.FromKilograms(1.02413e26), Length.FromMeters(24_622_000d), Length.FromMeters(4_498_396_441_000d),
            TimeInterval.FromSeconds(5_200_418_560d), TimeInterval.FromSeconds(57_996d), Temperature.FromKelvin(72d), 16, true),
        new(
            "Pluto", PlanetaryBodyKind.DwarfPlanet, Mass.FromKilograms(1.303e22), Length.FromMeters(1_188_300d), Length.FromMeters(5_906_376_272_000d),
            TimeInterval.FromSeconds(7_820_438_400d), TimeInterval.FromSeconds(551_856d), Temperature.FromKelvin(44d), 5, false)
    ];
}