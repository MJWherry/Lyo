using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Supported barcode symbologies.</summary>
[DebuggerDisplay("BarcodeSymbology.{ToString()}")]
public enum BarcodeSymbology
{
    /// <summary>ISO/IEC 15417 Code 128 (subset B: ASCII 32–127).</summary>
    Code128
}