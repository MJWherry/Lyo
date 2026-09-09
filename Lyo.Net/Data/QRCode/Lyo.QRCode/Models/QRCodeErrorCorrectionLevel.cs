using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>QR error-correction levels.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum QRCodeErrorCorrectionLevel
{
    /// <summary>Low error correction (about 7% recovery).</summary>
    Low,

    /// <summary>Medium error correction (about 15% recovery).</summary>
    Medium,

    /// <summary>Quartile error correction (about 25% recovery).</summary>
    Quartile,

    /// <summary>High error correction (about 30% recovery).</summary>
    High
}