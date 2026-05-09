using Lyo.Exceptions.Models;
using Lyo.QRCode.Models;
using Lyo.QRCode.Payloads;

namespace Lyo.QRCode.Tests;

public class QrPayloadTests
{
    [Fact]
    public void WifiQrPayload_Wpa2_escapes_special_chars()
    {
        var p = new WifiQrPayload("a;b", "p\"d", QrWifiSecurityType.Wpa, hidden: true);
        Assert.Equal(@"WIFI:T:WPA;S:a\;b;P:p\""d;H:true;;", p.ToQrString());
    }

    [Fact]
    public void WifiQrPayload_nopass_no_password()
    {
        var p = new WifiQrPayload("Guest", "", QrWifiSecurityType.Nopass);
        Assert.Equal("WIFI:T:nopass;S:Guest;;", p.ToQrString());
    }

    [Fact]
    public void WifiQrPayload_nopass_rejects_password()
    {
        Assert.Throws<InvalidFormatException>(() => new WifiQrPayload("Guest", "x", QrWifiSecurityType.Nopass).ToQrString());
    }

    [Fact]
    public void WifiQrPayload_requires_password_for_wpa()
    {
        Assert.Throws<ArgumentException>(() => new WifiQrPayload("Net", "", QrWifiSecurityType.Wpa).ToQrString());
    }

    [Fact]
    public void WifiQrPayload_Sae()
    {
        var p = new WifiQrPayload("n", "pw", QrWifiSecurityType.Sae);
        Assert.Equal("WIFI:T:SAE;S:n;P:pw;;", p.ToQrString());
    }

    [Fact]
    public void WifiQrPayload_wpa_not_hidden_omits_H()
    {
        var p = new WifiQrPayload("Net", "secret", QrWifiSecurityType.Wpa, hidden: false);
        Assert.Equal("WIFI:T:WPA;S:Net;P:secret;;", p.ToQrString());
    }

    [Fact]
    public void SmsPayload_throws_when_uri_too_long()
    {
        var body = new string('x', SmsPayload.MaxSmsQrUriLength);
        var p = new SmsPayload("+1", body);
        Assert.Throws<InvalidFormatException>(() => p.ToQrString());
    }

    [Fact]
    public void HttpUrlPayload_force_https()
    {
        var p = new HttpUrlPayload("http://example.com/path?q=1", forceHttps: true);
        Assert.Equal("https://example.com/path?q=1", p.ToQrString());
    }

    [Fact]
    public void HttpUrlPayload_rejects_non_http()
    {
        Assert.Throws<InvalidFormatException>(() => new HttpUrlPayload("ftp://x").ToQrString());
    }

    [Fact]
    public void MailtoPayload_encodes_query()
    {
        var p = new MailtoPayload("a@b.co", subject: "Hi & there", body: "Line1\nLine2");
        var s = p.ToQrString();
        Assert.StartsWith("mailto:", s);
        Assert.Contains("subject=", s);
        Assert.Contains("body=", s);
        Assert.Contains("%26", s);
    }

    [Fact]
    public void TelPayload_strips_separators()
    {
        var p = new TelPayload("+1 (555) 123-4567");
        Assert.Equal("tel:+15551234567", p.ToQrString());
    }

    [Fact]
    public void SmsPayload_sms_scheme_with_body()
    {
        var p = new SmsPayload("+15551234567", "hello & world");
        Assert.Equal("sms:+15551234567?body=hello%20%26%20world", p.ToQrString());
    }

    [Fact]
    public void SmsPayload_smsto_scheme_opt_in()
    {
        var p = new SmsPayload("+15551234567", "hi", useSmstoScheme: true);
        Assert.Equal("smsto:+15551234567?body=hi", p.ToQrString());
    }

    [Fact]
    public void GeoPayload_with_label()
    {
        var p = new GeoPayload(37.78, -122.4, queryLabel: "SF & pier");
        Assert.Equal("geo:37.78,-122.4?q=SF%20%26%20pier", p.ToQrString());
    }

    [Fact]
    public void VCard3Payload_minimal()
    {
        var p = new VCard3Payload("Jane Doe", telephone: "+1 555", email: "j@ex.com");
        var s = p.ToQrString();
        Assert.Contains("BEGIN:VCARD", s);
        Assert.Contains("VERSION:3.0", s);
        Assert.Contains("FN:Jane Doe", s);
        Assert.Contains("TEL:+1 555", s);
        Assert.Contains("EMAIL:j@ex.com", s);
        Assert.Contains("END:VCARD", s);
    }

    [Fact]
    public void VCard3Payload_escapes_fn()
    {
        var p = new VCard3Payload("Doe; Jane");
        Assert.Contains(@"FN:Doe\; Jane", p.ToQrString());
    }

    [Fact]
    public void MeCardPayload_terminator()
    {
        var p = new MeCardPayload("A", telephone: "1", email: "e@e");
        Assert.Equal("MECARD:N:A;TEL:1;EMAIL:e@e;;", p.ToQrString());
    }

    [Fact]
    public void WhatsAppUrlPayload_digits_only()
    {
        var p = new WhatsAppUrlPayload("+1 (555) 111-2222");
        Assert.Equal("https://wa.me/15551112222", p.ToQrString());
    }

    [Fact]
    public void TelegramUrlPayload()
    {
        var p = new TelegramUrlPayload("@MyBot");
        Assert.Equal("https://t.me/MyBot", p.ToQrString());
    }

    [Fact]
    public void TelegramUrlPayload_invalid()
    {
        Assert.Throws<InvalidFormatException>(() => new TelegramUrlPayload("1bad").ToQrString());
    }

    [Fact]
    public void SignalUrlPayload_requires_plus()
    {
        Assert.Throws<InvalidFormatException>(() => new SignalUrlPayload("15551234567").ToQrString());
        var ok = new SignalUrlPayload("+15551234567");
        Assert.Equal("sgnl://signal.me/#p/+15551234567", ok.ToQrString());
    }

    [Fact]
    public void QRCodeBuilder_WithPayload_sets_data()
    {
        var (data, _) = QRCodeBuilder.New()
            .WithPayload(new WifiQrPayload("x", "y", QrWifiSecurityType.Wpa))
            .WithFormat(QRCodeFormat.Png)
            .Build();

        Assert.Equal("WIFI:T:WPA;S:x;P:y;H:false;;", data);
    }

    [Fact]
    public void QRCodeBuilder_WithPayload_after_with_data_last_wins()
    {
        var (data, _) = QRCodeBuilder.New()
            .WithData("first")
            .WithPayload(new PlainTextQrPayload("second"))
            .Build();

        Assert.Equal("second", data);
    }
}
