using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>One email attachment. Bytes are used for SMTP send and are not persisted in database logs.</summary>
/// <param name="FileName">Attachment file name.</param>
/// <param name="Data">Bytes to send. Not persisted in database logs.</param>
/// <param name="ContentType">Optional MIME type.</param>
/// <param name="MetadataJson">Optional JSON bag (template params, tags, and similar).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailAttachment(string FileName, byte[] Data, string? ContentType = null, string? MetadataJson = null)
{
    /// <inheritdoc />
    public override string ToString() => FileName;
}