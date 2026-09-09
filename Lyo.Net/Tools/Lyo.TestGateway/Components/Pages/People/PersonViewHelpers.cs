using Lyo.TestGateway.Models;

namespace Lyo.TestGateway.Components.Pages.People;

internal static class PersonViewHelpers
{
    public const string NullPlaceholder = "null";

    private static readonly string[] KnownPhoneTypes = ["Mobile", "Home", "Work", "Fax", "LandLine", "LandLine/Services", "Other"];

    public static string Field(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value;

    public static int? Age(DateOnly? dob)
    {
        if (dob is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dob.Value.Year;
        if (today < dob.Value.AddYears(age))
            age--;

        return age;
    }

    public static string AgeLabel(DateOnly? dob) => Age(dob) is { } age ? age.ToString() : "";

    public static string SourceLabel(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "";

        var i = source.LastIndexOf('.');
        return i >= 0 && i < source.Length - 1 ? source[(i + 1)..] : source;
    }

    public static string FormatPhone(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return "";

        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '1')
            digits = digits[1..];

        return digits.Length == 10 ? $"{digits[..3]}-{digits[3..6]}-{digits[6..]}" : number.Trim();
    }

    public static string FormatDate(DateOnly? value) => value?.ToString("d") ?? "";

    public static string FormatDate(DateTime? value) => value?.ToString("g") ?? "";

    public static string FormatDateRange(DateOnly? first, DateOnly? last)
    {
        if (first is null && last is null)
            return "";

        return $"{FormatDate(first)} – {FormatDate(last)}";
    }

    public static string FormatCoordinates(PersonPointRes? point) => point is null ? "" : $"{point.Y}, {point.X}";

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
}
