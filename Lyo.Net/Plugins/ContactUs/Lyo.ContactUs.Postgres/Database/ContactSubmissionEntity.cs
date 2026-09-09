using System.ComponentModel.DataAnnotations;
using Lyo.ContactUs.Models;

namespace Lyo.ContactUs.Postgres.Database;

/// <summary>Row that stores a contact form submission in the database.</summary>
public sealed class ContactSubmissionEntity
{
    /// <summary>Holds the unique identifier.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Sender's name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>Value of the sender's email address.</summary>
    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = null!;

    /// <summary>Value of the subject of the message.</summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = null!;

    /// <summary>Message body for this record.</summary>
    [Required]
    [MaxLength(10000)]
    public string Message { get; set; } = null!;

    /// <summary>Holds the optional phone number.</summary>
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>Holds the optional company name.</summary>
    [MaxLength(200)]
    public string? Company { get; set; }

    /// <summary>Creation timestamp for this record.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Builds an entity from a contact form request.</summary>
    public static ContactSubmissionEntity FromRequest(Guid id, ContactUsRequest request)
        => new() {
            Id = id,
            Name = request.Name,
            Email = request.Email,
            Subject = request.Subject,
            Message = request.Message,
            Phone = request.Phone,
            Company = request.Company,
            CreatedTimestamp = DateTime.UtcNow
        };
}