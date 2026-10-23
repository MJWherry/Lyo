using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>One email attachment. Bytes are sent to the provider and are not persisted in database logs.</summary>
/// <param name="FileName">Attachment file name.</param>
/// <param name="Data">Bytes to send. Not persisted in database logs.</param>
/// <param name="ContentType">Optional MIME type.</param>
/// <param name="MetadataJson">Optional JSON bag stored only on the request. Providers do not put this on the wire.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailAttachment(string FileName, byte[] Data, string? ContentType = null, string? MetadataJson = null)
{
    /// <inheritdoc />
    public override string ToString() => FileName;
}
