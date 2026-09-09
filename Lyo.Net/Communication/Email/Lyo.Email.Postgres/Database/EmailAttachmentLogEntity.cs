using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Email.Postgres.Database;

/// <summary>Attachment metadata for a logged email. File bytes are not stored.</summary>
public class EmailAttachmentLogEntity
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Owning email-log id.</summary>
    [Required]
    public Guid EmailLogId { get; set; }

    /// <summary>Navigation to the parent email log.</summary>
    [ForeignKey(nameof(EmailLogId))]
    public EmailLogEntity EmailLog { get; set; } = null!;

    /// <summary>Attachment file name.</summary>
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = null!;

    /// <summary>Optional MIME type (for example application/pdf).</summary>
    [MaxLength(100)]
    public string? ContentType { get; set; }

    /// <summary>Byte length of the attachment that was sent or received. The bytes themselves are not stored.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Optional JSON metadata bag.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Order of this attachment on the message.</summary>
    public int SortOrder { get; set; }

    /// <summary>When this row was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When this row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
