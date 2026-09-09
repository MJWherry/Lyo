using System.ComponentModel.DataAnnotations;

namespace Lyo.Sms.Postgres.Database;

/// <summary>Database row for an outbound SMS log.</summary>
public class SmsLogEntity
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Recipient number (E.164).</summary>
    [Required]
    [MaxLength(50)]
    public string To { get; set; } = null!;

    /// <summary>Sender number (E.164).</summary>
    [MaxLength(50)]
    public string? From { get; set; }

    /// <summary>Body text.</summary>
    [MaxLength(2000)]
    public string? Body { get; set; }

    /// <summary>MMS media URLs stored as a JSON array.</summary>
    [MaxLength(5000)]
    public string? MediaUrlsJson { get; set; }

    /// <summary>Whether the send succeeded.</summary>
    [Required]
    public bool IsSuccess { get; set; }

    /// <summary>Success note when the send succeeded.</summary>
    [MaxLength(1000)]
    public string? Message { get; set; }

    /// <summary>Error text when the send failed.</summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>Elapsed milliseconds.</summary>
    public long ElapsedTimeMs { get; set; }

    /// <summary>Provider-assigned message id.</summary>
    [MaxLength(200)]
    public string? MessageId { get; set; }

    /// <summary>Provider status.</summary>
    [MaxLength(100)]
    public string? Status { get; set; }

    /// <summary>Error code when the send failed.</summary>
    public int? ErrorCode { get; set; }

    /// <summary>When the message was created.</summary>
    public DateTime? DateCreated { get; set; }

    /// <summary>When the message was sent.</summary>
    public DateTime? DateSent { get; set; }

    /// <summary>When the status last changed.</summary>
    public DateTime? DateUpdated { get; set; }

    /// <summary>When this log row was created.</summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}