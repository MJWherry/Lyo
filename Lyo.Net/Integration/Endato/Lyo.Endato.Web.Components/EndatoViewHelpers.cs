using Lyo.Endato.Client.Models.Person.Response;

namespace Lyo.Endato.Web.Components;

internal static class EndatoViewHelpers
{
    public const string NullPlaceholder = "null";

    private static readonly string[] KnownPhoneTypes = ["Mobile", "Home", "Work", "Fax", "LandLine", "LandLine/Services", "Other"];

    public static string Field(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value;

    public static string Text(string? value) => Field(value);

    public static string FormatPhone(string? number)
    {
        var formatted = EndatoContactFormatter.FormatNumber(number);
        return string.IsNullOrWhiteSpace(formatted) ? "" : formatted;
    }

    public static string FormatDate(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    public static string FormatDateRange(string? first, string? last)
    {
        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            return "";

        return $"{FormatDate(first)} – {FormatDate(last)}";
    }

    public static string FormatCoordinates(decimal? latitude, decimal? longitude)
        => latitude is null && longitude is null ? "" : $"{latitude}, {longitude}";

    public static string FormatName(Name? name)
    {
        if (name is null)
            return "";

        return FormatName(name.Prefix, name.FirstName, name.MiddleName, name.LastName, name.Suffix);
    }

    public static string FormatName(string? prefix, string? first, string? middle, string? last, string? suffix)
    {
        var name = string.Join(" ", new[] { prefix, first, middle, last, suffix }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(name) ? "" : name;
    }

    public static IReadOnlyList<string> PhoneTypeOptions(string? current, IEnumerable<string?> extras)
    {
        var types = new List<string>(KnownPhoneTypes);
        foreach (var type in extras.Append(current)) {
            if (string.IsNullOrWhiteSpace(type))
                continue;
            if (types.All(t => !string.Equals(t, type, StringComparison.OrdinalIgnoreCase)))
                types.Add(type);
        }

        return types;
    }

    public static IReadOnlyList<(string Label, int Count)> ActiveIndicators(Indicators? indicators)
    {
        if (indicators is null)
            return [];

        (string Label, int Count)[] all = [
            ("Bankruptcy", indicators.HasBankruptcyRecords),
            ("Business", indicators.HasBusinessRecords),
            ("Divorce", indicators.HasDivorceRecords),
            ("Domains", indicators.HasDomainsRecords),
            ("Evictions", indicators.HasEvictionsRecords),
            ("FEIN", indicators.HasFeinRecords),
            ("Foreclosures", indicators.HasForeclosuresRecords),
            ("Foreclosures v2", indicators.HasForeclosuresV2Records),
            ("Judgments", indicators.HasJudgmentRecords),
            ("Liens", indicators.HasLienRecords),
            ("Marriage", indicators.HasMarriageRecords),
            ("Licenses", indicators.HasProfessionalLicenseRecords),
            ("Property", indicators.HasPropertyRecords),
            ("Vehicles", indicators.HasVehicleRegistrationsRecords),
            ("Workplace", indicators.HasWorkplaceRecords),
            ("DEA", indicators.HasDeaRecords),
            ("Property v2", indicators.HasPropertyV2Records),
            ("UCC", indicators.HasUccRecords),
            ("Unbanked", indicators.HasUnbankedData),
            ("Mobile phones", indicators.HasMobilePhones),
            ("Landlines", indicators.HasLandLines),
            ("Emails", indicators.HasEmails),
            ("Addresses", indicators.HasAddresses),
            ("Current addresses", indicators.HasCurrentAddresses),
            ("Historical addresses", indicators.HasHistoricalAddresses),
            ("Debt", indicators.HasDebtRecords)
        ];

        return all.Where(static i => i.Count > 0).ToArray();
    }
}
