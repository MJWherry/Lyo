using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Person Search person result.</summary>
public sealed class EndatoPsPersonEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid QueryId { get; set; }

    [ForeignKey(nameof(QueryId))]
    public EndatoPsQueryEntity Query { get; set; } = null!;

    [MaxLength(12)]
    public string? Prefix { get; set; }

    [MaxLength(25)]
    public string? FirstName { get; set; }

    [MaxLength(25)]
    public string? MiddleName { get; set; }

    [MaxLength(25)]
    public string? LastName { get; set; }

    [MaxLength(12)]
    public string? Suffix { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public ICollection<EndatoPsAddressEntity> Addresses { get; set; } = new List<EndatoPsAddressEntity>();

    public ICollection<EndatoPsEmailAddressEntity> EmailAddresses { get; set; } = new List<EndatoPsEmailAddressEntity>();

    public ICollection<EndatoPsPhoneNumberEntity> PhoneNumbers { get; set; } = new List<EndatoPsPhoneNumberEntity>();
}