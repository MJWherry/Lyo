using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>Represents an email attachment with optional metadata for logging and correlation with external storage.</summary>
/// <param name="FileName">The file name for the attachment.</param>
/// <param name="Data">The attachment data. Not stored in database logs; used only for sending.</param>
/// <param name="FileStorageId">Optional ID to correlate with an external file storage service (e.g. for audit trail). Not a foreign key.</param>
/// <param name="TemplateId">Optional template ID used for formatting the attachment or email.</param>
/// <param name="ContentType">Optional MIME content type.</param>
/// <param name="MetadataJson">Optional metadata as JSON for extensibility (e.g. template params, tags).</param>
[DebuggerDisplay("{FileName,nq}")]
public sealed record EmailAttachment(string FileName, byte[] Data, string? FileStorageId = null, Guid? TemplateId = null, string? ContentType = null, string? MetadataJson = null);