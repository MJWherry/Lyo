using Lyo.QRCode.Models;

namespace Lyo.QRCode;

/// <summary>
/// Picks a high enough error-correction level when a center logo obscures part of the symbol.
/// Larger logos need stronger ECC than the default; otherwise the composite looks valid but may not scan.
/// </summary>
internal static class QRCodeIconEccHelper
{
    /// <summary>Returns the level used to encode the QR matrix (never lower than <paramref name="requested" />).</summary>
    public static QRCodeErrorCorrectionLevel GetEffectiveLevel(QRCodeErrorCorrectionLevel requested, QRCodeIconOptions? icon)
    {
        if (icon == null)
            return requested;

        var pct = QRCodeIconOptions.ClampIconSizePercent(icon.IconSizePercent);
        var minForLogo = MinimumLevelForIconPercent(pct);
        return Max(requested, minForLogo);
    }

    /// <summary>Minimum ECC so typical payloads remain scannable for a logo covering <paramref name="iconPercent" />% of the image side.</summary>
    private static QRCodeErrorCorrectionLevel MinimumLevelForIconPercent(int iconPercent)
    {
        if (iconPercent >= 23)
            return QRCodeErrorCorrectionLevel.High;
        if (iconPercent >= 16)
            return QRCodeErrorCorrectionLevel.Quartile;
        return QRCodeErrorCorrectionLevel.Medium;
    }

    private static QRCodeErrorCorrectionLevel Max(QRCodeErrorCorrectionLevel a, QRCodeErrorCorrectionLevel b)
        => Order(a) >= Order(b) ? a : b;

    private static int Order(QRCodeErrorCorrectionLevel e) => e switch {
        QRCodeErrorCorrectionLevel.Low => 0,
        QRCodeErrorCorrectionLevel.Medium => 1,
        QRCodeErrorCorrectionLevel.Quartile => 2,
        QRCodeErrorCorrectionLevel.High => 3,
        _ => 1
    };
}
