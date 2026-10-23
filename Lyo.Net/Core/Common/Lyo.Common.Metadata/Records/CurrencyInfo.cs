using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Lyo.Common.Metadata.Records;

/// <summary>Catalog row for an ISO 4217 currency; <see cref="Code" /> is the alpha-3 key.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CurrencyInfo(
    string Code,
    int NumericCode,
    string Name,
    string Symbol,
    int MinorUnits,
    CurrencyDenominationCoverage DenominationCoverage = CurrencyDenominationCoverage.NotCataloged)
{
    private static readonly IReadOnlyDictionary<string, CurrencyDenominationInfo> EmptyDenominations =
        new Dictionary<string, CurrencyDenominationInfo>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Unrecognized or unspecified currency.</summary>
    public static readonly CurrencyInfo Unknown = new("", 0, "Unknown currency", "?", -1) { CoinSymbol = "?" };

    public static readonly CurrencyInfo Usd = Create(
        "USD", 840, "US Dollar", "$", 2, coinSymbol: "¢",
        coins: CoinMap(
            ("penny", 0.01m, "Penny"), ("nickel", 0.05m, "Nickel"), ("dime", 0.10m, "Dime"), ("quarter", 0.25m, "Quarter"), ("halfdollar", 0.50m, "Half dollar"),
            ("1", 1m, "One-dollar coin")),
        bills: BillMap(
            ("1", 1m, "One-dollar bill"), ("2", 2m, "Two-dollar bill"), ("5", 5m, "Five-dollar bill"), ("10", 10m, "Ten-dollar bill"), ("20", 20m, "Twenty-dollar bill"),
            ("50", 50m, "Fifty-dollar bill"), ("100", 100m, "One-hundred-dollar bill")));

    public static readonly CurrencyInfo Eur = Create(
        "EUR", 978, "Euro", "€", 2, coinSymbol: "c",
        coins: CoinMap(
            ("1cent", 0.01m, "One-cent coin"), ("2cent", 0.02m, "Two-cent coin"), ("5cent", 0.05m, "Five-cent coin"), ("10cent", 0.10m, "Ten-cent coin"),
            ("20cent", 0.20m, "Twenty-cent coin"), ("50cent", 0.50m, "Fifty-cent coin"), ("1", 1m, "One-euro coin"), ("2", 2m, "Two-euro coin")),
        bills: BillMap(
            ("5", 5m, "Five-euro note"), ("10", 10m, "Ten-euro note"), ("20", 20m, "Twenty-euro note"), ("50", 50m, "Fifty-euro note"), ("100", 100m, "One-hundred-euro note"),
            ("200", 200m, "Two-hundred-euro note"), ("500", 500m, "Five-hundred-euro note")));

    public static readonly CurrencyInfo Gbp = Create(
        "GBP", 826, "Pound Sterling", "£", 2, coinSymbol: "p",
        coins: CoinMap(
            ("1penny", 0.01m, "One-penny coin"), ("2pence", 0.02m, "Two-pence coin"), ("5pence", 0.05m, "Five-pence coin"), ("10pence", 0.10m, "Ten-pence coin"),
            ("20pence", 0.20m, "Twenty-pence coin"), ("50pence", 0.50m, "Fifty-pence coin"), ("1", 1m, "One-pound coin"), ("2", 2m, "Two-pound coin")),
        bills: BillMap(("5", 5m, "Five-pound note"), ("10", 10m, "Ten-pound note"), ("20", 20m, "Twenty-pound note"), ("50", 50m, "Fifty-pound note")));

    public static readonly CurrencyInfo Jpy = Create(
        "JPY", 392, "Japanese Yen", "¥", 0, coinSymbol: "¥",
        coins: CoinMap(
            ("1", 1m, "One-yen coin"), ("5", 5m, "Five-yen coin"), ("10", 10m, "Ten-yen coin"), ("50", 50m, "Fifty-yen coin"), ("100", 100m, "One-hundred-yen coin"),
            ("500", 500m, "Five-hundred-yen coin")),
        bills: BillMap(
            ("1000", 1000m, "One-thousand-yen note"), ("2000", 2000m, "Two-thousand-yen note"), ("5000", 5000m, "Five-thousand-yen note"),
            ("10000", 10000m, "Ten-thousand-yen note")));

    public static readonly CurrencyInfo Cad = Create(
        "CAD", 124, "Canadian Dollar", "$", 2, coinSymbol: "¢",
        coins: CoinMap(("nickel", 0.05m, "Nickel"), ("dime", 0.10m, "Dime"), ("quarter", 0.25m, "Quarter"), ("1", 1m, "Loonie"), ("2", 2m, "Toonie")),
        bills: BillMap(
            ("5", 5m, "Five-dollar bill"), ("10", 10m, "Ten-dollar bill"), ("20", 20m, "Twenty-dollar bill"), ("50", 50m, "Fifty-dollar bill"),
            ("100", 100m, "One-hundred-dollar bill")));

    public static readonly CurrencyInfo Aud = Create(
        "AUD", 36, "Australian Dollar", "$", 2, coinSymbol: "c",
        coins: CoinMap(
            ("5cent", 0.05m, "Five-cent coin"), ("10cent", 0.10m, "Ten-cent coin"), ("20cent", 0.20m, "Twenty-cent coin"), ("50cent", 0.50m, "Fifty-cent coin"),
            ("1", 1m, "One-dollar coin"), ("2", 2m, "Two-dollar coin")),
        bills: BillMap(
            ("5", 5m, "Five-dollar note"), ("10", 10m, "Ten-dollar note"), ("20", 20m, "Twenty-dollar note"), ("50", 50m, "Fifty-dollar note"),
            ("100", 100m, "One-hundred-dollar note")));

    public static readonly CurrencyInfo Chf = Create(
        "CHF", 756, "Swiss Franc", "Fr", 2, coinSymbol: "c",
        coins: CoinMap(
            ("5cent", 0.05m, "Five-centime coin"), ("10cent", 0.10m, "Ten-centime coin"), ("20cent", 0.20m, "Twenty-centime coin"), ("50cent", 0.50m, "Half-franc coin"),
            ("1", 1m, "One-franc coin"), ("2", 2m, "Two-franc coin"), ("5", 5m, "Five-franc coin")),
        bills: BillMap(
            ("10", 10m, "Ten-franc note"), ("20", 20m, "Twenty-franc note"), ("50", 50m, "Fifty-franc note"), ("100", 100m, "One-hundred-franc note"),
            ("200", 200m, "Two-hundred-franc note"), ("1000", 1000m, "One-thousand-franc note")));

    public static readonly CurrencyInfo Cny = Create(
        "CNY", 156, "Yuan Renminbi", "¥", 2, coinSymbol: "角", coins: CoinMap(("1jiao", 0.1m, "One-jiao coin"), ("5jiao", 0.5m, "Five-jiao coin"), ("1", 1m, "One-yuan coin")),
        bills: BillMap(
            ("1", 1m, "One-yuan note"), ("5", 5m, "Five-yuan note"), ("10", 10m, "Ten-yuan note"), ("20", 20m, "Twenty-yuan note"), ("50", 50m, "Fifty-yuan note"),
            ("100", 100m, "One-hundred-yuan note")));

    public static readonly CurrencyInfo Inr = Create(
        "INR", 356, "Indian Rupee", "₹", 2, coinSymbol: "₹",
        coins: CoinMap(("1", 1m, "One-rupee coin"), ("2", 2m, "Two-rupee coin"), ("5", 5m, "Five-rupee coin"), ("10", 10m, "Ten-rupee coin"), ("20", 20m, "Twenty-rupee coin")),
        bills: BillMap(
            ("10", 10m, "Ten-rupee note"), ("20", 20m, "Twenty-rupee note"), ("50", 50m, "Fifty-rupee note"), ("100", 100m, "One-hundred-rupee note"),
            ("200", 200m, "Two-hundred-rupee note"), ("500", 500m, "Five-hundred-rupee note"), ("2000", 2000m, "Two-thousand-rupee note")));

    public static readonly CurrencyInfo Mxn = Create(
        "MXN", 484, "Mexican Peso", "$", 2, coinSymbol: "¢",
        coins: CoinMap(
            ("10cent", 0.10m, "Ten-centavo coin"), ("20cent", 0.20m, "Twenty-centavo coin"), ("50cent", 0.50m, "Fifty-centavo coin"), ("1", 1m, "One-peso coin"),
            ("2", 2m, "Two-peso coin"), ("5", 5m, "Five-peso coin"), ("10", 10m, "Ten-peso coin"), ("20", 20m, "Twenty-peso coin")),
        bills: BillMap(
            ("20", 20m, "Twenty-peso note"), ("50", 50m, "Fifty-peso note"), ("100", 100m, "One-hundred-peso note"), ("200", 200m, "Two-hundred-peso note"),
            ("500", 500m, "Five-hundred-peso note"), ("1000", 1000m, "One-thousand-peso note")));

    public static readonly CurrencyInfo Brl = Create(
        "BRL", 986, "Brazilian Real", "R$", 2, coinSymbol: "¢",
        coins: CoinMap(
            ("5cent", 0.05m, "Five-centavo coin"), ("10cent", 0.10m, "Ten-centavo coin"), ("25cent", 0.25m, "Twenty-five-centavo coin"), ("50cent", 0.50m, "Fifty-centavo coin"),
            ("1", 1m, "One-real coin")),
        bills: BillMap(
            ("2", 2m, "Two-real note"), ("5", 5m, "Five-real note"), ("10", 10m, "Ten-real note"), ("20", 20m, "Twenty-real note"), ("50", 50m, "Fifty-real note"),
            ("100", 100m, "One-hundred-real note"), ("200", 200m, "Two-hundred-real note")));

    public static readonly CurrencyInfo Krw = Create(
        "KRW", 410, "Won", "₩", 0, coinSymbol: "₩",
        coins: CoinMap(("10", 10m, "Ten-won coin"), ("50", 50m, "Fifty-won coin"), ("100", 100m, "One-hundred-won coin"), ("500", 500m, "Five-hundred-won coin")),
        bills: BillMap(
            ("1000", 1000m, "One-thousand-won note"), ("5000", 5000m, "Five-thousand-won note"), ("10000", 10000m, "Ten-thousand-won note"),
            ("50000", 50000m, "Fifty-thousand-won note")));

    public static readonly CurrencyInfo Sek = Create(
        "SEK", 752, "Swedish Krona", "kr", 2, coinSymbol: "kr",
        coins: CoinMap(("1", 1m, "One-krona coin"), ("2", 2m, "Two-kronor coin"), ("5", 5m, "Five-kronor coin"), ("10", 10m, "Ten-kronor coin")),
        bills: BillMap(
            ("20", 20m, "Twenty-kronor note"), ("50", 50m, "Fifty-kronor note"), ("100", 100m, "One-hundred-kronor note"), ("200", 200m, "Two-hundred-kronor note"),
            ("500", 500m, "Five-hundred-kronor note"), ("1000", 1000m, "One-thousand-kronor note")));

    public static readonly CurrencyInfo Nok = Create(
        "NOK", 578, "Norwegian Krone", "kr", 2, coinSymbol: "kr",
        coins: CoinMap(("1", 1m, "One-krone coin"), ("5", 5m, "Five-krone coin"), ("10", 10m, "Ten-krone coin"), ("20", 20m, "Twenty-krone coin")),
        bills: BillMap(
            ("50", 50m, "Fifty-krone note"), ("100", 100m, "One-hundred-krone note"), ("200", 200m, "Two-hundred-krone note"), ("500", 500m, "Five-hundred-krone note"),
            ("1000", 1000m, "One-thousand-krone note")));

    public static readonly CurrencyInfo Dkk = Create(
        "DKK", 208, "Danish Krone", "kr", 2, coinSymbol: "øre",
        coins: CoinMap(
            ("50ore", 0.50m, "Fifty-øre coin"), ("1", 1m, "One-krone coin"), ("2", 2m, "Two-krone coin"), ("5", 5m, "Five-krone coin"), ("10", 10m, "Ten-krone coin"),
            ("20", 20m, "Twenty-krone coin")),
        bills: BillMap(
            ("50", 50m, "Fifty-krone note"), ("100", 100m, "One-hundred-krone note"), ("200", 200m, "Two-hundred-krone note"), ("500", 500m, "Five-hundred-krone note"),
            ("1000", 1000m, "One-thousand-krone note")));

    public static readonly CurrencyInfo Nzd = Create(
        "NZD", 554, "New Zealand Dollar", "$", 2, coinSymbol: "c",
        coins: CoinMap(
            ("10cent", 0.10m, "Ten-cent coin"), ("20cent", 0.20m, "Twenty-cent coin"), ("50cent", 0.50m, "Fifty-cent coin"), ("1", 1m, "One-dollar coin"),
            ("2", 2m, "Two-dollar coin")),
        bills: BillMap(
            ("5", 5m, "Five-dollar note"), ("10", 10m, "Ten-dollar note"), ("20", 20m, "Twenty-dollar note"), ("50", 50m, "Fifty-dollar note"),
            ("100", 100m, "One-hundred-dollar note")));

    public static readonly CurrencyInfo Sgd = Create(
        "SGD", 702, "Singapore Dollar", "$", 2, coinSymbol: "¢",
        coins: CoinMap(
            ("5cent", 0.05m, "Five-cent coin"), ("10cent", 0.10m, "Ten-cent coin"), ("20cent", 0.20m, "Twenty-cent coin"), ("50cent", 0.50m, "Fifty-cent coin"),
            ("1", 1m, "One-dollar coin")),
        bills: BillMap(
            ("2", 2m, "Two-dollar note"), ("5", 5m, "Five-dollar note"), ("10", 10m, "Ten-dollar note"), ("50", 50m, "Fifty-dollar note"), ("100", 100m, "One-hundred-dollar note"),
            ("1000", 1000m, "One-thousand-dollar note")));

    public static readonly CurrencyInfo Hkd = Create(
        "HKD", 344, "Hong Kong Dollar", "$", 2, coinSymbol: "¢",
        coins: CoinMap(
            ("10cent", 0.10m, "Ten-cent coin"), ("20cent", 0.20m, "Twenty-cent coin"), ("50cent", 0.50m, "Fifty-cent coin"), ("1", 1m, "One-dollar coin"),
            ("2", 2m, "Two-dollar coin"), ("5", 5m, "Five-dollar coin"), ("10", 10m, "Ten-dollar coin")),
        bills: BillMap(
            ("10", 10m, "Ten-dollar note"), ("20", 20m, "Twenty-dollar note"), ("50", 50m, "Fifty-dollar note"), ("100", 100m, "One-hundred-dollar note"),
            ("500", 500m, "Five-hundred-dollar note"), ("1000", 1000m, "One-thousand-dollar note")));

    public static readonly CurrencyInfo Zar = Create(
        "ZAR", 710, "Rand", "R", 2, coinSymbol: "c",
        coins: CoinMap(
            ("10cent", 0.10m, "Ten-cent coin"), ("20cent", 0.20m, "Twenty-cent coin"), ("50cent", 0.50m, "Fifty-cent coin"), ("1", 1m, "One-rand coin"), ("2", 2m, "Two-rand coin"),
            ("5", 5m, "Five-rand coin")),
        bills: BillMap(
            ("10", 10m, "Ten-rand note"), ("20", 20m, "Twenty-rand note"), ("50", 50m, "Fifty-rand note"), ("100", 100m, "One-hundred-rand note"),
            ("200", 200m, "Two-hundred-rand note")));

    public static readonly CurrencyInfo Aed = Create(
        "AED", 784, "UAE Dirham", "د.إ", 2, coinSymbol: "فلس",
        coins: CoinMap(("25fils", 0.25m, "Twenty-five-fils coin"), ("50fils", 0.50m, "Fifty-fils coin"), ("1", 1m, "One-dirham coin")),
        bills: BillMap(
            ("5", 5m, "Five-dirham note"), ("10", 10m, "Ten-dirham note"), ("20", 20m, "Twenty-dirham note"), ("50", 50m, "Fifty-dirham note"),
            ("100", 100m, "One-hundred-dirham note"), ("200", 200m, "Two-hundred-dirham note"), ("500", 500m, "Five-hundred-dirham note"),
            ("1000", 1000m, "One-thousand-dirham note")));

    private static readonly Dictionary<string, CurrencyInfo> ByCode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, CurrencyInfo> ByNumericCode = [];
    private static readonly List<CurrencyInfo> AllCurrencies = [];

    /// <summary>Registered currencies, excluding <see cref="Unknown" />.</summary>
    public static IReadOnlyList<CurrencyInfo> All => AllCurrencies;

    /// <summary>Circulating coins keyed by short slug (for example <c>nickel</c>, <c>10cent</c>). Empty when not cataloged.</summary>
    public IReadOnlyDictionary<string, CurrencyDenominationInfo> Coins { get; private init; } = EmptyDenominations;

    /// <summary>Circulating bills keyed by short slug (for example <c>20</c>, <c>100</c>). Empty when not cataloged.</summary>
    public IReadOnlyDictionary<string, CurrencyDenominationInfo> Bills { get; private init; } = EmptyDenominations;

    /// <summary>
    /// Glyph for coin / fractional amounts (for example <c>¢</c>, <c>p</c>, <c>øre</c>). Falls back to <see cref="Symbol" /> when unset (same major-unit glyph for coins and
    /// bills).
    /// </summary>
    public string CoinSymbol { get; private init; } = string.Empty;

    /// <summary>True when bill and coin denominations are cataloged for this currency.</summary>
    public bool IsDenominationCataloged => DenominationCoverage == CurrencyDenominationCoverage.Cataloged;

    static CurrencyInfo()
    {
        var fields = typeof(CurrencyInfo).PublicStaticFields<CurrencyInfo>();
        foreach (var currency in fields) {
            if (currency == Unknown)
                continue;

            AllCurrencies.Add(currency);
            ByCode[currency.Code] = currency;
            ByNumericCode[currency.NumericCode] = currency;
        }
    }

    /// <summary>Cataloged circulating denominations sorted by value descending, or empty when not cataloged.</summary>
    public IReadOnlyList<CurrencyDenominationInfo> GetDenominations()
    {
        if (!IsDenominationCataloged)
            return [];

        return Bills.Values.Concat(Coins.Values).OrderByDescending(d => d.Value).ToArray();
    }

    /// <summary>Looks up a denomination by slug in <see cref="Coins" /> first, then <see cref="Bills" />.</summary>
    public bool TryGetDenomination(string? key, out CurrencyDenominationInfo denomination)
    {
        denomination = null!;
        if (key.IsNullOrWhitespace() || !IsDenominationCataloged)
            return false;

        var trimmed = key.Trim();
        if (Coins.TryGetValue(trimmed, out denomination!))
            return true;

        return Bills.TryGetValue(trimmed, out denomination!);
    }

    /// <summary>Looks up a denomination by major-unit value and kind.</summary>
    public bool TryGetDenomination(decimal value, CurrencyDenominationKind kind, out CurrencyDenominationInfo denomination)
    {
        denomination = null!;
        if (!IsDenominationCataloged)
            return false;

        var map = kind == CurrencyDenominationKind.Coin ? Coins : Bills;
        foreach (var row in map.Values) {
            if (row.Value == value) {
                denomination = row;
                return true;
            }
        }

        return false;
    }

    public override string ToString() => string.IsNullOrEmpty(Code) ? Name : $"{Name} ({Code})";

    /// <summary>Looks up a currency by ISO 4217 alpha-3 code.</summary>
    /// <param name="code">Alpha-3 token, for example <c>USD</c> or <c>jpy</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the code is not registered.</returns>
    public static CurrencyInfo FromCode(string? code)
    {
        if (code.IsNullOrWhitespace())
            return Unknown;

        return ByCode.TryGetValue(code.Trim(), out var currency) ? currency : Unknown;
    }

    /// <summary>Looks up a currency by ISO 4217 numeric code.</summary>
    /// <param name="numericCode">Numeric code, for example <c>840</c> for USD or <c>392</c> for JPY.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the code is not registered.</returns>
    public static CurrencyInfo FromNumericCode(int numericCode) => ByNumericCode.GetValueOrDefault(numericCode, Unknown)!;

    private static CurrencyInfo Create(
        string code,
        int numericCode,
        string name,
        string symbol,
        int minorUnits,
        IReadOnlyDictionary<string, CurrencyDenominationInfo> coins,
        IReadOnlyDictionary<string, CurrencyDenominationInfo> bills,
        string? coinSymbol = null)
        => new(code, numericCode, name, symbol, minorUnits, CurrencyDenominationCoverage.Cataloged) { Coins = coins, Bills = bills, CoinSymbol = coinSymbol ?? symbol };

    private static IReadOnlyDictionary<string, CurrencyDenominationInfo> CoinMap(params (string Key, decimal Value, string Name)[] entries)
        => Map(CurrencyDenominationKind.Coin, entries);

    private static IReadOnlyDictionary<string, CurrencyDenominationInfo> BillMap(params (string Key, decimal Value, string Name)[] entries)
        => Map(CurrencyDenominationKind.Bill, entries);

    private static IReadOnlyDictionary<string, CurrencyDenominationInfo> Map(CurrencyDenominationKind kind, params (string Key, decimal Value, string Name)[] entries)
    {
        var map = new Dictionary<string, CurrencyDenominationInfo>(entries.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value, name) in entries)
            map[key] = new(value, kind, name);

        return map;
    }
}