using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Scientific.Chemistry;

/// <summary>Represents a chemical element from the periodic table with optional standard atomic weight.</summary>
/// <remarks>Use <see cref="PeriodicTable.All" /> for the curated list of elements through oganesson (Z = 118).</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalElement
{
    /// <summary>Atomic number Z (count of protons); must be positive.</summary>
    public int AtomicNumber { get; init; }

    /// <summary>IUPAC element symbol (for example <c>Fe</c>, <c>U</c>).</summary>
    public string Symbol { get; init; }

    /// <summary>English element name (for example <c>Iron</c>).</summary>
    public string Name { get; init; }

    /// <summary>Optional relative atomic mass (dimensionless ratio on the carbon-12 scale) when known for this catalog entry.</summary>
    /// <remarks>When present, must be strictly positive; <see langword="null" /> means “no numeric mass supplied” (typical for synthetic/heavy elements in stub rows).</remarks>
    public double? AtomicMass { get; init; }

    /// <summary>Creates an element record after validating identifiers and optional mass.</summary>
    /// <param name="atomicNumber">Strictly positive atomic number.</param>
    /// <param name="symbol">Non-empty element symbol.</param>
    /// <param name="name">Non-empty element name.</param>
    /// <param name="atomicMass">Optional mass; when not <see langword="null" />, must be strictly greater than zero.</param>
    /// <exception cref="ArgumentOutsideRangeException"><paramref name="atomicNumber" /> is not positive, or <paramref name="atomicMass" /> is present and not strictly positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="symbol" /> or <paramref name="name" /> is null or whitespace.</exception>
    public ChemicalElement(int atomicNumber, string symbol, string name, double? atomicMass = null)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(atomicNumber, 0);
        ArgumentHelpers.ThrowIfNullOrEmpty(symbol);
        ArgumentHelpers.ThrowIfNullOrEmpty(name);
        ArgumentHelpers.ThrowIfNotNullAndLessThanOrEqual(atomicMass, 0d);
        AtomicNumber = atomicNumber;
        Symbol = symbol;
        Name = name;
        AtomicMass = atomicMass;
    }

    /// <inheritdoc />
    public override string ToString() => AtomicMass is { } m ? $"{AtomicNumber} {Symbol} ({Name}), mass≈{m}" : $"{AtomicNumber} {Symbol} ({Name})";
}

/// <summary>Static catalog of <see cref="ChemicalElement" /> rows covering Z = 1 through 118.</summary>
/// <remarks>Atomic masses are omitted for many entries; use <see cref="ElementAtomicMasses" /> for numeric weights in stoichiometry.</remarks>
public static class PeriodicTable
{
    /// <summary>Read-only list of elements in ascending <see cref="ChemicalElement.AtomicNumber" /> order.</summary>
    public static IReadOnlyList<ChemicalElement> All { get; } = [
        new(1, "H", "Hydrogen"), new(2, "He", "Helium"), new(3, "Li", "Lithium"), new(4, "Be", "Beryllium"), new(5, "B", "Boron"), new(6, "C", "Carbon"), new(7, "N", "Nitrogen"),
        new(8, "O", "Oxygen"), new(9, "F", "Fluorine"), new(10, "Ne", "Neon"), new(11, "Na", "Sodium"), new(12, "Mg", "Magnesium"), new(13, "Al", "Aluminium"),
        new(14, "Si", "Silicon"), new(15, "P", "Phosphorus"), new(16, "S", "Sulfur"), new(17, "Cl", "Chlorine"), new(18, "Ar", "Argon"), new(19, "K", "Potassium"),
        new(20, "Ca", "Calcium"), new(21, "Sc", "Scandium"), new(22, "Ti", "Titanium"), new(23, "V", "Vanadium"), new(24, "Cr", "Chromium"), new(25, "Mn", "Manganese"),
        new(26, "Fe", "Iron"), new(27, "Co", "Cobalt"), new(28, "Ni", "Nickel"), new(29, "Cu", "Copper"), new(30, "Zn", "Zinc"), new(31, "Ga", "Gallium"),
        new(32, "Ge", "Germanium"), new(33, "As", "Arsenic"), new(34, "Se", "Selenium"), new(35, "Br", "Bromine"), new(36, "Kr", "Krypton"), new(37, "Rb", "Rubidium"),
        new(38, "Sr", "Strontium"), new(39, "Y", "Yttrium"), new(40, "Zr", "Zirconium"), new(41, "Nb", "Niobium"), new(42, "Mo", "Molybdenum"), new(43, "Tc", "Technetium"),
        new(44, "Ru", "Ruthenium"), new(45, "Rh", "Rhodium"), new(46, "Pd", "Palladium"), new(47, "Ag", "Silver"), new(48, "Cd", "Cadmium"), new(49, "In", "Indium"),
        new(50, "Sn", "Tin"), new(51, "Sb", "Antimony"), new(52, "Te", "Tellurium"), new(53, "I", "Iodine"), new(54, "Xe", "Xenon"), new(55, "Cs", "Caesium"),
        new(56, "Ba", "Barium"), new(57, "La", "Lanthanum"), new(58, "Ce", "Cerium"), new(59, "Pr", "Praseodymium"), new(60, "Nd", "Neodymium"), new(61, "Pm", "Promethium"),
        new(62, "Sm", "Samarium"), new(63, "Eu", "Europium"), new(64, "Gd", "Gadolinium"), new(65, "Tb", "Terbium"), new(66, "Dy", "Dysprosium"), new(67, "Ho", "Holmium"),
        new(68, "Er", "Erbium"), new(69, "Tm", "Thulium"), new(70, "Yb", "Ytterbium"), new(71, "Lu", "Lutetium"), new(72, "Hf", "Hafnium"), new(73, "Ta", "Tantalum"),
        new(74, "W", "Tungsten"), new(75, "Re", "Rhenium"), new(76, "Os", "Osmium"), new(77, "Ir", "Iridium"), new(78, "Pt", "Platinum"), new(79, "Au", "Gold"),
        new(80, "Hg", "Mercury"), new(81, "Tl", "Thallium"), new(82, "Pb", "Lead"), new(83, "Bi", "Bismuth"), new(84, "Po", "Polonium"), new(85, "At", "Astatine"),
        new(86, "Rn", "Radon"), new(87, "Fr", "Francium"), new(88, "Ra", "Radium"), new(89, "Ac", "Actinium"), new(90, "Th", "Thorium"), new(91, "Pa", "Protactinium"),
        new(92, "U", "Uranium"), new(93, "Np", "Neptunium"), new(94, "Pu", "Plutonium"), new(95, "Am", "Americium"), new(96, "Cm", "Curium"), new(97, "Bk", "Berkelium"),
        new(98, "Cf", "Californium"), new(99, "Es", "Einsteinium"), new(100, "Fm", "Fermium"), new(101, "Md", "Mendelevium"), new(102, "No", "Nobelium"),
        new(103, "Lr", "Lawrencium"), new(104, "Rf", "Rutherfordium"), new(105, "Db", "Dubnium"), new(106, "Sg", "Seaborgium"), new(107, "Bh", "Bohrium"),
        new(108, "Hs", "Hassium"), new(109, "Mt", "Meitnerium"), new(110, "Ds", "Darmstadtium"), new(111, "Rg", "Roentgenium"), new(112, "Cn", "Copernicium"),
        new(113, "Nh", "Nihonium"), new(114, "Fl", "Flerovium"), new(115, "Mc", "Moscovium"), new(116, "Lv", "Livermorium"), new(117, "Ts", "Tennessine"),
        new(118, "Og", "Oganesson")
    ];
}
