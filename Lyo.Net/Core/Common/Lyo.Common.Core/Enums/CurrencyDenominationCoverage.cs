using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Whether circulating bill and coin denominations are maintained for a currency.</summary>
public enum CurrencyDenominationCoverage
{
    /// <summary>Denominations are not cataloged (unknown, volatile, or not maintained).</summary>
    [Description("Not cataloged")]
    NotCataloged = 0,

    /// <summary>Standard circulating bill and coin denominations are cataloged for this currency.</summary>
    [Description("Cataloged")]
    Cataloged = 1
}
