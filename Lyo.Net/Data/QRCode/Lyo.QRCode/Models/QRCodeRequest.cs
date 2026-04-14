using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Represents a QR code generation request for batch operations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeRequest
{
    /// <summary>Gets or sets the data to encode in the QR code.</summary>
    public string Data { get; set; } = null!;

    /// <summary>Gets or sets the optional QR code generation options.</summary>
    public QRCodeOptions? Options { get; set; }

    /// <summary>Gets or sets an optional identifier for this request (useful for batch operations).</summary>
    public string? Id { get; set; }

    public override string ToString()
        => $"Id: {Id ?? "(none)"}, Data: {Data?.Substring(0, Math.Min(Data?.Length ?? 0, 50))}{(Data?.Length > 50 ? "..." : "")}, Options: {Options?.ToString() ?? "(default)"}";
}