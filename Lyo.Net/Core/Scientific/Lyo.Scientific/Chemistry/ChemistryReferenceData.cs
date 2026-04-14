using System.Diagnostics;

namespace Lyo.Scientific.Chemistry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Isotope(string Symbol, int MassNumber, double AtomicMass, double? NaturalAbundancePercent = null)
{
    public string Symbol { get; init; } = string.IsNullOrWhiteSpace(Symbol) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Symbol)) : Symbol;

    public int MassNumber { get; init; } = MassNumber <= 0 ? throw new ArgumentOutOfRangeException(nameof(MassNumber)) : MassNumber;

    public double AtomicMass { get; init; } = AtomicMass <= 0d ? throw new ArgumentOutOfRangeException(nameof(AtomicMass)) : AtomicMass;

    public override string ToString() => NaturalAbundancePercent is { } p ? $"{Symbol}-{MassNumber}, mass={AtomicMass}, x={p:0.###}%" : $"{Symbol}-{MassNumber}, mass={AtomicMass}";
}

public static class ElementAtomicMasses
{
    public static IReadOnlyDictionary<string, double> BySymbol { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) {
        ["H"] = 1.008,
        ["He"] = 4.002602,
        ["Li"] = 6.94,
        ["Be"] = 9.0121831,
        ["B"] = 10.81,
        ["C"] = 12.011,
        ["N"] = 14.007,
        ["O"] = 15.999,
        ["F"] = 18.998403163,
        ["Ne"] = 20.1797,
        ["Na"] = 22.98976928,
        ["Mg"] = 24.305,
        ["Al"] = 26.9815385,
        ["Si"] = 28.085,
        ["P"] = 30.973761998,
        ["S"] = 32.06,
        ["Cl"] = 35.45,
        ["Ar"] = 39.948,
        ["K"] = 39.0983,
        ["Ca"] = 40.078,
        ["Sc"] = 44.955908,
        ["Ti"] = 47.867,
        ["V"] = 50.9415,
        ["Cr"] = 51.9961,
        ["Mn"] = 54.938044,
        ["Fe"] = 55.845,
        ["Co"] = 58.933194,
        ["Ni"] = 58.6934,
        ["Cu"] = 63.546,
        ["Zn"] = 65.38,
        ["Ga"] = 69.723,
        ["Ge"] = 72.63,
        ["As"] = 74.921595,
        ["Se"] = 78.971,
        ["Br"] = 79.904,
        ["Kr"] = 83.798,
        ["Rb"] = 85.4678,
        ["Sr"] = 87.62,
        ["Y"] = 88.90584,
        ["Zr"] = 91.224,
        ["Nb"] = 92.90637,
        ["Mo"] = 95.95,
        ["Tc"] = 98,
        ["Ru"] = 101.07,
        ["Rh"] = 102.9055,
        ["Pd"] = 106.42,
        ["Ag"] = 107.8682,
        ["Cd"] = 112.414,
        ["In"] = 114.818,
        ["Sn"] = 118.71,
        ["Sb"] = 121.76,
        ["Te"] = 127.6,
        ["I"] = 126.90447,
        ["Xe"] = 131.293,
        ["Cs"] = 132.90545196,
        ["Ba"] = 137.327,
        ["La"] = 138.90547,
        ["Ce"] = 140.116,
        ["Pr"] = 140.90766,
        ["Nd"] = 144.242,
        ["Pm"] = 145,
        ["Sm"] = 150.36,
        ["Eu"] = 151.964,
        ["Gd"] = 157.25,
        ["Tb"] = 158.92535,
        ["Dy"] = 162.5,
        ["Ho"] = 164.93033,
        ["Er"] = 167.259,
        ["Tm"] = 168.93422,
        ["Yb"] = 173.045,
        ["Lu"] = 174.9668,
        ["Hf"] = 178.49,
        ["Ta"] = 180.94788,
        ["W"] = 183.84,
        ["Re"] = 186.207,
        ["Os"] = 190.23,
        ["Ir"] = 192.217,
        ["Pt"] = 195.084,
        ["Au"] = 196.96657,
        ["Hg"] = 200.592,
        ["Tl"] = 204.38,
        ["Pb"] = 207.2,
        ["Bi"] = 208.9804,
        ["Po"] = 209,
        ["At"] = 210,
        ["Rn"] = 222,
        ["Fr"] = 223,
        ["Ra"] = 226,
        ["Ac"] = 227,
        ["Th"] = 232.0377,
        ["Pa"] = 231.03588,
        ["U"] = 238.02891,
        ["Np"] = 237,
        ["Pu"] = 244,
        ["Am"] = 243,
        ["Cm"] = 247,
        ["Bk"] = 247,
        ["Cf"] = 251,
        ["Es"] = 252,
        ["Fm"] = 257,
        ["Md"] = 258,
        ["No"] = 259,
        ["Lr"] = 266,
        ["Rf"] = 267,
        ["Db"] = 268,
        ["Sg"] = 269,
        ["Bh"] = 270,
        ["Hs"] = 277,
        ["Mt"] = 278,
        ["Ds"] = 281,
        ["Rg"] = 282,
        ["Cn"] = 285,
        ["Nh"] = 286,
        ["Fl"] = 289,
        ["Mc"] = 290,
        ["Lv"] = 293,
        ["Ts"] = 294,
        ["Og"] = 294
    };
}

public static class Isotopes
{
    public static IReadOnlyList<Isotope> Common { get; } = [
        new("H", 1, 1.00782503223, 99.9885), new("H", 2, 2.01410177812, 0.0115), new("C", 12, 12.0, 98.93), new("C", 13, 13.00335483507, 1.07),
        new("O", 16, 15.99491461957, 99.757), new("U", 235, 235.0439299, 0.72), new("U", 238, 238.05078826, 99.27)
    ];
}