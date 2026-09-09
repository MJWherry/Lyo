using Lyo.Exceptions.Models;
using Lyo.QRCode.Models;
using Lyo.QRCode.Payloads;

namespace Lyo.QRCode.Tests;

public class QrPayloadTests
{
    [Fact]
    public void WifiQrPayload_Wpa2_Escapes_Special_Chars()
    {
        var p = new WifiQrPayload("a;b", "p\"d", QrWifiSecurityType.Wpa, true);
        Assert.Equal(@"WIFI:T:WPA;S:a\;b;P:p\""d;H:true;;", p.ToQrString());
    }

    [Fact]
    public void WifiQrPayload_Nopass_No_Password()
    {
        var p = new WifiQrPayload("Guest", "", QrWifiSecurityType.Nopass);
        Assert.Equal("WIFI:T:nopass;S:Guest;;", p.ToQrString());
    }

    [Fact]
    public void WifiQrPayload_Nopass_Rejects_Password() => Assert.Throws<InvalidFormatException>(() => new WifiQrPayload("Guest", "x", QrWifiSecurityType.Nopass).ToQrString());

    [Fact]
    public void WifiQrPayload_Requires_Password_For_Wpa()
    {
        Assert.Throws<ArgumentException>(() => _ = new WifiQrPayload("Net", "", QrWifiSecurityType.Wpa));
        Assert.Throws<InvalidFormatException>(() => new WifiQrPayload("Net", null, QrWifiSecurityType.Wpa).ToQrString());
    }

    [Fact]
    public void WifiQrPayload_Sae()
    {
        var p = new WifiQrPayload("n", "pw", QrWifiSecurityType.Sae);
        Assert.Equal("WIFI:T:SAE;S:n;P:pw;;", p.ToQrString());
    }

    [Fact]
    public void WifiQrPayload_Wpa_Not_Hidden_Omits_H()
    {
        var p = new WifiQrPayload("Net", "secret", QrWifiSecurityType.Wpa);
        Assert.Equal("WIFI:T:WPA;S:Net;P:secret;;", p.ToQrString());
    }

    [Fact]
    public void SmsPayload_Throws_When_Uri_Too_Long()
    {
        var body = new string('x', SmsPayload.MaxSmsQrUriLength);
        var p = new SmsPayload("+1", body);
        Assert.Throws<InvalidFormatException>(p.ToQrString);
    }

    [Fact]
    public void HttpUrlPayload_Force_Https()
    {
        var p = new HttpUrlPayload("http://example.com/path?q=1", true);
        Assert.Equal("https://example.com/path?q=1", p.ToQrString());
    }

    [Fact]
    public void HttpUrlPayload_Rejects_Non_Http() => Assert.Throws<InvalidFormatException>(() => new HttpUrlPayload("ftp://x").ToQrString());

    [Fact]
    public void MailtoPayload_Encodes_Query()
    {
        var p = new MailtoPayload("a@b.co", "Hi & there", "Line1\nLine2");
        var s = p.ToQrString();
        Assert.StartsWith("mailto:", s);
        Assert.Contains("subject=", s);
        Assert.Contains("body=", s);
        Assert.Contains("%26", s);
    }

    [Fact]
    public void TelPayload_Strips_Separators()
    {
        var p = new TelPayload("+1 (555) 123-4567");
        Assert.Equal("tel:+15551234567", p.ToQrString());
    }

    [Fact]
    public void SmsPayload_Sms_Scheme_With_Body()
    {
        var p = new SmsPayload("+15551234567", "hello & world");
        Assert.Equal("sms:+15551234567?body=hello%20%26%20world", p.ToQrString());
    }

    [Fact]
    public void SmsPayload_Smsto_Scheme_Opt_In()
    {
        var p = new SmsPayload("+15551234567", "hi", true);
        Assert.Equal("smsto:+15551234567?body=hi", p.ToQrString());
    }

    [Fact]
    public void GeoPayload_With_Label()
    {
        var p = new GeoPayload(37.78, -122.4, "SF & pier");
        Assert.Equal("geo:37.78,-122.4?q=SF%20%26%20pier", p.ToQrString());
    }

    [Fact]
    public void VCard3Payload_Minimal()
    {
        var p = new VCard3Payload("Jane Doe", "+1 555", "j@ex.com");
        var s = p.ToQrString();
        Assert.Contains("BEGIN:VCARD", s);
        Assert.Contains("VERSION:3.0", s);
        Assert.Contains("FN:Jane Doe", s);
        Assert.Contains("TEL:+1 555", s);
        Assert.Contains("EMAIL:j@ex.com", s);
        Assert.Contains("END:VCARD", s);
    }

    [Fact]
    public void VCard3Payload_Escapes_Fn()
    {
        var p = new VCard3Payload("Doe; Jane");
        Assert.Contains(@"FN:Doe\; Jane", p.ToQrString());
    }

    [Fact]
    public void MeCardPayload_Terminator()
    {
        var p = new MeCardPayload("A", "1", "e@e");
        Assert.Equal("MECARD:N:A;TEL:1;EMAIL:e@e;;", p.ToQrString());
    }

    [Fact]
    public void WhatsAppUrlPayload_Digits_Only()
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
    public void TelegramUrlPayload_Invalid() => Assert.Throws<InvalidFormatException>(() => new TelegramUrlPayload("1bad").ToQrString());

    [Fact]
    public void SignalUrlPayload_Requires_Plus()
    {
        Assert.Throws<InvalidFormatException>(() => new SignalUrlPayload("15551234567").ToQrString());
        var ok = new SignalUrlPayload("+15551234567");
        Assert.Equal("sgnl://signal.me/#p/+15551234567", ok.ToQrString());
    }

    [Fact]
    public void QRCodeBuilder_WithPayload_Sets_Data()
    {
        var (data, _) = QRCodeBuilder.New().WithPayload(new WifiQrPayload("x", "y", QrWifiSecurityType.Wpa)).WithFormat(QRCodeFormat.Png).Build();
        Assert.Equal("WIFI:T:WPA;S:x;P:y;;", data);
    }

    [Fact]
    public void QRCodeBuilder_WithPayload_After_With_Data_Last_Wins()
    {
        var (data, _) = QRCodeBuilder.New().WithData("first").WithPayload(new PlainTextQrPayload("second")).Build();
        Assert.Equal("second", data);
    }
}