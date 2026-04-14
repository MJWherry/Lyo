using System.ComponentModel.DataAnnotations;

namespace Lyo.Sms.Twilio.Postgres.Database;

/// <summary>Entity representing a Twilio-specific SMS message log.</summary>
public class TwilioSmsLogEntity
{
    /// <summary>Gets or sets the Twilio message SID (primary key, 34 chars).</summary>
    [Key]
    [MaxLength(34)]
    public string Id { get; set; } = null!;

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
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the timestamp when this log entry was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets or sets the number of SMS segments (for long messages).</summary>
    public int? NumSegments { get; set; }

    /// <summary>Gets or sets the Twilio account SID used for the operation.</summary>
    [MaxLength(100)]
    public string? AccountSid { get; set; }

    /// <summary>Gets or sets the price charged for the message.</summary>
    public decimal? Price { get; set; }

    /// <summary>Gets or sets the currency unit for the price (e.g., "USD").</summary>
    [MaxLength(10)]
    public string? PriceUnit { get; set; }

    /// <summary>Gets or sets the direction of the message (inbound or outbound).</summary>
    public MessageDirection Direction { get; set; } = MessageDirection.Outbound;
}