using Lyo.Geolocation.Models.Addresses;

namespace Lyo.People.Models;

/// <summary>Represents an employment record for a person</summary>
public class Employment
{
    /// <summary>Unique identifier for the employment record</summary>
    public Guid Id { get; set; }

    /// <summary>Company or organization name</summary>
    public string CompanyName { get; set; } = null!;

    /// <summary>Job title or position</summary>
    public string? JobTitle { get; set; }

    /// <summary>Department within the company</summary>
    public string? Department { get; set; }

    /// <summary>Start date of employment</summary>
    public DateTime StartDate { get; set; }

    /// <summary>End date of employment (null if current)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Employee ID or badge number</summary>
    public string? EmployeeId { get; set; }

    /// <summary>Job description</summary>
    public string? Description { get; set; }

    /// <summary>Company address ID</summary>
    public Guid? CompanyAddressId { get; set; }

    /// <summary>Company address navigation property</summary>
    public Address? CompanyAddress { get; set; }

    /// <summary>Supervisor or manager person ID</summary>
    public Guid? SupervisorPersonId { get; set; }

    /// <summary>Salary or compensation amount</summary>
    public decimal? Salary { get; set; }

    /// <summary>Currency code for salary (ISO 4217)</summary>
    public string? SalaryCurrency { get; set; }

    /// <summary>Employment type (full-time, part-time, contract, etc.)</summary>
    public EmploymentType? Type { get; set; }

    /// <summary>Whether this is the current employment</summary>
    public bool IsCurrent => EndDate == null || EndDate > DateTime.UtcNow;

    /// <summary>Duration of employment</summary>
    public TimeSpan? Duration => EndDate.HasValue ? EndDate.Value - StartDate : DateTime.UtcNow - StartDate;

    /// <summary>Duration in years</summary>
    public double? DurationYears => Duration?.TotalDays / 365.25;

    /// <summary>Duration in months</summary>
    public double? DurationMonths => Duration?.TotalDays / 30.44;
}