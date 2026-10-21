using System.Text;
using Lyo.Exceptions;

namespace Lyo.People.Models;

/// <summary>Reads and writes one vCard 3.0. Repeated TEL, EMAIL, and ADR values are kept as lists.</summary>
public sealed class VCardDocument
{
    /// <summary>Reads the first vCard in <paramref name="text" />.</summary>
    public VCardCard Read(string text)
    {
        ArgumentHelpers.ThrowIfNull(text);
        var card = new VCardCard();
        foreach (var line in Unfold(text)) {
            var colon = line.IndexOf(':');
            if (colon <= 0)
                continue;

            var name = line[..colon];
            var semi = name.IndexOf(';');
            if (semi >= 0)
                name = name[..semi];

            var value = Unescape(line[(colon + 1)..]);
            switch (name.ToUpperInvariant()) {
                case "FN":
                    card.FormattedName = value;
                    break;
                case "N":
                    var parts = value.Split(';');
                    card.FamilyName = Part(parts, 0);
                    card.GivenName = Part(parts, 1);
                    break;
                case "TEL":
                    card.Telephones.Add(value);
                    break;
                case "EMAIL":
                    card.Emails.Add(value);
                    break;
                case "ADR":
                    card.Addresses.Add(ReadAddress(value));
                    break;
                case "ORG":
                    card.Organization = value;
                    break;
                case "TITLE":
                    card.Title = value;
                    break;
                case "URL":
                    card.Url = value;
                    break;
            }
        }

        return card;
    }

    /// <summary>Writes one vCard 3.0. Every telephone, email, and address is emitted.</summary>
    public string Write(VCardCard card)
    {
        ArgumentHelpers.ThrowIfNull(card);
        var builder = new StringBuilder();
        builder.Append("BEGIN:VCARD\r\nVERSION:3.0\r\n");
        if (!string.IsNullOrEmpty(card.FormattedName))
            builder.Append("FN:").Append(Escape(card.FormattedName!)).Append("\r\n");

        if (!string.IsNullOrEmpty(card.FamilyName) || !string.IsNullOrEmpty(card.GivenName))
            builder.Append("N:").Append(Escape(card.FamilyName ?? "")).Append(';').Append(Escape(card.GivenName ?? "")).Append(";;;\r\n");

        foreach (var telephone in card.Telephones)
            builder.Append("TEL:").Append(Escape(telephone)).Append("\r\n");

        foreach (var email in card.Emails)
            builder.Append("EMAIL:").Append(Escape(email)).Append("\r\n");

        foreach (var address in card.Addresses)
            builder.Append("ADR:;;")
                .Append(Escape(address.Street))
                .Append(';')
                .Append(Escape(address.Locality))
                .Append(';')
                .Append(Escape(address.Region))
                .Append(';')
                .Append(Escape(address.PostalCode))
                .Append(';')
                .Append(Escape(address.Country))
                .Append("\r\n");

        if (!string.IsNullOrEmpty(card.Organization))
            builder.Append("ORG:").Append(Escape(card.Organization!)).Append("\r\n");

        if (!string.IsNullOrEmpty(card.Title))
            builder.Append("TITLE:").Append(Escape(card.Title!)).Append("\r\n");

        if (!string.IsNullOrEmpty(card.Url))
            builder.Append("URL:").Append(Escape(card.Url!)).Append("\r\n");

        builder.Append("END:VCARD\r\n");
        return builder.ToString();
    }

    private static VCardAddress ReadAddress(string value)
    {
        var parts = value.Split(';');
        return new() {
            Street = Part(parts, 2),
            Locality = Part(parts, 3),
            Region = Part(parts, 4),
            PostalCode = Part(parts, 5),
            Country = Part(parts, 6)
        };
    }

    private static string? Part(string[] parts, int index) => index < parts.Length && parts[index].Length > 0 ? parts[index] : null;

    private static List<string> Unfold(string text)
    {
        var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lines = new List<string>();
        foreach (var rawLine in raw) {
            if (rawLine.Length == 0)
                continue;

            if ((rawLine[0] == ' ' || rawLine[0] == '\t') && lines.Count > 0)
                lines[lines.Count - 1] += rawLine.Substring(1);
            else
                lines.Add(rawLine);
        }

        return lines;
    }

    private static string Escape(string? value) => (value ?? "").Replace("\\", "\\\\").Replace("\n", "\\n").Replace(",", "\\,").Replace(";", "\\;");

    private static string Unescape(string value) => value.Replace("\\n", "\n").Replace("\\,", ",").Replace("\\;", ";").Replace("\\\\", "\\");
}