using Lyo.People.Models.Enum;

namespace Lyo.People.Models.Contact;

/// <summary>Links a person to an email address with relationship type (personal, work, etc.). Allows a person to have multiple email addresses of different types.</summary>
public class ContactEmailAddress
{
    /// <summary>Unique identifier for the contact-email association</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the person this email address belongs to</summary>
    public Guid PersonId { get; set; }

    /// <summary>ID of the email address</summary>
    public Guid EmailAddressId { get; set; }

    /// <summary>Type of email (personal, work, other)</summary>
    public ContactEmailType Type { get; set; }

    /// <summary>Whether this is the primary email address for the person</summary>
    public bool IsPrimary { get; set; }

    /// <summary>When the person started using this email address</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>When the person stopped using this email address (null = current)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether this contact is currently in use</summary>
    public bool IsCurrent => EndDate == null;

    /// <summary>Whether the person has opted out of marketing emails at this address</summary>
    public bool OptedOutOfMarketing { get; set; }

    /// <summary>Optional notes about this email address</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation property to the email address</summary>
    public EmailAddress? EmailAddress { get; set; }
}