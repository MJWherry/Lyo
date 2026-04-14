using System.ComponentModel.DataAnnotations;

namespace Lyo.ContactUs.Models;

/// <summary>Request model for a contact form submission.</summary>
public sealed record ContactUsRequest
{
    /// <summary>Gets or sets the sender's name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    /// <summary>Gets or sets the sender's email address.</summary>
    [Required]
    [MaxLength(320)]
    [EmailAddress]
    public string Email { get; init; } = null!;

    /// <summary>Gets or sets the subject of the message.</summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; init; } = null!;

    /// <summary>Gets or sets the message body.</summary>
    [Required]
    [MaxLength(10000)]
    public string Message { get; init; } = null!;

    /// <summary>Gets or sets the optional phone number.</summary>
    [MaxLength(50)]
    public string? Phone { get; init; }

    /// <summary>Gets or sets the optional company name.</summary>
    [MaxLength(200)]
    public string? Company { get; init; }
}