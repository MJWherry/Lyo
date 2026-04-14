using Lyo.Common;
using Lyo.Common.Enums;

namespace Lyo.People.Models;

/// <summary>Represents a person's name with various components and formatting options</summary>
public class PersonName
{
    /// <summary>Name prefix (Mr., Mrs., Dr., etc.)</summary>
    public NamePrefix? Prefix { get; set; }

    /// <summary>First name</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>Middle name(s)</summary>
    public string? MiddleName { get; set; }

    /// <summary>Last name</summary>
    public string LastName { get; set; } = null!;

    /// <summary>Name suffix (Jr., Sr., III, PhD, etc.)</summary>
    public NameSuffix? Suffix { get; set; }

    /// <summary>Preferred name or nickname</summary>
    public string? PreferredName { get; set; }

    /// <summary>Maiden name (typically used for married women)</summary>
    public string? MaidenName { get; set; }

    /// <summary>Full name combining first and last name</summary>
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Display name using preferred name if available, otherwise full name</summary>
    public string DisplayName => PreferredName ?? FullName;

    /// <summary>Formal name including prefix and suffix</summary>
    public string FormalName {
        get {
            var parts = new List<string>();
            if (Prefix.HasValue) {
                var prefixDesc = Prefix.Value.GetDescription();
                if (!string.IsNullOrEmpty(prefixDesc))
                    parts.Add(prefixDesc!);
            }

            parts.Add(FullName);
            if (Suffix.HasValue) {
                var suffixDesc = Suffix.Value.GetDescription();
                if (!string.IsNullOrEmpty(suffixDesc))
                    parts.Add(suffixDesc!);
            }

            return string.Join(" ", parts);
        }
    }

    /// <summary>Full name with middle name included</summary>
    public string FullNameWithMiddle {
        get {
            var parts = new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" ", parts);
        }
    }

    /// <summary>Last name first format (e.g., "Smith, John")</summary>
    public string LastNameFirst => $"{LastName}, {FirstName}";

    /// <summary>Gets initials from first and last name</summary>
    public string GetInitials()
    {
        var initials = "";
        if (!string.IsNullOrWhiteSpace(FirstName))
            initials += FirstName[0];

        if (!string.IsNullOrWhiteSpace(LastName))
            initials += LastName[0];

        return initials.ToUpperInvariant();
    }

    /// <summary>Gets formatted name based on the specified format</summary>
    public string GetFormattedName(NameFormat format = NameFormat.Full)
        => format switch {
            NameFormat.Full => FullName,
            NameFormat.FullWithMiddle => FullNameWithMiddle,
            NameFormat.Formal => FormalName,
            NameFormat.Display => DisplayName,
            NameFormat.LastNameFirst => LastNameFirst,
            NameFormat.Initials => GetInitials(),
            var _ => FullName
        };
}