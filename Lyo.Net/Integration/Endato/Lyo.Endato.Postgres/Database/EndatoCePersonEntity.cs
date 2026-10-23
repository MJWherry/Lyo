using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Row that stores an Endato Contact Enrichment person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EndatoCePersonEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(25)]
    public string FirstName { get; set; } = null!;

    [MaxLength(25)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(25)]
    public string LastName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public EndatoCeQueryEntity? Query { get; set; }

    public ICollection<EndatoCeAddressEntity> Addresses { get; set; } = new List<EndatoCeAddressEntity>();

    public ICollection<EndatoCePhoneNumberEntity> PhoneNumbers { get; set; } = new List<EndatoCePhoneNumberEntity>();

    public ICollection<EndatoCeEmailAddressEntity> EmailAddresses { get; set; } = new List<EndatoCeEmailAddressEntity>();

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}