using Lyo.Email.Builders;
using MimeKit;

namespace Lyo.Email.Tests;

public class EmailRequestBuilderTests
{
    [Fact]
    public void New_CreatesNewBuilder()
    {
        var builder = EmailRequestBuilder.New();
        Assert.NotNull(builder);
    }

    [Fact]
    public void AddTo_StringEmail_AddsRecipient()
    {
        var builder = EmailRequestBuilder.New();
        var result = builder.AddTo("test@example.com", "Test User");
        Assert.Same(builder, result);
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.To.Mailboxes);
        Assert.Equal("test@example.com", message.To.Mailboxes.First().Address);
        Assert.Equal("Test User", message.To.Mailboxes.First().Name);
    }

    [Fact]
    public void AddTo_StringEmailOnly_UsesEmailAsName()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("test@example.com");
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.To.Mailboxes);
        Assert.Equal("test@example.com", message.To.Mailboxes.First().Address);
        Assert.Equal("test@example.com", message.To.Mailboxes.First().Name);
    }

    [Fact]
    public void AddTo_MultipleEmails_AddsAll()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("test1@example.com", "User 1").AddTo("test2@example.com", "User 2");
        var message = builder.SetSubject("Test").Build();
        Assert.Equal(2, message.To.Count);
    }

    [Fact]
    public void AddTo_ParamsArray_AddsAll()
    {
        var builder = EmailRequestBuilder.New();
        // Using 3+ arguments to test params array (2 args would match AddTo(string, string) overload)
        builder.AddTo("test1@example.com", "test2@example.com", "test3@example.com");
        var message = builder.SetSubject("Test").Build();
        Assert.Equal(3, message.To.Count);
    }

    [Fact]
    public void AddTo_Enumerable_AddsAll()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo(["test1@example.com", "test2@example.com"]);
        var message = builder.SetSubject("Test").Build();
        Assert.Equal(2, message.To.Count);
    }

    [Fact]
    public void AddTo_MailboxAddress_AddsRecipient()
    {
        var builder = EmailRequestBuilder.New();
        var mailbox = new MailboxAddress("Test User", "test@example.com");
        builder.AddTo(mailbox);
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.To.Mailboxes);
        Assert.Equal("test@example.com", message.To.Mailboxes.First().Address);
    }

    [Fact]
    public void AddCc_StringEmail_AddsCcRecipient()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("to@example.com").AddCc("cc@example.com", "CC User");
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.Cc.Mailboxes);
        Assert.Equal("cc@example.com", message.Cc.Mailboxes.First().Address);
    }

    [Fact]
    public void AddBcc_StringEmail_AddsBccRecipient()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("to@example.com").AddBcc("bcc@example.com", "BCC User");
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.Bcc.Mailboxes);
        Assert.Equal("bcc@example.com", message.Bcc.Mailboxes.First().Address);
    }

    [Fact]
    public void SetFrom_StringEmail_AddsFrom()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetFrom("sender@example.com", "Sender Name");
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.From.Mailboxes);
        Assert.Equal("sender@example.com", message.From.Mailboxes.First().Address);
        Assert.Equal("Sender Name", message.From.Mailboxes.First().Name);
    }

    [Fact]
    public void SetFrom_StringEmailOnly_UsesEmailAsName()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetFrom("sender@example.com");
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.From.Mailboxes);
        Assert.Equal("sender@example.com", message.From.Mailboxes.First().Address);
        Assert.Equal("sender@example.com", message.From.Mailboxes.First().Name);
    }

    [Fact]
    public void SetReplyTo_StringEmail_AddsReplyTo()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetReplyTo("reply@example.com", "Reply Name");
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.ReplyTo.Mailboxes);
        Assert.Equal("reply@example.com", message.ReplyTo.Mailboxes.First().Address);
    }

    [Fact]
    public void SetSubject_SetsSubject()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetSubject("Test Subject");
        var message = builder.Build();
        Assert.Equal("Test Subject", message.Subject);
    }

    [Fact]
    public void SetPriority_SetsPriority()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetPriority(MessagePriority.Urgent);
        var message = builder.SetSubject("Test").Build();
        Assert.Equal(MessagePriority.Urgent, message.Priority);
    }

    [Fact]
    public void SetHtmlBody_SetsHtmlBody()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetHtmlBody("<html><body>Test</body></html>");
        var message = builder.SetSubject("Test").Build();
        Assert.NotNull(message.HtmlBody);
        Assert.Contains("Test", message.HtmlBody);
    }

    [Fact]
    public void SetTextBody_SetsTextBody()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetTextBody("Plain text body");
        var message = builder.SetSubject("Test").Build();
        Assert.NotNull(message.TextBody);
        Assert.Contains("Plain text body", message.TextBody);
    }

    [Fact]
    public void AppendHtmlBody_AppendsToHtmlBody()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetHtmlBody("<html><body>First</body></html>");
        builder.AppendHtmlBody("<p>Second</p>");
        var message = builder.SetSubject("Test").Build();
        Assert.Contains("First", message.HtmlBody);
        Assert.Contains("Second", message.HtmlBody);
    }

    [Fact]
    public void AppendTextBody_AppendsToTextBody()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetTextBody("First");
        builder.AppendTextBody("Second");
        var message = builder.SetSubject("Test").Build();
        Assert.Contains("First", message.TextBody);
        Assert.Contains("Second", message.TextBody);
    }

    [Fact]
    public void AddAttachment_FileNameAndData_AddsAttachment()
    {
        var builder = EmailRequestBuilder.New();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        builder.AddAttachment("test.txt", data);
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.BodyParts.OfType<MimePart>());
    }

    [Fact]
    public void AddAttachment_FileNameAndStream_AddsAttachment()
    {
        var builder = EmailRequestBuilder.New();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(data);
        builder.AddAttachment("test.txt", stream);
        var message = builder.SetSubject("Test").Build();
        Assert.Single(message.BodyParts.OfType<MimePart>());
    }

    [Fact]
    public void AddAttachment_Dictionary_AddsAllAttachments()
    {
        var builder = EmailRequestBuilder.New();
        var files = new Dictionary<string, byte[]> { { "file1.txt", [1, 2, 3] }, { "file2.txt", [4, 5, 6] } };
        builder.AddAttachment(files);
        var message = builder.SetSubject("Test").Build();
        var attachments = message.BodyParts.OfType<MimePart>().ToList();
        Assert.Equal(2, attachments.Count);
    }

    [Fact]
    public void AddAttachmentFromFile_ValidPath_AddsAttachment()
    {
        var tempFile = Path.GetTempFileName();
        try {
            File.WriteAllText(tempFile, "test content");
            var builder = EmailRequestBuilder.New();
            builder.AddAttachmentFromFile(tempFile);
            var message = builder.SetSubject("Test").Build();
            Assert.Single(message.BodyParts.OfType<MimePart>());
        }
        finally {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void AddAttachmentsAsZip_Dictionary_AddsZipAttachment()
    {
        var builder = EmailRequestBuilder.New();
        var files = new Dictionary<string, byte[]> { { "file1.txt", [1, 2, 3] }, { "file2.txt", [4, 5, 6] } };
        builder.AddAttachmentsAsZip("files.zip", files);
        var message = builder.SetSubject("Test").Build();
        var attachments = message.BodyParts.OfType<MimePart>().ToList();
        Assert.Single(attachments);
        Assert.Equal("files.zip", attachments[0].FileName);
    }

    [Fact]
    public void AddAttachmentsAsZip_FilePaths_AddsZipAttachment()
    {
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        try {
            File.WriteAllText(tempFile1, "content1");
            File.WriteAllText(tempFile2, "content2");
            var builder = EmailRequestBuilder.New();
            builder.AddAttachmentsAsZip("files.zip", tempFile1, tempFile2);
            var message = builder.SetSubject("Test").Build();
            var attachments = message.BodyParts.OfType<MimePart>().ToList();
            Assert.Single(attachments);
            Assert.Equal("files.zip", attachments[0].FileName);
        }
        finally {
            File.Delete(tempFile1);
            File.Delete(tempFile2);
        }
    }

    [Fact]
    public void AddZippedFile_WithConfigure_AddsZipAttachment()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddZippedFile(
            "files.zip", zip => {
                zip.AddFile("file1.txt", [1, 2, 3]);
                zip.AddFile("file2.txt", [4, 5, 6]);
            });

        var message = builder.SetSubject("Test").Build();
        var attachments = message.BodyParts.OfType<MimePart>().ToList();
        Assert.Single(attachments);
        Assert.Equal("files.zip", attachments[0].FileName);
    }

    [Fact]
    public void AddHeader_AddsCustomHeader()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddHeader("X-Custom-Header", "CustomValue");
        var message = builder.SetSubject("Test").Build();
        Assert.Equal("CustomValue", message.Headers["X-Custom-Header"]);
    }

    [Fact]
    public void ClearTo_ClearsToRecipients()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("test@example.com");
        builder.ClearTo();
        var message = builder.SetSubject("Test").Build();
        Assert.Empty(message.To);
    }

    [Fact]
    public void ClearCc_ClearsCcRecipients()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddCc("cc@example.com");
        builder.ClearCc();
        var message = builder.SetSubject("Test").Build();
        Assert.Empty(message.Cc);
    }

    [Fact]
    public void ClearBcc_ClearsBccRecipients()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddBcc("bcc@example.com");
        builder.ClearBcc();
        var message = builder.SetSubject("Test").Build();
        Assert.Empty(message.Bcc);
    }

    [Fact]
    public void ClearAttachments_ClearsAttachments()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("test@example.com").SetTextBody("Test body").AddAttachment("test.txt", [1, 2, 3]);
        builder.ClearAttachments();
        var message = builder.Build();
        // After clearing attachments, check that only body parts remain (not attachments)
        // When there's a body and attachments, MimeKit creates a Multipart
        // When attachments are cleared, the body should be the only part
        if (message.Body is Multipart multipart) {
            // Count attachment parts (parts with ContentDisposition = Attachment)
            var attachmentParts = multipart.OfType<MimePart>()
                .Where(p => p.ContentDisposition != null && p.ContentDisposition.Disposition == ContentDisposition.Attachment)
                .ToList();

            Assert.Empty(attachmentParts);
        }
        else {
            // If body is not multipart, there are no attachments (body is TextPart or similar)
            Assert.True(true); // No attachments
        }
    }

    [Fact]
    public void Build_CreatesMimeMessage()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetSubject("Test Subject").SetTextBody("Test Body").AddTo("test@example.com");
        var message = builder.Build();
        Assert.NotNull(message);
        Assert.Equal("Test Subject", message.Subject);
    }

    [Fact]
    public void ToString_ReturnsDescriptiveString()
    {
        var builder = EmailRequestBuilder.New();
        builder.SetSubject("Test Subject").AddTo("test@example.com");
        var result = builder.ToString();
        Assert.Contains("Test Subject", result);
        Assert.Contains("1", result);
    }

    [Fact]
    public void ToString_NoSubject_ShowsNoSubject()
    {
        var builder = EmailRequestBuilder.New();
        builder.AddTo("test@example.com");
        var result = builder.ToString();
        Assert.Contains("(no subject)", result);
    }
}