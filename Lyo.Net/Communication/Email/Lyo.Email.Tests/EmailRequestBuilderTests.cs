using Lyo.Common.Metadata.Records;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;

namespace Lyo.Email.Tests;

public class EmailRequestBuilderTests
{
    [Fact]
    public void New_CreatesNewBuilder() => Assert.NotNull(EmailRequestBuilder.New());

    [Fact]
    public void AddTo_StringEmail_AddsRecipient()
    {
        var request = EmailRequestBuilder.New().AddTo("test@example.com", "Test User").SetSubject("Test").Build();
        var to = Assert.Single(request.ToAddresses!);
        Assert.Equal("test@example.com", to.Address);
        Assert.Equal("Test User", to.Name);
    }

    [Fact]
    public void AddTo_StringEmailOnly_UsesEmailAsName()
    {
        var request = EmailRequestBuilder.New().AddTo("test@example.com").SetSubject("Test").Build();
        var to = Assert.Single(request.ToAddresses!);
        Assert.Equal("test@example.com", to.Address);
        Assert.Equal("test@example.com", to.Name);
    }

    [Fact]
    public void AddTo_EmailAddress_AddsRecipient()
    {
        var request = EmailRequestBuilder.New().AddTo(new EmailAddress("test@example.com", "Test User")).SetSubject("Test").Build();
        var to = Assert.Single(request.ToAddresses!);
        Assert.Equal("test@example.com", to.Address);
        Assert.Equal("Test User", to.Name);
    }

    [Fact]
    public void AddTo_ParamsArray_AddsAll()
    {
        var request = EmailRequestBuilder.New().AddTo("test1@example.com", "test2@example.com", "test3@example.com").SetSubject("Test").Build();
        Assert.Equal(3, request.ToAddresses!.Count);
    }

    [Fact]
    public void AddCc_StringEmail_KeepsDisplayName()
    {
        var request = EmailRequestBuilder.New().AddTo("to@example.com").AddCc("cc@example.com", "CC User").Build();
        var cc = Assert.Single(request.CcAddresses!);
        Assert.Equal("cc@example.com", cc.Address);
        Assert.Equal("CC User", cc.Name);
    }

    [Fact]
    public void AddBcc_StringEmail_KeepsDisplayName()
    {
        var request = EmailRequestBuilder.New().AddTo("to@example.com").AddBcc("bcc@example.com", "BCC User").Build();
        var bcc = Assert.Single(request.BccAddresses!);
        Assert.Equal("bcc@example.com", bcc.Address);
        Assert.Equal("BCC User", bcc.Name);
    }

    [Fact]
    public void SetFrom_LastCallWins()
    {
        var request = EmailRequestBuilder.New().SetFrom("first@example.com", "First").SetFrom("sender@example.com", "Sender Name").Build();
        Assert.Equal("sender@example.com", request.FromAddress);
        Assert.Equal("Sender Name", request.FromName);
    }

    [Fact]
    public void SetFrom_StringEmailOnly_UsesEmailAsName()
    {
        var request = EmailRequestBuilder.New().SetFrom("sender@example.com").Build();
        Assert.Equal("sender@example.com", request.FromAddress);
        Assert.Equal("sender@example.com", request.FromName);
    }

    [Fact]
    public void SetReplyTo_AppendsEachCall()
    {
        var request = EmailRequestBuilder.New().SetReplyTo("one@example.com", "One").SetReplyTo("two@example.com", "Two").Build();
        Assert.Equal(2, request.ReplyTo!.Count);
        Assert.Equal("one@example.com", request.ReplyTo[0].Address);
        Assert.Equal("Two", request.ReplyTo[1].Name);
    }

    [Fact]
    public void SetSubject_Empty_IsAllowed()
    {
        var request = EmailRequestBuilder.New().SetSubject(string.Empty).Build();
        Assert.Equal(string.Empty, request.Subject);
    }

    [Fact]
    public void SetSubject_Null_Throws() => Assert.Throws<ArgumentNullException>(() => EmailRequestBuilder.New().SetSubject(null!));

    [Theory]
    [InlineData(EmailPriority.Normal)]
    [InlineData(EmailPriority.NonUrgent)]
    [InlineData(EmailPriority.Urgent)]
    public void SetPriority_SetsPriority(EmailPriority priority)
    {
        var request = EmailRequestBuilder.New().SetPriority(priority).Build();
        Assert.Equal(priority, request.Priority);
    }

    [Fact]
    public void SetHtmlAndText_AndAppend_KeepBothBodies()
    {
        var request = EmailRequestBuilder.New().SetHtmlBody("<p>First</p>").AppendHtmlBody("<p>Second</p>").SetTextBody("First").AppendTextBody("Second").Build();
        Assert.Contains("First", request.HtmlBody);
        Assert.Contains("Second", request.HtmlBody);
        Assert.Contains("First", request.TextBody);
        Assert.Contains("Second", request.TextBody);
    }

    [Fact]
    public void AddAttachment_Bytes_KeepsContentTypeAndMetadata()
    {
        var data = new byte[] { 1, 2, 3 };
        var request = EmailRequestBuilder.New().AddAttachment("test.txt", data, "text/plain", """{"k":1}""").Build();
        var attachment = Assert.Single(request.Attachments!);
        Assert.Equal("test.txt", attachment.FileName);
        Assert.Equal(data, attachment.Data);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal("""{"k":1}""", attachment.MetadataJson);
    }

    [Fact]
    public void AddAttachment_Stream_ReadsBytes()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);
        var request = EmailRequestBuilder.New().AddAttachment("test.txt", stream).Build();
        var attachment = Assert.Single(request.Attachments!);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, attachment.Data);
    }

    [Fact]
    public void AddAttachment_Dictionary_AddsAll()
    {
        var request = EmailRequestBuilder.New().AddAttachment(new Dictionary<string, byte[]> { ["file1.txt"] = [1], ["file2.txt"] = [2] }).Build();
        Assert.Equal(2, request.Attachments!.Count);
    }

    [Fact]
    public void AddAttachmentFromFile_ReadsTheFile()
    {
        using var session = new IOTempSession();
        var path = session.CreateFile("hello"u8.ToArray(), "note.txt");
        var request = EmailRequestBuilder.New().AddAttachmentFromFile(path).Build();
        var attachment = Assert.Single(request.Attachments!);
        Assert.Equal("note.txt", attachment.FileName);
        Assert.Equal("hello"u8.ToArray(), attachment.Data);
    }

    [Fact]
    public void AddAttachmentsAsZip_SetsZipContentType()
    {
        var request = EmailRequestBuilder.New().AddAttachmentsAsZip("files.zip", new Dictionary<string, byte[]> { ["file1.txt"] = [1] }).Build();
        var attachment = Assert.Single(request.Attachments!);
        Assert.Equal("files.zip", attachment.FileName);
        Assert.Equal(FileTypeInfo.Zip.MimeType, attachment.ContentType);
        Assert.NotEmpty(attachment.Data);
    }

    [Fact]
    public void AddZippedFile_AddsZipAttachment()
    {
        var request = EmailRequestBuilder.New().AddZippedFile("files.zip", zip => zip.AddFile("file1.txt", [1, 2, 3])).Build();
        var attachment = Assert.Single(request.Attachments!);
        Assert.Equal("files.zip", attachment.FileName);
        Assert.Equal(FileTypeInfo.Zip.MimeType, attachment.ContentType);
    }

    [Fact]
    public void AddHeader_AddsCustomHeader()
    {
        var request = EmailRequestBuilder.New().AddHeader("X-Custom-Header", "CustomValue").Build();
        var header = Assert.Single(request.Headers!);
        Assert.Equal("X-Custom-Header", header.Name);
        Assert.Equal("CustomValue", header.Value);
    }

    [Fact]
    public void ClearToCcBccAndAttachments_RemovesThem()
    {
        var request = EmailRequestBuilder.New().AddTo("to@example.com").AddCc("cc@example.com").AddBcc("bcc@example.com").AddAttachment("test.txt", [1]).ClearTo().ClearCc().ClearBcc()
            .ClearAttachments()
            .Build();

        Assert.Null(request.ToAddresses);
        Assert.Null(request.CcAddresses);
        Assert.Null(request.BccAddresses);
        Assert.Null(request.Attachments);
    }

    [Fact]
    public void ToString_IncludesSubjectAndCount()
    {
        var text = EmailRequestBuilder.New().SetSubject("Test Subject").AddTo("test@example.com").ToString();
        Assert.Contains("Test Subject", text);
        Assert.Contains("1", text);
    }

    [Fact]
    public void ToString_NoSubject_ShowsNoSubject() => Assert.Contains("(no subject)", EmailRequestBuilder.New().AddTo("test@example.com").ToString());

    [Fact]
    public void EmailAddress_ToString_ReturnsAddress() => Assert.Equal("ada@x.com", new EmailAddress("ada@x.com", "Ada").ToString());
}
