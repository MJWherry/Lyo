using System.Diagnostics;

namespace Lyo.QRCode.Payloads;

/// <summary>Maps to the <c>T:</c> field in a <c>WIFI:</c> QR string (de‑facto Android/iOS grammar).</summary>
[DebuggerDisplay("QrWifiSecurityType.{ToString()}")]
public enum QrWifiSecurityType
{
    /// <summary>Open network (<c>T:nopass</c>).</summary>
    Nopass,

    /// <summary>WPA or WPA2-Personal (<c>T:WPA</c>).</summary>
    Wpa,

    /// <summary>WEP (<c>T:WEP</c>).</summary>
    Wep,

    /// <summary>WPA3-Personal / SAE (<c>T:SAE</c>).</summary>
    Sae
}
