using EnrichmentEmail = Lyo.Endato.Client.Models.Enrichment.Response.Email;
using EnrichmentPhone = Lyo.Endato.Client.Models.Enrichment.Response.Phone;
using PersonEmail = Lyo.Endato.Client.Models.Person.Response.Email;
using PersonPhone = Lyo.Endato.Client.Models.Person.Response.Phone;

namespace Lyo.Endato.Web.Components;

internal static class EndatoContactFormatter
{
    public static string FormatPossibleNumbers(IReadOnlyList<PersonPhone> phones)
        => string.Join(", ", phones
            .Where(IsMobilePhone)
            .OrderByDescending(p => ParseReportDate(p.LastReportedDate))
            .ThenBy(p => p.PhoneOrder)
            .Select(FormatPhoneEntry));

    public static string FormatPossibleEmails(IReadOnlyList<PersonEmail> emails)
        => string.Join(", ", emails
            .OrderByDescending(e => e.EmailEngagementData?.LastTouchedDate ?? DateTime.MinValue)
            .ThenBy(e => e.EmailOrdinal)
            .Select(FormatEmailEntry));

    public static string FormatEnrichmentPhones(IReadOnlyList<EnrichmentPhone> phones)
        => string.Join(", ", phones
            .OrderByDescending(p => ParseReportDate(p.LastReportedDate))
            .Select(p => $"({FormatDisplayDate(p.LastReportedDate)}) {FormatPhoneNumber(p.Number)} ({p.Type})"));

    public static string FormatEnrichmentEmails(IReadOnlyList<EnrichmentEmail> emails)
        => string.Join(", ", emails
            .OrderByDescending(e => ParseReportDate(e.LastReportedDate))
            .Select(e => $"({FormatDisplayDate(e.LastReportedDate)}) {e.EmailAddress}"));

    private static bool IsMobilePhone(PersonPhone phone)
    {
        var type = phone.PhoneType?.Trim() ?? string.Empty;
        return type.Contains("mobile", StringComparison.OrdinalIgnoreCase)
            || type.Contains("wireless", StringComparison.OrdinalIgnoreCase)
            || type.Contains("cell", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatPhoneEntry(PersonPhone phone)
        => $"({FormatDisplayDate(phone.LastReportedDate)}) {FormatPhoneNumber(phone.PhoneNumber)}";

    private static string FormatEmailEntry(PersonEmail email)
    {
        var date = email.EmailEngagementData?.LastTouchedDate;
        var dateLabel = date.HasValue ? date.Value.ToString("MM/dd/yyyy") : "unknown";
        return $"({dateLabel}) {email.EmailAddress}";
    }

    private static string FormatDisplayDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return "unknown";

        if (DateTime.TryParse(date, out var dt))
            return dt.ToString("MM/dd/yyyy");

        if (DateOnly.TryParse(date, out var d))
            return d.ToString("MM/dd/yyyy");

        return date.Trim();
    }

    private static DateTime ParseReportDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return DateTime.MinValue;

        if (DateTime.TryParse(date, out var dt))
            return dt;

        if (DateOnly.TryParse(date, out var d))
            return d.ToDateTime(TimeOnly.MinValue);

        return DateTime.MinValue;
    }

    private static string FormatPhoneNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return string.Empty;

        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '1')
            digits = digits[1..];

        return digits.Length == 10
            ? $"{digits[..3]}-{digits[3..6]}-{digits[6..]}"
            : number.Trim();
    }
}
