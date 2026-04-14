using Lyo.Common.Enums;
using Lyo.People.Models.Enum;

namespace Lyo.People.Models;

/// <summary>Represents an identification document (passport, driver's license, etc.)</summary>
public class Identification
{
    /// <summary>Unique identifier for the identification</summary>
    public Guid Id { get; set; }

    /// <summary>Type of identification document</summary>
    public IdentificationType Type { get; set; }

    /// <summary>Identification number</summary>
    public string Number { get; set; } = null!;

    /// <summary>Country that issued the identification</summary>
    public CountryCode? IssuingCountry { get; set; }

    /// <summary>Authority that issued the identification</summary>
    public string? IssuingAuthority { get; set; }

    /// <summary>Date when the identification was issued</summary>
    public DateTime? IssueDate { get; set; }

    /// <summary>Date when the identification expires</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Whether the identification has been verified</summary>
    public bool IsVerified { get; set; }

    /// <summary>URL to photo of the identification document</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Whether the identification is expired</summary>
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    /// <summary>Whether the identification is valid (not expired and verified)</summary>
    public bool IsValid => IsVerified && !IsExpired;

    /// <summary>Days until expiration (negative if expired)</summary>
    public int? DaysUntilExpiration => ExpiryDate.HasValue ? (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays : null;
}