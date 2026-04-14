using System.ComponentModel.DataAnnotations;

namespace Lyo.Email.Postgres.Database;

/// <summary>Entity representing a sent email log in the database.</summary>
public class EmailLogEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the sender email address.</summary>
    [MaxLength(500)]
    public string? FromAddress { get; set; }

    /// <summary>Gets or sets the sender display name.</summary>
    [MaxLength(500)]
    public string? FromName { get; set; }

    /// <summary>Gets or sets the To addresses as JSON array.</summary>
    [MaxLength(4000)]
    public string? ToAddressesJson { get; set; }

    /// <summary>Gets or sets the Cc addresses as JSON array.</summary>
    [MaxLength(4000)]
    public string? CcAddressesJson { get; set; }

    /// <summary>Gets or sets the Bcc addresses as JSON array.</summary>
    [MaxLength(4000)]
    public string? BccAddressesJson { get; set; }

    /// <summary>Gets or sets the email subject.</summary>
    [MaxLength(1000)]
    public string? Subject { get; set; }

    /// <summary>Gets or sets whether the email was sent successfully.</summary>
    [Required]
    public bool IsSuccess { get; set; }

    /// <summary>Gets or sets the success message (e.g. SMTP response).</summary>
    [MaxLength(1000)]
    public string? Message { get; set; }

    /// <summary>Gets or sets the error message (if failed).</summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the message ID from the email provider.</summary>
    [MaxLength(200)]
    public string? MessageId { get; set; }

    /// <summary>Gets or sets the timestamp when this log entry was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the timestamp when this log entry was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}