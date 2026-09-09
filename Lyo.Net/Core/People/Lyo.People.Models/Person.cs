#if NET6_0_OR_GREATER
#else
using DateOnly = Lyo.DateAndTime.DateOnlyModel;
#endif
using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.EntityReference.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Enums;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Preferences;
using Lyo.People.Models.Relationships;

namespace Lyo.People.Models;

/// <summary>Person aggregate: contact details, demographics, and relationships for one individual.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Person : IEntitySourceDerived
{
    /// <summary>Primary key for this person.</summary>
    public Guid Id { get; set; }

    /// <summary>Structured name for the person.</summary>
    public PersonName Name { get; set; } = null!;

    /// <summary>Birth date, when known.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Age derived from <see cref="DateOfBirth" />.</summary>
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

    /// <summary>Biological sex, when recorded.</summary>
    public Sex? Sex { get; set; }

    // Demographics (Lyo.Common.Core enums)
    /// <summary>Nationality or country of origin.</summary>
    public CountryCode? Nationality { get; set; }

    /// <summary>Language the person prefers for communication.</summary>
    public LanguageCodeInfo? PreferredLanguage { get; set; }

    /// <summary>Race classification, when recorded.</summary>
    public Race? Race { get; set; }

    /// <summary>Marital status, when recorded.</summary>
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>Disability status, when recorded.</summary>
    public DisabilityStatus? DisabilityStatus { get; set; }

    /// <summary>Veteran status, when recorded.</summary>
    public VeteranStatus? VeteranStatus { get; set; }

    /// <summary>Birth-place address id, when known.</summary>
    public Guid? PlaceOfBirthAddressId { get; set; }

    /// <summary>Countries of citizenship.</summary>
    public ICollection<CountryCode> Citizenship { get; set; } = new List<CountryCode>();

    /// <summary>Id of the emergency-contact person.</summary>
    public Guid? EmergencyContactPersonId { get; set; }

    // Contact
    /// <summary>Email links owned by this person.</summary>
    public ICollection<ContactEmailAddress> EmailAddresses { get; set; } = new List<ContactEmailAddress>();

    /// <summary>Phone links owned by this person.</summary>
    public ICollection<ContactPhoneNumber> PhoneNumbers { get; set; } = new List<ContactPhoneNumber>();

    /// <summary>Social-media profiles for this person.</summary>
    public ICollection<SocialMediaProfile> SocialProfiles { get; set; } = new List<SocialMediaProfile>();

    // Addresses (geolocation models)
    /// <summary>Address links owned by this person.</summary>
    public ICollection<ContactAddress> Addresses { get; set; } = new List<ContactAddress>();

    // Identification
    /// <summary>Identity documents on file.</summary>
    public ICollection<Identification> Identifications { get; set; } = new List<Identification>();

    // Relationships
    /// <summary>Links to other people.</summary>
    public ICollection<PersonRelationship> Relationships { get; set; } = new List<PersonRelationship>();

    // Employment
    /// <summary>Past and current employment rows.</summary>
    public ICollection<Employment> Employments { get; set; } = new List<Employment>();

    /// <summary>Job title at the current employer, when known.</summary>
    public string? CurrentJobTitle { get; set; }

    /// <summary>Current employer name, when known.</summary>
    public string? CurrentCompany { get; set; }

    // Preferences
    /// <summary>Contact and privacy preferences.</summary>
    public PersonPreferences Preferences { get; set; } = new();

    // Metadata
    /// <summary>When this person row was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this person row was last changed.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>User or system that created the row.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>True when the person row is treated as active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Free-text notes about the person.</summary>
    public string? Notes { get; set; }

    /// <summary>Caller-defined key/value extras.</summary>
    public IDictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();

    /// <inheritdoc />
    public EntitySourceRecord? Source { get; set; }

    /// <inheritdoc />
    public DateTime? LocallyModifiedAt { get; set; }

    // Address helpers
    /// <summary>Primary current address, if one is marked.</summary>
    public ContactAddress? GetPrimaryAddress() => Addresses.FirstOrDefault(a => a.IsPrimary && a.IsCurrent);

    /// <summary>Current address of the given type, if present.</summary>
    public ContactAddress? GetAddressByType(ContactAddressType type) => Addresses.FirstOrDefault(a => a.Type == type && a.IsCurrent);

    /// <summary>Addresses that have not ended.</summary>
    public IEnumerable<ContactAddress> GetCurrentAddresses() => Addresses.Where(a => a.IsCurrent);

    // Phone helpers
    /// <summary>Primary current phone, if one is marked.</summary>
    public ContactPhoneNumber? GetPrimaryPhoneNumber() => PhoneNumbers.FirstOrDefault(p => p.IsPrimary && p.IsCurrent);

    /// <summary>Current phone of the given type, if present.</summary>
    public ContactPhoneNumber? GetPhoneNumberByType(ContactPhoneType type) => PhoneNumbers.FirstOrDefault(p => p.Type == type && p.IsCurrent);

    /// <summary>Phones that have not ended.</summary>
    public IEnumerable<ContactPhoneNumber> GetCurrentPhoneNumbers() => PhoneNumbers.Where(p => p.IsCurrent);

    // Email helpers
    /// <summary>Primary current email, if one is marked.</summary>
    public ContactEmailAddress? GetPrimaryEmailAddress() => EmailAddresses.FirstOrDefault(e => e.IsPrimary && e.IsCurrent);

    /// <summary>Current email of the given type, if present.</summary>
    public ContactEmailAddress? GetEmailAddressByType(ContactEmailType type) => EmailAddresses.FirstOrDefault(e => e.Type == type && e.IsCurrent);

    /// <summary>Emails that have not ended.</summary>
    public IEnumerable<ContactEmailAddress> GetCurrentEmailAddresses() => EmailAddresses.Where(e => e.IsCurrent);

    /// <inheritdoc />
    public override string ToString() => $"Person: id={Id}, name={Name.DisplayName}, emails={EmailAddresses.Count}, phones={PhoneNumbers.Count}";
}