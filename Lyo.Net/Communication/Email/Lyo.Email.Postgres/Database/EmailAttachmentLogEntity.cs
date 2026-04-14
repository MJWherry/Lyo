using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Email.Postgres.Database;

/// <summary>Entity representing an attachment reference for a sent email. Does not store file data.</summary>
public class EmailAttachmentLogEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the email log this attachment belongs to.</summary>
    [Required]
    public Guid EmailLogId { get; set; }

    /// <summary>Navigation property to the email log.</summary>
    [ForeignKey(nameof(EmailLogId))]
    public EmailLogEntity EmailLog { get; set; } = null!;

    /// <summary>Gets or sets the attachment file name.</summary>
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = null!;

    /// <summary>Gets or sets the optional ID for correlation with external file storage (e.g. your file service). Not a foreign key.</summary>
    [MaxLength(200)]
    public string? FileStorageId { get; set; }

    /// <summary>Gets or sets the optional template ID used for formatting.</summary>
    public Guid? TemplateId { get; set; }

    /// <summary>Gets or sets the optional content type (e.g. application/pdf).</summary>
    [MaxLength(100)]
    public string? ContentType { get; set; }

    /// <summary>Gets or sets optional metadata as JSON for extensibility.</summary>
    [MaxLength(2000)]
    public string? MetadataJson { get; set; }

    /// <summary>Gets or sets the sort order of the attachment within the email.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the timestamp when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets the timestamp when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}