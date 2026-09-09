using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.People.Models.Enum;

namespace Lyo.People.Models;

/// <summary>Name parts and derived display/format strings for a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonName
{
    /// <summary>Honorific prefix (Mr., Mrs., Dr., and so on).</summary>
    public NamePrefix? Prefix { get; set; }

    /// <summary>Given name.</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>Middle name or names.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Family name.</summary>
    public string LastName { get; set; } = null!;

    /// <summary>Honorific suffix (Jr., Sr., III, PhD, and so on).</summary>
    public NameSuffix? Suffix { get; set; }

    /// <summary>Nickname or preferred given name.</summary>
    public string? PreferredName { get; set; }

    /// <summary>Maiden family name (often used after marriage).</summary>
    public string? MaidenName { get; set; }

    /// <summary>Given name plus family name.</summary>
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Preferred name when set; otherwise <see cref="FullName" />.</summary>
    public string DisplayName => PreferredName ?? FullName;

    /// <summary>Formal rendering including prefix and suffix when present.</summary>
    public string FormalName {
        get {
            var parts = new List<string>();
            if (Prefix.HasValue) {
                var prefixDesc = Prefix.Value.GetDescription();
                if (!prefixDesc.IsNullOrEmpty())
                    parts.Add(prefixDesc);
            }

            parts.Add(FullName);
            if (Suffix.HasValue) {
                var suffixDesc = Suffix.Value.GetDescription();
                if (!suffixDesc.IsNullOrEmpty())
                    parts.Add(suffixDesc);
            }

            return string.Join(" ", parts);
        }
    }

    /// <summary>Given, middle, and family names joined.</summary>
    public string FullNameWithMiddle {
        get {
            var parts = new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" ", parts);
        }
    }

    /// <summary>Family name first (for example, "Smith, John").</summary>
    public string LastNameFirst => $"{LastName}, {FirstName}";

    /// <summary>Initials from given and family names.</summary>
    public string GetInitials()
    {
        var initials = "";
        if (!string.IsNullOrWhiteSpace(FirstName))
            initials += FirstName[0];

        if (!string.IsNullOrWhiteSpace(LastName))
            initials += LastName[0];

        return initials.ToUpperInvariant();
    }

    /// <summary>Name rendered with the chosen <paramref name="format" />.</summary>
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

    /// <inheritdoc />
    public override string ToString() => $"PersonName: {DisplayName}";
}
