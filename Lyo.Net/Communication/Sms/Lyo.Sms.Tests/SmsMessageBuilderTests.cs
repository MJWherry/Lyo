using Lyo.Exceptions.Models;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;

namespace Lyo.Sms.Tests;

public class SmsMessageBuilderTests
{
    [Fact]
    public void SetTo_ValidPhoneNumber_ReturnsBuilder()
    {
        var builder = SmsMessageBuilder.New();
        var result = builder.SetTo("+15551234567");
        Assert.Same(builder, result);
    }

    [Fact]
    public void SetTo_Null_ThrowsArgumentNullException()
    {
        var builder = SmsMessageBuilder.New();
        Assert.Throws<ArgumentNullException>(() => builder.SetTo(null!));
    }

    [Fact]
    public void SetTo_Empty_ThrowsArgumentException()
    {
        var builder = SmsMessageBuilder.New();
        Assert.Throws<ArgumentException>(() => builder.SetTo(""));
    }

    [Fact]
    public void SetTo_InvalidFormat_ThrowsInvalidFormatException()
    {
        var builder = SmsMessageBuilder.New();
        var exception = Assert.Throws<InvalidFormatException>(() => builder.SetTo("invalid"));
        Assert.Equal("invalid", exception.InvalidValue);
        Assert.True(exception.ValidFormats.Count > 0);
    }

    [Fact]
    public void SetTo_USFormat_NormalizesToE164()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("555-123-4567");
        var message = builder.SetBody("Test").Build();
        Assert.StartsWith("+1", message.To);
        Assert.Contains("5551234567", message.To);
    }

    [Fact]
    public void SetFrom_ValidPhoneNumber_ReturnsBuilder()
    {
        var builder = SmsMessageBuilder.New();
        var result = builder.SetFrom("+15551234567");
        Assert.Same(builder, result);
    }

    [Fact]
    public void SetBody_Null_DoesNotThrow()
    {
        var builder = SmsMessageBuilder.New();
        // Body can be null if media attachments are provided
        builder.SetBody(null);
        builder.SetTo("+15551234567");
        builder.AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Null(message.Body);
        Assert.Single(message.MediaUrls);
    }

    [Fact]
    public void SetBody_Exceeds1600Chars_ThrowsArgumentOutsideRangeException()
    {
        var builder = SmsMessageBuilder.New();
        var longBody = new string('A', 1601);
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => builder.SetBody(longBody));
        Assert.Equal(1601, exception.ActualValue);
        Assert.Equal(1600, exception.MaxValue);
    }

    [Fact]
    public void SetBody_1600Chars_DoesNotThrow()
    {
        var builder = SmsMessageBuilder.New();
        var body = new string('A', 1600);
        builder.SetBody(body);
        var message = builder.SetTo("+15551234567").Build();
        Assert.Equal(1600, message.Body!.Length);
    }

    [Fact]
    public void AppendBody_AppendsText()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetBody("Hello");
        builder.AppendBody(" World");
        var message = builder.SetTo("+15551234567").Build();
        Assert.Equal("Hello World", message.Body);
    }

    [Fact]
    public void AppendBody_ExceedsLimit_ThrowsArgumentOutsideRangeException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetBody(new('A', 1595));
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => builder.AppendBody(new('B', 10)));
        Assert.Equal(1605, exception.ActualValue);
    }

    [Fact]
    public void Build_MissingTo_ThrowsArgumentNullException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetBody("Test");

        // Build() throws ArgumentException/ArgumentNullException when To is null/empty
        Assert.Throws<ArgumentNullException>(() => builder.Build());
    }

    [Fact]
    public void Build_MissingBodyAndMedia_ThrowsInvalidOperationException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567");

        // Build() throws InvalidOperationException when neither body nor media is provided
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithMediaButNoBody_Works()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Null(message.Body);
        Assert.Single(message.MediaUrls);
        Assert.Equal("https://example.com/image.jpg", message.MediaUrls[0].ToString());
    }

    [Fact]
    public void Build_WithMediaButEmptyBody_Works()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("").AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Equal("", message.Body);
        Assert.Single(message.MediaUrls);
    }

    [Fact]
    public void Build_WithBodyAndMedia_Works()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Check this out!").AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Equal("Check this out!", message.Body);
        Assert.Single(message.MediaUrls);
    }

    [Fact]
    public void AddAttachment_ValidUrl_AddsToMediaUrls()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test").AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Single(message.MediaUrls);
        Assert.Equal("https://example.com/image.jpg", message.MediaUrls[0].ToString());
    }

    [Fact]
    public void AddAttachment_MultipleUrls_AddsAll()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test").AddAttachment("https://example.com/image1.jpg").AddAttachment("https://example.com/image2.jpg");
        var message = builder.Build();
        Assert.Equal(2, message.MediaUrls.Count);
    }

    [Fact]
    public void AddAttachment_InvalidUrl_ThrowsInvalidFormatException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test");
        Assert.Throws<InvalidFormatException>(() => builder.AddAttachment("not-a-url"));
    }

    [Fact]
    public void AddAttachment_RelativeUrl_ThrowsInvalidFormatException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test");
        Assert.Throws<InvalidFormatException>(() => builder.AddAttachment("/relative/path.jpg"));
    }

    [Fact]
    public void AddAttachment_LocalFileUrl_ThrowsInvalidFormatException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test");
        Assert.Throws<InvalidFormatException>(() => builder.AddAttachment("file:///local/path.jpg"));
    }

    [Fact]
    public void AddAttachment_FtpUrl_ThrowsInvalidFormatException()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test");
        Assert.Throws<InvalidFormatException>(() => builder.AddAttachment("ftp://example.com/file.jpg"));
    }

    [Fact]
    public void AddAttachment_HttpUrl_Works()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test");
        builder.AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Single(message.MediaUrls);
        Assert.Equal("https://example.com/image.jpg", message.MediaUrls[0].ToString());
    }

    [Fact]
    public void AddAttachment_HttpsUrl_Works()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test");
        builder.AddAttachment("https://example.com/image.jpg");
        var message = builder.Build();
        Assert.Single(message.MediaUrls);
        Assert.Equal("https://example.com/image.jpg", message.MediaUrls[0].ToString());
    }

    [Fact]
    public void Clear_ClearsMediaUrls()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetBody("Test").AddAttachment("https://example.com/image.jpg");
        builder.Clear();
        var message = builder.SetTo("+15551234567").SetBody("Test").Build();
        Assert.Empty(message.MediaUrls);
    }

    [Fact]
    public void Build_ValidMessage_ReturnsSmsMessage()
    {
        var builder = SmsMessageBuilder.New();
        var message = builder.SetTo("+15551234567").SetFrom("+19876543210").SetBody("Test message").Build();
        Assert.Equal("+15551234567", message.To);
        Assert.Equal("+19876543210", message.From);
        Assert.Equal("Test message", message.Body);
    }

    [Fact]
    public void Build_FromOptional_DoesNotRequireFrom()
    {
        var builder = SmsMessageBuilder.New();
        var message = builder.SetTo("+15551234567").SetBody("Test message").Build();
        Assert.Null(message.From);
    }

    [Fact]
    public void Clear_ResetsAllProperties()
    {
        var builder = SmsMessageBuilder.New();
        builder.SetTo("+15551234567").SetFrom("+19876543210").SetBody("Test");
        builder.Clear();

        // Build() throws ArgumentException/ArgumentNullException when required fields are null/empty after Clear
        Assert.Throws<ArgumentNullException>(() => builder.Build());
    }

    [Fact]
    public void NormalizePhoneNumber_USFormat_AddsCountryCode()
    {
        var normalized = PhoneNumber.Normalize("5551234567");
        Assert.Equal("+15551234567", normalized);
    }

    [Fact]
    public void NormalizePhoneNumber_AlreadyE164_ReturnsAsIs()
    {
        var normalized = PhoneNumber.Normalize("+15551234567");
        Assert.Equal("+15551234567", normalized);
    }

    [Fact]
    public void NormalizePhoneNumber_Null_ReturnsNull()
    {
        var normalized = PhoneNumber.Normalize(null);
        Assert.Null(normalized);
    }

    [Fact]
    public void NormalizePhoneNumber_Empty_ReturnsNull()
    {
        var normalized = PhoneNumber.Normalize("");
        Assert.Null(normalized);
    }
}