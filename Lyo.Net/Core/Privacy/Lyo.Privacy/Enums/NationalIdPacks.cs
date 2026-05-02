using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Enums;

/// <summary>Bit masks for <see cref="NationalIdRedactionRule" />.</summary>
[Flags]
public enum NationalIdPacks
{
    None = 0,

    /// <summary>US Social Security number pattern.</summary>
    UnitedStatesSsn = 1 << 0,

    /// <summary>UK National Insurance number (loose pattern).</summary>
    UnitedKingdomNino = 1 << 1,

    /// <summary>Germany Steueridentifikationsnummer (11 digits, non-zero start).</summary>
    GermanySteuerId = 1 << 2
}