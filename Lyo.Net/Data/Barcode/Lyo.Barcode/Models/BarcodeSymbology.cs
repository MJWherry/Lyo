using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Barcode families this library can encode.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum BarcodeSymbology
{
    /// <summary>ISO/IEC 15417 Code 128, subset B (ASCII 32 through 127).</summary>
    Code128
}