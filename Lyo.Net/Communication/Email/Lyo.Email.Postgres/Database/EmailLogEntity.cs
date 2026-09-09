using System.ComponentModel.DataAnnotations;

namespace Lyo.Email.Postgres.Database;

/// <summary>Database row for an outbound or inbound email log. HTML content is not stored here; hosts write their own file and record the path.</summary>
public class EmailLogEntity
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Whether this row is outbound (sent) or inbound (received). Defaults to outbound.</summary>
    public EmailDirection Direction { get; set; } = EmailDirection.Outbound;

    /// <summary>Sender address.</summary>
    [MaxLength(500)]
    public string? FromAddress { get; set; }

    /// <summary>Sender display name.</summary>
    [MaxLength(500)]
    public string? FromName { get; set; }

    /// <summary>To addresses stored as a JSON array.</summary>
    public string? ToAddressesJson { get; set; }

    /// <summary>CC addresses stored as a JSON array.</summary>
    public string? CcAddressesJson { get; set; }

    /// <summary>BCC addresses stored as a JSON array.</summary>
    public string? BccAddressesJson { get; set; }

    /// <summary>Subject line.</summary>
    [MaxLength(1000)]
    public string? Subject { get; set; }

    /// <summary>Plain-text body as sent or received.</summary>
    public string? TextBody { get; set; }

    /// <summary>File name of the host-owned HTML body file, when one was written (for example <c>welcome.html</c>).</summary>
    [MaxLength(500)]
    public string? HtmlFileName { get; set; }

    /// <summary>Host-owned path or URI of the HTML body file. Not a FileStorage id.</summary>
    [MaxLength(2000)]
    public string? HtmlFilePath { get; set; }

    /// <summary>Whether the send or fetch succeeded.</summary>
    [Required]
    public bool IsSuccess { get; set; }

    /// <summary>Success note (for example an SMTP reply or fetch note).</summary>
    [MaxLength(1000)]
    public string? Message { get; set; }

    /// <summary>Error text when the send or fetch failed.</summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>MIME Message-ID.</summary>
    [MaxLength(200)]
    public string? MessageId { get; set; }

    /// <summary>When the message left SMTP, or the message Date header on inbound.</summary>
    public DateTime? SentTimestamp { get; set; }

    /// <summary>When inbound mail was fetched. Null on outbound rows.</summary>
    public DateTime? ReceivedTimestamp { get; set; }

    /// <summary>When this log row was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>When this log row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Attachment metadata rows for this message. Bytes are not stored.</summary>
    public ICollection<EmailAttachmentLogEntity> Attachments { get; set; } = [];
}
