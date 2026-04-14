using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Represents the result of a batch QR code generation operation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record BatchQRCodeResult(IReadOnlyList<QRCodeResult> Results, TimeSpan ElapsedTime)
{
    /// <summary>Gets the number of successful QR code generations.</summary>
    public int SuccessCount => Results.Count(r => r.IsSuccess);

    /// <summary>Gets the number of failed QR code generations.</summary>
    public int FailureCount => Results.Count(r => !r.IsSuccess);

    /// <summary>Gets the total number of QR code generations.</summary>
    public int TotalCount => Results.Count;

    public override string ToString() => $"Batch QR Code: {SuccessCount}/{TotalCount} successful in {ElapsedTime:g}";
}