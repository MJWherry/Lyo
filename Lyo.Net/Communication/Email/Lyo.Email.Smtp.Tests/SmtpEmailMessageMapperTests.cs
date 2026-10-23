using System.Text;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using MimeKit;

namespace Lyo.Email.Smtp.Tests;

public class SmtpEmailMessageMapperTests
{
    [Fact]
    public void ToMimeMessage_KeepsNamesBodiesContentTypeReplyToHeadersAndPriority()
    {
        var request = new EmailRequest(
            ToAddresses: [new EmailAddress("ada@example.com", "Ada"), new EmailAddress("bea@example.com")],
            CcAddresses: [new EmailAddress("cee@example.com", "Cee")],
            BccAddresses: [new EmailAddress("dee@example.com", "Dee")],
            Subject: "Hello",
            Attachments: [new EmailAttachment("note.txt", "file"u8.ToArray(), "text/plain", """{"keep":true}""")],
            TextBody: "plain text",
            HtmlBody: "<p>html</p>",
            ReplyTo: [new EmailAddress("reply@example.com", "Reply"), new EmailAddress("other@example.com", "Other")],
            Headers: [new EmailHeader("X-Custom-Header", "CustomValue")],
            Priority: EmailPriority.Urgent);

        var message = SmtpEmailMessageMapper.ToMimeMessage(request, Options());
        Assert.Equal("from@example.com", message.From.Mailboxes.Single().Address);
        Assert.Equal("From", message.From.Mailboxes.Single().Name);
        Assert.Equal(["ada@example.com", "bea@example.com"], message.To.Mailboxes.Select(static mailbox => mailbox.Address));
        Assert.Equal("Ada", message.To.Mailboxes.First().Name);
        Assert.Equal("bea@example.com", message.To.Mailboxes.Last().Name);
        Assert.Equal("Cee", message.Cc.Mailboxes.Single().Name);
        Assert.Equal("Dee", message.Bcc.Mailboxes.Single().Name);
        Assert.Equal("plain text", message.TextBody);
        Assert.Contains("html", message.HtmlBody);
        Assert.Equal(["reply@example.com", "other@example.com"], message.ReplyTo.Mailboxes.Select(static mailbox => mailbox.Address));
        Assert.Equal("CustomValue", message.Headers["X-Custom-Header"]);
        Assert.Equal(MessagePriority.Urgent, message.Priority);
        Assert.Equal("urgent", message.Headers[HeaderId.Priority]);
        var attachment = Assert.IsAssignableFrom<MimePart>(Assert.Single(message.Attachments));
        Assert.Equal("note.txt", attachment.FileName);
        Assert.Equal("text/plain", attachment.ContentType.MimeType);
        Assert.DoesNotContain("keep", Encoding.UTF8.GetString(Read(attachment)));
    }

    [Theory]
    [InlineData(EmailPriority.Normal, MessagePriority.Normal, "normal")]
    [InlineData(EmailPriority.NonUrgent, MessagePriority.NonUrgent, "non-urgent")]
    [InlineData(EmailPriority.Urgent, MessagePriority.Urgent, "urgent")]
    public void ToMimeMessage_Priority_WritesTheMimeKitHeader(EmailPriority priority, MessagePriority expected, string header)
    {
        var message = SmtpEmailMessageMapper.ToMimeMessage(new EmailRequest(Priority: priority), Options());
        Assert.Equal(expected, message.Priority);
        var written = message.Headers[HeaderId.Priority];
        if (priority == EmailPriority.Normal)
            Assert.Null(written);
        else
            Assert.Equal(header, written);
    }

    [Fact]
    public void ToMimeMessage_ZipContentType_Survives()
    {
        var request = EmailRequestBuilder.New().AddAttachmentsAsZip("files.zip", new Dictionary<string, byte[]> { ["file1.txt"] = [1] }).Build();
        var message = SmtpEmailMessageMapper.ToMimeMessage(request, Options());
        var attachment = Assert.IsType<MimePart>(Assert.Single(message.Attachments));
        Assert.Equal("application/zip", attachment.ContentType.MimeType);
    }

    [Fact]
    public void ToMimeMessage_NullFrom_UsesOptionsDefaultWithoutMutatingTheRequest()
    {
        var request = new EmailRequest(ToAddresses: [new EmailAddress("ada@example.com")]);
        var message = SmtpEmailMessageMapper.ToMimeMessage(request, Options());
        Assert.Null(request.FromAddress);
        Assert.Equal("from@example.com", message.From.Mailboxes.Single().Address);
        Assert.Equal("From", message.From.Mailboxes.Single().Name);
        Assert.Equal("ada@example.com", message.To.Mailboxes.Single().Name);
    }

    private static SmtpEmailOptions Options()
        => new() { Host = "127.0.0.1", Port = 1, DefaultFromAddress = "from@example.com", DefaultFromName = "From" };

    private static byte[] Read(MimePart part)
    {
        using var stream = new MemoryStream();
        var content = part.Content ?? throw new InvalidOperationException("Attachment has no content.");
        content.DecodeTo(stream);
        return stream.ToArray();
    }
}
