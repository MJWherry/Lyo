using System.ComponentModel.DataAnnotations;
using Lyo.ContactUs.Models;

namespace Lyo.ContactUs.Postgres.Database;

/// <summary>Entity representing a contact form submission in the database.</summary>
public sealed class ContactSubmissionEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the sender's name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>Gets or sets the sender's email address.</summary>
    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = null!;

    /// <summary>Gets or sets the subject of the message.</summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = null!;

    /// <summary>Gets or sets the message body.</summary>
    [Required]
    [MaxLength(10000)]
    public string Message { get; set; } = null!;

    /// <summary>Gets or sets the optional phone number.</summary>
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>Gets or sets the optional company name.</summary>
    [MaxLength(200)]
    public string? Company { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Creates entity from a contact form request.</summary>
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