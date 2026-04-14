namespace Lyo.QRCode.Models;

/// <summary>QR code error correction levels.</summary>
public enum QRCodeErrorCorrectionLevel
{
    /// <summary>Low error correction (~7% recovery).</summary>
    Low,

    /// <summary>Medium error correction (~15% recovery).</summary>
    Medium,

    /// <summary>Quartile error correction (~25% recovery).</summary>
    Quartile,

    /// <summary>High error correction (~30% recovery).</summary>
    High
}