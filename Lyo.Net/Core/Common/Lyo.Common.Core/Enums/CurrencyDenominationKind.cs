using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>Physical form of a currency denomination.</summary>
public enum CurrencyDenominationKind
{
    /// <summary>Banknote or paper bill.</summary>
    [Description("Bill")]
    Bill = 0,

    /// <summary>Coin.</summary>
    [Description("Coin")]
    Coin = 1
}
