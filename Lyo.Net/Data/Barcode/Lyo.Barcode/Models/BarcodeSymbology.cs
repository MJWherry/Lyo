using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Supported barcode symbologies.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum BarcodeSymbology
{
    /// <summary>ISO/IEC 15417 Code 128 (subset B: ASCII 32–127).</summary>
    Code128
}