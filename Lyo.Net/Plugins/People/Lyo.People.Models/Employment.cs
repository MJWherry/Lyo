using System.Diagnostics;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.People.Models;

/// <summary>One employment stint for a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Employment
{
    /// <summary>Primary key for this employment row.</summary>
    public Guid Id { get; set; }

    /// <summary>Employer or organization name.</summary>
    public string CompanyName { get; set; } = null!;

    /// <summary>Role or job title.</summary>
    public string? JobTitle { get; set; }

    /// <summary>Department inside the employer.</summary>
    public string? Department { get; set; }

    /// <summary>When employment began.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>When employment ended; null while current.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Employee or badge number.</summary>
    public string? EmployeeId { get; set; }

    /// <summary>Role description.</summary>
    public string? Description { get; set; }

    /// <summary>Employer address id.</summary>
    public Guid? CompanyAddressId { get; set; }

    /// <summary>Employer address navigation.</summary>
    public Address? CompanyAddress { get; set; }

    /// <summary>Supervisor person id.</summary>
    public Guid? SupervisorPersonId { get; set; }

    /// <summary>Compensation amount.</summary>
    public decimal? Salary { get; set; }

    /// <summary>ISO 4217 currency for <see cref="Salary" />.</summary>
    public string? SalaryCurrency { get; set; }

    /// <summary>Arrangement (full-time, part-time, contract, and so on).</summary>
    public EmploymentType? Type { get; set; }

    /// <summary>True when there is no end date, or the end date is still in the future.</summary>
    public bool IsCurrent => EndDate == null || EndDate > DateTime.UtcNow;

    /// <summary>Length of the stint.</summary>
    public TimeSpan? Duration => EndDate.HasValue ? EndDate.Value - StartDate : DateTime.UtcNow - StartDate;

    /// <summary>Stint length in years.</summary>
    public double? DurationYears => Duration?.TotalDays / 365.25;

    /// <summary>Stint length in months.</summary>
    public double? DurationMonths => Duration?.TotalDays / 30.44;

    /// <inheritdoc />
    public override string ToString() => $"Employment: id={Id}, company={CompanyName}, title={JobTitle ?? "?"}, current={IsCurrent}";
}
