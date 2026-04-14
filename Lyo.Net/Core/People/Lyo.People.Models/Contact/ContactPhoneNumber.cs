using Lyo.People.Models.Enum;

namespace Lyo.People.Models.Contact;

/// <summary>Links a person to a phone number with relationship type (home, work, etc.). Allows a person to have multiple phone numbers of different types.</summary>
public class ContactPhoneNumber
{
    /// <summary>Unique identifier for the contact-phone association</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the person this phone number belongs to</summary>
    public Guid PersonId { get; set; }

    /// <summary>ID of the phone number</summary>
    public Guid PhoneNumberId { get; set; }

    /// <summary>Type of phone (mobile, home, work, etc.)</summary>
    public ContactPhoneType Type { get; set; }

    /// <summary>Whether this is the primary phone number for the person</summary>
    public bool IsPrimary { get; set; }

    /// <summary>When the person started using this phone number</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>When the person stopped using this phone number (null = current)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether this contact is currently in use</summary>
    public bool IsCurrent => EndDate == null;

    /// <summary>Optional notes about this phone number</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation property to the phone number</summary>
    public PhoneNumber? PhoneNumber { get; set; }
}