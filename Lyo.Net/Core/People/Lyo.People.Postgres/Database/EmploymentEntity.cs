using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing employment records in PostgreSQL.</summary>
public sealed class EmploymentEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Guid? CompanyAddressId { get; set; }

    public Guid? SupervisorPersonId { get; set; }

    public decimal? Salary { get; set; }

    [MaxLength(3)]
    public string? SalaryCurrency { get; set; }

    [MaxLength(20)]
    public string? Type { get; set; } // FullTime, PartTime, Contract, etc.

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    [ForeignKey(nameof(PersonId))]
    public PersonEntity? Person { get; set; }

    [ForeignKey(nameof(CompanyAddressId))]
    public AddressEntity? CompanyAddress { get; set; }
}