using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Outcome of a batch QR generation run.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record BatchQRCodeResult(IReadOnlyList<QRCodeResult> Results, TimeSpan ElapsedTime)
{
    /// <summary>How many QR generations succeeded.</summary>
    public int SuccessCount => Results.Count(r => r.IsSuccess);

    /// <summary>How many QR generations failed.</summary>
    public int FailureCount => Results.Count(r => !r.IsSuccess);

    /// <summary>Total QR generations in the batch.</summary>
    public int TotalCount => Results.Count;

    public override string ToString() => $"Batch QR Code: {SuccessCount}/{TotalCount} successful in {ElapsedTime:g}";
}