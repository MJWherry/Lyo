using System.Diagnostics;
using Lyo.People.Models.Enum;

namespace Lyo.People.Models.Contact;

/// <summary>Person-to-email link with a role (personal, work, and so on). One person may own several addresses of different types.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ContactEmailAddress
{
    /// <summary>Primary key for this person-email link.</summary>
    public Guid Id { get; set; }

    /// <summary>Person who owns the link.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Linked <see cref="EmailAddress" /> id.</summary>
    public Guid EmailAddressId { get; set; }

    /// <summary>Role of the mailbox (personal, work, other).</summary>
    public ContactEmailType Type { get; set; }

    /// <summary>True when this is the person's primary email.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>When the person began using this mailbox.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>When the person stopped using this mailbox; null while current.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>True when the link has not ended.</summary>
    public bool IsCurrent => EndDate == null;

    /// <summary>True when marketing mail is opted out at this address.</summary>
    public bool OptedOutOfMarketing { get; set; }

    /// <summary>Free-text notes about this mailbox.</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation to the email payload.</summary>
    public EmailAddress? EmailAddress { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ContactEmailAddress: person={PersonId}, email={EmailAddress?.Email ?? EmailAddressId.ToString()}, type={Type}, primary={IsPrimary}";
}
