using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Email.Postgres.Database;

/// <summary>Attachment metadata for a logged email. File bytes are not stored.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class EmailAttachmentLogEntity
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; } = LyoGuid.CreateV7();

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

    /// <summary>File name, content type, and size. The parent log is omitted.</summary>
    public override string ToString() => $"{FileName} {ContentType} SizeBytes={SizeBytes}";
}