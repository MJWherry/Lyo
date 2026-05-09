using System.Diagnostics;

namespace Lyo.QRCode.Payloads;

/// <summary>Discriminator for structured QR payloads (UI presets and factory helpers).</summary>
[DebuggerDisplay("QrPayloadKind.{ToString()}")]
public enum QrPayloadKind
{
    /// <summary>Arbitrary text; <see cref="PlainTextQrPayload" />.</summary>
    PlainText = 0,

    /// <summary>HTTP(S) URL; <see cref="HttpUrlPayload" />.</summary>
    Url,

    /// <summary>Wi‑Fi join string (<c>WIFI:</c>); <see cref="WifiQrPayload" />.</summary>
    Wifi,

    /// <summary><c>mailto:</c> URI; <see cref="MailtoPayload" />.</summary>
    Mailto,

    /// <summary><c>tel:</c> URI; <see cref="TelPayload" />.</summary>
    Tel,

    /// <summary><c>sms:</c> or <c>smsto:</c> URI; <see cref="SmsPayload" />.</summary>
    Sms,

    /// <summary><c>geo:</c> URI; <see cref="GeoPayload" />.</summary>
    Geo,

    /// <summary>vCard 3.0 text; <see cref="VCard3Payload" />.</summary>
    VCard3,

    /// <summary>meCard string; <see cref="MeCardPayload" />.</summary>
    MeCard,

    /// <summary>WhatsApp chat deep link (<c>https://wa.me/</c>); <see cref="WhatsAppUrlPayload" />.</summary>
    WhatsApp,

    /// <summary>Telegram deep link (<c>https://t.me/</c>); <see cref="TelegramUrlPayload" />.</summary>
    Telegram,

    /// <summary>Signal deep link (<c>sgnl://</c>); <see cref="SignalUrlPayload" />.</summary>
    Signal
}
