using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.ContactUs.Models;

/// <summary>Request payload for a contact form submission.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ContactUsRequest
{
    /// <summary>Sender and subject. The message body is omitted.</summary>
    public override string ToString() => $"{Name} <{Email}> Subject={Subject} MessageLength={Message?.Length ?? 0}";

    /// <summary>Sender's name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    /// <summary>Value of the sender's email address.</summary>
    [Required]
    [MaxLength(320)]
    [EmailAddress]
    public string Email { get; init; } = null!;

    /// <summary>Value of the subject of the message.</summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; init; } = null!;

    /// <summary>Message body for this record.</summary>
    [Required]
    [MaxLength(10000)]
    public string Message { get; init; } = null!;

    /// <summary>Holds the optional phone number.</summary>
    [MaxLength(50)]
    public string? Phone { get; init; }

    /// <summary>Holds the optional company name.</summary>
    [MaxLength(200)]
    public string? Company { get; init; }
}