using System.Diagnostics;

namespace Lyo.QRCode.Payloads;

/// <summary>Discriminator for structured QR payloads, used by UI presets and factory helpers.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public enum QrPayloadKind
{
    /// <summary>Arbitrary text via <see cref="PlainTextQrPayload" />.</summary>
    PlainText = 0,

    /// <summary>HTTP(S) URL via <see cref="HttpUrlPayload" />.</summary>
    Url,

    /// <summary>Wi‑Fi join string (<c>WIFI:</c>) via <see cref="WifiQrPayload" />.</summary>
    Wifi,

    /// <summary><c>mailto:</c> URI via <see cref="MailtoPayload" />.</summary>
    Mailto,

    /// <summary><c>tel:</c> URI via <see cref="TelPayload" />.</summary>
    Tel,

    /// <summary><c>sms:</c> or <c>smsto:</c> URI via <see cref="SmsPayload" />.</summary>
    Sms,

    /// <summary><c>geo:</c> URI via <see cref="GeoPayload" />.</summary>
    Geo,

    /// <summary>vCard 3.0 text via <see cref="VCard3Payload" />.</summary>
    VCard3,

    /// <summary>meCard string via <see cref="MeCardPayload" />.</summary>
    MeCard,

    /// <summary>WhatsApp chat deep link (<c>https://wa.me/</c>) via <see cref="WhatsAppUrlPayload" />.</summary>
    WhatsApp,

    /// <summary>Telegram deep link (<c>https://t.me/</c>) via <see cref="TelegramUrlPayload" />.</summary>
    Telegram,

    /// <summary>Signal deep link (<c>sgnl://</c>) via <see cref="SignalUrlPayload" />.</summary>
    Signal
}