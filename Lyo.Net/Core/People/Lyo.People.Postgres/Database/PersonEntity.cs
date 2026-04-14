using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing person records in PostgreSQL.</summary>
public class PersonEntity
{
    [Key]
    public Guid Id { get; set; }

    // Name (flattened from PersonName)
    [MaxLength(12)]
    public string? NamePrefix { get; set; }

    [Required]
    [MaxLength(25)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(25)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(25)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(12)]
    public string? NameSuffix { get; set; }

    [MaxLength(30)]
    public string Source { get; set; } = "Manual";

    [MaxLength(100)]
    public string? PreferredName { get; set; }

    [MaxLength(100)]
    public string? MaidenName { get; set; }

    // Demographics
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(1)]
    public string? Sex { get; set; }

    [MaxLength(3)]
    public string? Nationality { get; set; }

    [MaxLength(20)]
    public string? PreferredLanguageBcp47 { get; set; }

    [MaxLength(1)]
    public string? Race { get; set; }

    [MaxLength(1)]
    public string? MaritalStatus { get; set; }

    [MaxLength(2)]
    public string? DisabilityStatus { get; set; }

    [MaxLength(2)]
    public string? VeteranStatus { get; set; }

    public Guid? PlaceOfBirthAddressId { get; set; }

    public Guid? EmergencyContactPersonId { get; set; }

    // Employment summary (denormalized for querying)
    [MaxLength(200)]
    public string? CurrentJobTitle { get; set; }

    [MaxLength(200)]
    public string? CurrentCompany { get; set; }

    // Metadata
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    [MaxLength(500)]
    public string? CreatedBy { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(4000)]
    public string? Notes { get; set; }

    /// <summary>Citizenship countries as JSON array of country codes.</summary>
    [Column(TypeName = "jsonb")]
    public string? CitizenshipJson { get; set; }

    /// <summary>Person preferences as JSON.</summary>
    [Column(TypeName = "jsonb")]
    public string? PreferencesJson { get; set; }

    /// <summary>Custom fields as JSON object.</summary>
    [Column(TypeName = "jsonb")]
    public string? CustomFieldsJson { get; set; }

    // Navigation properties
    public virtual ICollection<ContactEmailAddressEntity> ContactEmailAddresses { get; set; } = new List<ContactEmailAddressEntity>();

    public virtual ICollection<ContactPhoneNumberEntity> ContactPhoneNumbers { get; set; } = new List<ContactPhoneNumberEntity>();

    public virtual ICollection<ContactAddressEntity> ContactAddresses { get; set; } = new List<ContactAddressEntity>();
}