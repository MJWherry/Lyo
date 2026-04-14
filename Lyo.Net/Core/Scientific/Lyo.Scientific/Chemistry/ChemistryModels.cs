using System.Diagnostics;

namespace Lyo.Scientific.Chemistry;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChemicalElement(int AtomicNumber, string Symbol, string Name, double? AtomicMass = null)
{
    public int AtomicNumber { get; init; } = AtomicNumber <= 0 ? throw new ArgumentOutOfRangeException(nameof(AtomicNumber)) : AtomicNumber;

    public string Symbol { get; init; } = string.IsNullOrWhiteSpace(Symbol) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Symbol)) : Symbol;

    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(Name)) : Name;

    public double? AtomicMass { get; init; } = AtomicMass is not null && AtomicMass <= 0d ? throw new ArgumentOutOfRangeException(nameof(AtomicMass)) : AtomicMass;

    public override string ToString() => AtomicMass is { } m ? $"{AtomicNumber} {Symbol} ({Name}), mass≈{m}" : $"{AtomicNumber} {Symbol} ({Name})";
}

public static class PeriodicTable
{
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