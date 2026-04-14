using Lyo.Exceptions.Models;
using Lyo.Sms.Builders;

namespace Lyo.Sms.Tests;

public class BulkSmsBuilderTests
{
    [Fact]
    public void SetDefaultFrom_ValidPhoneNumber_SetsDefault()
    {
        var builder = BulkSmsBuilder.New();
        var result = builder.SetDefaultFrom("+15551234567");
        Assert.Same(builder, result);
    }

    [Fact]
    public void SetDefaultFrom_Null_ThrowsArgumentNullException()
    {
        var builder = BulkSmsBuilder.New();
        Assert.Throws<ArgumentNullException>(() => builder.SetDefaultFrom(null!));
    }

    [Fact]
    public void SetDefaultFrom_Empty_ThrowsArgumentException()
    {
        var builder = BulkSmsBuilder.New();
        Assert.Throws<ArgumentException>(() => builder.SetDefaultFrom(""));
    }

    [Fact]
    public void SetDefaultFrom_InvalidFormat_ThrowsInvalidFormatException()
    {
        var builder = BulkSmsBuilder.New();
        var exception = Assert.Throws<InvalidFormatException>(() => builder.SetDefaultFrom("invalid"));
        Assert.Equal("invalid", exception.InvalidValue);
        Assert.True(exception.ValidFormats.Count > 0);
    }

    [Fact]
    public void Add_ValidMessage_AddsMessage()
    {
        var builder = BulkSmsBuilder.New();
        builder.Add("+15551234567", "Test message");
        Assert.Equal(1, builder.Count);
    }

    [Fact]
    public void Add_NullTo_ThrowsArgumentNullException()
    {
        var builder = BulkSmsBuilder.New();
        Assert.Throws<ArgumentNullException>(() => builder.Add(null!, "Test"));
    }

    [Fact]
    public void Add_NullBody_DoesNotThrow()
    {
        var builder = BulkSmsBuilder.New();
        // Body can be null if media attachments are provided
        builder.Add("+15551234567", null);
        builder.AddAttachment("https://example.com/image.jpg");
        Assert.Equal(1, builder.Count);
    }

    [Fact]
    public void AddAttachment_AddsToLastMessage()
    {
        var builder = BulkSmsBuilder.New();
        builder.Add("+15551234567", "Test1").AddAttachment("https://example.com/image1.jpg").Add("+15551234568", "Test2").AddAttachment("https://example.com/image2.jpg");
        var builders = builder.Build().ToList();
        var message1 = builders[0].Build();
        var message2 = builders[1].Build();
        Assert.Single(message1.MediaUrls);
        Assert.Single(message2.MediaUrls);
    }

    [Fact]
    public void AddAttachment_NoMessages_ThrowsInvalidOperationException()
    {
        var builder = BulkSmsBuilder.New();
        Assert.Throws<InvalidOperationException>(() => builder.AddAttachment("https://example.com/image.jpg"));
    }

    [Fact]
    public void AddAttachmentToMessage_ValidIndex_AddsAttachment()
    {
        var builder = BulkSmsBuilder.New();
        builder.Add("+15551234567", "Test1")
            .Add("+15551234568", "Test2")
            .AddAttachmentToMessage(0, "https://example.com/image1.jpg")
            .AddAttachmentToMessage(1, "https://example.com/image2.jpg");

        var builders = builder.Build().ToList();
        var message1 = builders[0].Build();
        var message2 = builders[1].Build();
        Assert.Single(message1.MediaUrls);
        Assert.Single(message2.MediaUrls);
    }

    [Fact]
    public void AddAttachmentToMessage_InvalidIndex_ThrowsArgumentOutsideRangeException()
    {
        var builder = BulkSmsBuilder.New();
        builder.Add("+15551234567", "Test");
        Assert.Throws<ArgumentOutsideRangeException>(() => builder.AddAttachmentToMessage(5, "https://example.com/image.jpg"));
    }

    [Fact]
    public void Add_Exceeds1600Chars_ThrowsArgumentOutsideRangeException()
    {
        var builder = BulkSmsBuilder.New();
        var longBody = new string('A', 1601);
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => builder.Add("+15551234567", longBody));
        Assert.Equal(1601, exception.ActualValue);
        Assert.Equal(1600, exception.MaxValue);
    }

    [Fact]
    public void Add_WithFrom_AddsMessageWithSender()
    {
        var builder = BulkSmsBuilder.New();
        builder.Add("+15551234567", "Test", "+19876543210");
        Assert.Equal(1, builder.Count);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        var builder = BulkSmsBuilder.New();
        builder.SetDefaultFrom("+15551234567").Add("+19876543210", "Test1").Add("+19876543211", "Test2");
        builder.Clear();
        Assert.Equal(0, builder.Count);
    }

    [Fact]
    public void Build_NoMessages_ThrowsInvalidOperationException()
    {
        var builder = BulkSmsBuilder.New();
        Assert.Throws<InvalidOperationException>(() => builder.Build().ToList());
    }

    [Fact]
    public void Build_WithMessages_ReturnsBuilders()
    {
        var builder = BulkSmsBuilder.New();
        builder.SetDefaultFrom("+15551234567").Add("+19876543210", "Test1").Add("+19876543211", "Test2");
        var builders = builder.Build().ToList();
        Assert.Equal(2, builders.Count);
    }

    [Fact]
    public void Build_UsesDefaultFrom_WhenNotSpecified()
    {
        var builder = BulkSmsBuilder.New();
        builder.SetDefaultFrom("+15551234567").Add("+19876543210", "Test1");
        var builders = builder.Build().ToList();
        var message = builders[0].Build();
        Assert.Equal("+15551234567", message.From);
    }

    [Fact]
    public void Build_UsesMessageSpecificFrom_WhenProvided()
    {
        var builder = BulkSmsBuilder.New();
        builder.SetDefaultFrom("+15551234567").Add("+19876543210", "Test1", "+19998887777");
        var builders = builder.Build().ToList();
        var message = builders[0].Build();
        Assert.Equal("+19998887777", message.From);
    }

    [Fact]
    public void SetMaxLimit_ValidLimit_SetsLimit()
    {
        var builder = BulkSmsBuilder.New();
        var result = builder.SetMaxLimit(100);
        Assert.Same(builder, result);
    }

    [Fact]
    public void SetMaxLimit_Zero_ThrowsArgumentOutsideRangeException()
    {
        var builder = BulkSmsBuilder.New();
        Assert.Throws<ArgumentOutsideRangeException>(() => builder.SetMaxLimit(0));
    }

    [Fact]
    public void Add_ExceedsMaxLimit_ThrowsArgumentOutsideRangeException()
    {
        var builder = BulkSmsBuilder.New();
        builder.SetMaxLimit(2);
        builder.Add("+15551234567", "Test1");
        builder.Add("+15551234568", "Test2");
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => builder.Add("+15551234569", "Test3"));
        Assert.Equal(2, exception.MaxValue);
        Assert.Equal(3, exception.ActualValue);
    }

    [Fact]
    public void Clear_ResetsMaxLimit()
    {
        var builder = BulkSmsBuilder.New();
        builder.SetMaxLimit(10).Add("+15551234567", "Test1");
        builder.Clear();

        // Should be able to add more than the previous limit after clear
        builder.Add("+15551234567", "Test1");
        builder.Add("+15551234568", "Test2");
        builder.Add("+15551234569", "Test3");
        Assert.Equal(3, builder.Count);
    }
}