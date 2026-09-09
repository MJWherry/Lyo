using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.People.Models.Enum;

namespace Lyo.People.Models;

/// <summary>Identity document (passport, driver's license, and similar).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Identification
{
    /// <summary>Primary key for this document row.</summary>
    public Guid Id { get; set; }

    /// <summary>Kind of document.</summary>
    public IdentificationType Type { get; set; }

    /// <summary>Document number.</summary>
    public string Number { get; set; } = null!;

    /// <summary>Country that issued the document.</summary>
    public CountryCode? IssuingCountry { get; set; }

    /// <summary>Issuer name or agency.</summary>
    public string? IssuingAuthority { get; set; }

    /// <summary>When the document was issued.</summary>
    public DateTime? IssueDate { get; set; }

    /// <summary>When the document expires.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>True when the document has been verified.</summary>
    public bool IsVerified { get; set; }

    /// <summary>URL of a photo of the document.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>True when <see cref="ExpiryDate" /> is in the past.</summary>
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    /// <summary>True when verified and not expired.</summary>
    public bool IsValid => IsVerified && !IsExpired;

    /// <summary>Days remaining until expiry (negative after expiry).</summary>
    public int? DaysUntilExpiration => ExpiryDate.HasValue ? (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays : null;

    /// <inheritdoc />
    public override string ToString() => $"Identification: id={Id}, type={Type}, number={Number}, verified={IsVerified}";
}
