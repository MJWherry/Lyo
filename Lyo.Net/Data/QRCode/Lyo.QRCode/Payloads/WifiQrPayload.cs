using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>Wi‑Fi network join string (<c>WIFI:T:…;S:…;P:…;;</c>). Omits <c>H:</c> when the SSID is not hidden (better Android/iOS compatibility than <c>H:false</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WifiQrPayload : IQrPayload
{
    /// <summary>Initializes a Wi‑Fi QR payload.</summary>
    /// <param name="ssid">Network name (required).</param>
    /// <param name="password">Pre-shared key (empty for open networks when <paramref name="security" /> is <see cref="QrWifiSecurityType.Nopass" />).</param>
    /// <param name="security">Security type for the <c>T:</c> field.</param>
    /// <param name="hidden">Whether the SSID is hidden.</param>
    public WifiQrPayload(string ssid, string password, QrWifiSecurityType security, bool hidden = false)
    {
        ArgumentHelpers.ThrowIfNull(ssid);
        Ssid = ssid;
        Password = password ?? string.Empty;
        Security = security;
        Hidden = hidden;
    }

    /// <summary>Network SSID.</summary>
    public string Ssid { get; }

    /// <summary>Network password (not used when <see cref="Security" /> is <see cref="QrWifiSecurityType.Nopass" />).</summary>
    public string Password { get; }

    /// <summary>Security type.</summary>
    public QrWifiSecurityType Security { get; }

    /// <summary>Whether the SSID is not broadcast.</summary>
    public bool Hidden { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"WifiQrPayload ssid={Ssid}, {Security}, hidden={Hidden}, passwordLength={Password.Length}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(Ssid), "SSID cannot be empty.", nameof(Ssid));

        if (Security != QrWifiSecurityType.Nopass && string.IsNullOrEmpty(Password))
            throw new InvalidFormatException("Password is required for this security type.", nameof(Password), null, "non-empty PSK");

        if (Security == QrWifiSecurityType.Nopass && !string.IsNullOrEmpty(Password))
            throw new InvalidFormatException("Open networks (nopass) must not include a password.", nameof(Password), Password, "empty password for nopass");

        var t = Security switch {
            QrWifiSecurityType.Nopass => "nopass",
            QrWifiSecurityType.Wpa => "WPA",
            QrWifiSecurityType.Wep => "WEP",
            QrWifiSecurityType.Sae => "SAE",
            var x => throw new ArgumentOutOfRangeException(nameof(Security), x, null)
        };

        var sb = new StringBuilder(32 + Ssid.Length + Password.Length);
        sb.Append("WIFI:T:").Append(t).Append(';');
        sb.Append("S:").Append(QrPayloadWifiEscape.EscapeFieldValue(Ssid)).Append(';');

        if (Security != QrWifiSecurityType.Nopass)
            sb.Append("P:").Append(QrPayloadWifiEscape.EscapeFieldValue(Password)).Append(';');

        if (Hidden)
            sb.Append("H:true;");

        sb.Append(';');
        return sb.ToString();
    }
}
