using System.Text;
using MimeKit;

namespace Lyo.Email.Tests;

public class EmailMimeParserTests
{
    [Fact]
    public void ParseEml_ReturnsTextHtmlAndAttachment()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Ada", "ada@x.com"));
        message.To.Add(new MailboxAddress("Bea", "bea@x.com"));
        message.Subject = "Hello";
        var body = new BodyBuilder {
            TextBody = "plain text",
            HtmlBody = "<p>html</p>"
        };
        body.Attachments.Add("note.txt", Encoding.UTF8.GetBytes("file-bytes"));
        message.Body = body.ToMessageBody();
        using var stream = new MemoryStream();
        message.WriteTo(stream);

        var parsed = EmailMimeParser.Parse(stream.ToArray());

        Assert.Equal("Hello", parsed.Headers["Subject"]);
        Assert.Equal("plain text", parsed.TextBody);
        Assert.Contains("html", parsed.HtmlBody);
        var attachment = Assert.Single(parsed.Attachments);
        Assert.Equal("note.txt", attachment.FileName);
        Assert.Equal("file-bytes", Encoding.UTF8.GetString(attachment.Bytes));
    }
}
