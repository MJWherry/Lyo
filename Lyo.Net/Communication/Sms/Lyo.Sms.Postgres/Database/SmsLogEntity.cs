using System.ComponentModel.DataAnnotations;

namespace Lyo.Sms.Postgres.Database;

/// <summary>Entity representing an outbound SMS message log in the database.</summary>
public class SmsLogEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the recipient phone number (E.164 format).</summary>
    [Required]
    [MaxLength(50)]
    public string To { get; set; } = null!;

    /// <summary>Gets or sets the sender phone number (E.164 format).</summary>
    [MaxLength(50)]
    public string? From { get; set; }

    /// <summary>Gets or sets the message body text.</summary>
    [MaxLength(2000)]
    public string? Body { get; set; }

    /// <summary>Gets or sets the media URLs as JSON array (for MMS messages).</summary>
    [MaxLength(5000)]
    public string? MediaUrlsJson { get; set; }

    /// <summary>Gets or sets whether the message was sent successfully.</summary>
    [Required]
    public bool IsSuccess { get; set; }

    /// <summary>Gets or sets the success message (if successful).</summary>
    [MaxLength(1000)]
    public string? Message { get; set; }

    /// <summary>Gets or sets the error message (if failed).</summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the elapsed time in milliseconds.</summary>
    public long ElapsedTimeMs { get; set; }

    /// <summary>Gets or sets the message ID from the SMS provider.</summary>
    [MaxLength(200)]
    public string? MessageId { get; set; }

    /// <summary>Gets or sets the message status.</summary>
    [MaxLength(100)]
    public string? Status { get; set; }

    /// <summary>Gets or sets the error code (if failed).</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Gets or sets the date and time the message was created.</summary>
    public DateTime? DateCreated { get; set; }

    /// <summary>Gets or sets the date and time the message was sent.</summary>
    public DateTime? DateSent { get; set; }

    /// <summary>Gets or sets the date and time the message status was last updated.</summary>
    public DateTime? DateUpdated { get; set; }

    /// <summary>Gets or sets the timestamp when this log entry was created.</summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}