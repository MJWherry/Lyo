using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Enums;

/// <summary>Bit masks consumed by <see cref="NationalIdRedactionRule" />.</summary>
[Flags]
public enum NationalIdPacks
{
    None = 0,

    /// <summary>United States Social Security number pattern.</summary>
    UnitedStatesSsn = 1 << 0,

    /// <summary>United Kingdom National Insurance number (loose pattern).</summary>
    UnitedKingdomNino = 1 << 1,

    /// <summary>German Steueridentifikationsnummer (11 digits, non-zero start).</summary>
    GermanySteuerId = 1 << 2
}