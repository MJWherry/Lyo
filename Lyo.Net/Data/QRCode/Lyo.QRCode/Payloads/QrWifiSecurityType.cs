using System.Diagnostics;

namespace Lyo.QRCode.Payloads;

/// <summary>Maps to the <c>T:</c> field in a <c>WIFI:</c> QR string (de facto Android/iOS grammar).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum QrWifiSecurityType
{
    /// <summary>Open network, written as <c>T:nopass</c>.</summary>
    Nopass,

    /// <summary>WPA or WPA2-Personal, written as <c>T:WPA</c>.</summary>
    Wpa,

    /// <summary>WEP, written as <c>T:WEP</c>.</summary>
    Wep,

    /// <summary>WPA3-Personal / SAE, written as <c>T:SAE</c>.</summary>
    Sae
}