using System.Diagnostics;
using Lyo.People.Models.Enum;

namespace Lyo.People.Models.Contact;

/// <summary>Person-to-phone link with a role (home, work, and so on). One person may own several numbers of different types.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ContactPhoneNumber
{
    /// <summary>Primary key for this person-phone link.</summary>
    public Guid Id { get; set; }

    /// <summary>Person who owns the link.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Linked <see cref="PhoneNumber" /> id.</summary>
    public Guid PhoneNumberId { get; set; }

    /// <summary>Role of the phone (mobile, home, work, and so on).</summary>
    public ContactPhoneType Type { get; set; }

    /// <summary>True when this is the person's primary phone.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>When the person began using this number.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>When the person stopped using this number; null while current.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>True when the link has not ended.</summary>
    public bool IsCurrent => EndDate == null;

    /// <summary>Free-text notes about this number.</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation to the phone payload.</summary>
    public PhoneNumber? PhoneNumber { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ContactPhoneNumber: person={PersonId}, phone={PhoneNumber?.Number ?? PhoneNumberId.ToString()}, type={Type}, primary={IsPrimary}";
}
