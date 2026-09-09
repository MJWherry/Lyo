using System.Diagnostics;
using Lyo.Common.Core.Extensions;

namespace Lyo.QRCode.Models;

/// <summary>One QR generation request in a batch.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeRequest
{
    /// <summary>Payload to encode in the QR code.</summary>
    public string Data { get; set; } = null!;

    /// <summary>Optional generation options.</summary>
    public QRCodeOptions? Options { get; set; }

    /// <summary>Optional id for this request. Useful in batches.</summary>
    public string? Id { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"Id: {Id ?? "(none)"}, Data: {Data[..Math.Min(Data.Length, 50)]}{Data.Truncated(50)}, Options: {Options?.ToString() ?? "(default)"}";
}