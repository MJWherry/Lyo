#if NET6_0_OR_GREATER
#else
using DateOnly = Lyo.DateAndTime.DateOnlyModel;
#endif
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Enums;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Preferences;
using Lyo.People.Models.Relationships;

namespace Lyo.People.Models;

/// <summary>Core person model representing an individual with contact info, demographics, and relationships</summary>
public class Person
{
    /// <summary>Unique identifier for the person</summary>
    public Guid Id { get; set; }

    /// <summary>Person's name</summary>
    public PersonName Name { get; set; } = null!;

    /// <summary>Date of birth</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Calculated age based on date of birth</summary>
    public int? Age {
        get {
            if (DateOfBirth is null)
                return null;
#if NET6_0_OR_GREATER
            var dob = DateOfBirth.Value;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return today.Year - dob.Year - (today.DayOfYear < dob.DayOfYear ? 1 : 0);
#else
            var dob = DateOfBirth!;
            var today = DateTime.UtcNow;
            var dobDayOfYear = new DateTime(dob.Year, dob.Month, dob.Day).DayOfYear;
            return today.Year - dob.Year - (today.DayOfYear < dobDayOfYear ? 1 : 0);
#endif
        }
    }

    /// <summary>Sex (biological)</summary>
    public Sex? Sex { get; set; }

    // Demographics (using Lyo.Common enums)
    /// <summary>Nationality/country of origin</summary>
    public CountryCode? Nationality { get; set; }

    /// <summary>Preferred language for communication</summary>
    public LanguageCodeInfo? PreferredLanguage { get; set; }

    /// <summary>Race classification</summary>
    public Race? Race { get; set; }

    /// <summary>Marital status</summary>
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Disability status</summary>
    public DisabilityStatus? DisabilityStatus { get; set; }

    /// <summary>Veteran status</summary>
    public VeteranStatus? VeteranStatus { get; set; }

    /// <summary>Place of birth (address reference)</summary>
    public Guid? PlaceOfBirthAddressId { get; set; }

    /// <summary>Citizenship countries</summary>
    public ICollection<CountryCode> Citizenship { get; set; } = new List<CountryCode>();

    /// <summary>Emergency contact person ID</summary>
    public Guid? EmergencyContactPersonId { get; set; }

    // Contact info
    /// <summary>Email addresses associated with this person</summary>
    public ICollection<ContactEmailAddress> EmailAddresses { get; set; } = new List<ContactEmailAddress>();

    /// <summary>Phone numbers associated with this person</summary>
    public ICollection<ContactPhoneNumber> PhoneNumbers { get; set; } = new List<ContactPhoneNumber>();

    /// <summary>Social media profiles</summary>
    public ICollection<SocialMediaProfile> SocialProfiles { get; set; } = new List<SocialMediaProfile>();

    // Addresses (using geolocation models)
    /// <summary>Addresses associated with this person</summary>
    public ICollection<ContactAddress> Addresses { get; set; } = new List<ContactAddress>();

    // Identification
    /// <summary>Identification documents</summary>
    public ICollection<Identification> Identifications { get; set; } = new List<Identification>();

    // Relationships
    /// <summary>Relationships with other people</summary>
    public ICollection<PersonRelationship> Relationships { get; set; } = new List<PersonRelationship>();

    // Employment/Organization
    /// <summary>Employment history</summary>
    public ICollection<Employment> Employments { get; set; } = new List<Employment>();

    /// <summary>Current job title</summary>
    public string? CurrentJobTitle { get; set; }

    /// <summary>Current company name</summary>
    public string? CurrentCompany { get; set; }

    // Preferences
    /// <summary>Person preferences including contact and privacy settings</summary>
    public PersonPreferences Preferences { get; set; } = new();

    // Metadata
    /// <summary>Date and time when the person record was created</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Date and time when the person record was last updated</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>User or system that created the record</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Whether the person record is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Additional notes about the person</summary>
    public string? Notes { get; set; }

    /// <summary>Custom fields for extensibility</summary>
    public IDictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();

    // Helper methods for addresses
    /// <summary>Gets the primary address for the person</summary>
    public ContactAddress? GetPrimaryAddress() => Addresses?.FirstOrDefault(a => a.IsPrimary && a.IsCurrent);

    /// <summary>Gets the address of the specified type</summary>
    public ContactAddress? GetAddressByType(ContactAddressType type) => Addresses?.FirstOrDefault(a => a.Type == type && a.IsCurrent);

    /// <summary>Gets all current (non-ended) addresses</summary>
    public IEnumerable<ContactAddress> GetCurrentAddresses() => Addresses?.Where(a => a.IsCurrent) ?? Enumerable.Empty<ContactAddress>();

    // Helper methods for phone numbers
    /// <summary>Gets the primary phone number for the person</summary>
    public ContactPhoneNumber? GetPrimaryPhoneNumber() => PhoneNumbers?.FirstOrDefault(p => p.IsPrimary && p.IsCurrent);

    /// <summary>Gets the phone number of the specified type</summary>
    public ContactPhoneNumber? GetPhoneNumberByType(ContactPhoneType type) => PhoneNumbers?.FirstOrDefault(p => p.Type == type && p.IsCurrent);

    /// <summary>Gets all current phone numbers</summary>
    public IEnumerable<ContactPhoneNumber> GetCurrentPhoneNumbers() => PhoneNumbers?.Where(p => p.IsCurrent) ?? Enumerable.Empty<ContactPhoneNumber>();

    // Helper methods for email addresses
    /// <summary>Gets the primary email address for the person</summary>
    public ContactEmailAddress? GetPrimaryEmailAddress() => EmailAddresses?.FirstOrDefault(e => e.IsPrimary && e.IsCurrent);

    /// <summary>Gets the email address of the specified type</summary>
    public ContactEmailAddress? GetEmailAddressByType(ContactEmailType type) => EmailAddresses?.FirstOrDefault(e => e.Type == type && e.IsCurrent);

    /// <summary>Gets all current email addresses</summary>
    public IEnumerable<ContactEmailAddress> GetCurrentEmailAddresses() => EmailAddresses?.Where(e => e.IsCurrent) ?? Enumerable.Empty<ContactEmailAddress>();
}