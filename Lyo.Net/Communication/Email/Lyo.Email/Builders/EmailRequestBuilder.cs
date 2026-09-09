using System.Diagnostics;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Result;
using MimeKit;

namespace Lyo.Email.Builders;

/// <summary>Fluent helper for an email: recipients, attachments, and custom headers.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EmailRequestBuilder
{
    private readonly List<EmailAttachment> _attachmentMetadata = [];

    private readonly BodyBuilder _bodyBuilder = new();

    private readonly MimeMessage _message = new();

    /// <summary>Attachment metadata used in logs (fileName and similar). Empty when there are none.</summary>
    public IReadOnlyList<EmailAttachment>? AttachmentMetadata => _attachmentMetadata.Count > 0 ? _attachmentMetadata : null;

    /// <summary>Adds several To recipients from an enumerable.</summary>
    /// <param name="to">Addresses; each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(IEnumerable<string> to)
    {
        foreach (var i in to)
            AddTo(i);

        return this;
    }

    /// <summary>Adds several To recipients via a params array.</summary>
    /// <param name="to">Addresses; each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    /// <remarks>Exactly two string arguments bind to AddTo(string email, string name). Pass three or more, or an array, to hit this overload.</remarks>
    public EmailRequestBuilder AddTo(params string[] to)
    {
        ArgumentHelpers.ThrowIfNull(to);
        foreach (var i in to)
            AddTo(i);

        return this;
    }

    /// <summary>Adds one To recipient with address and display name.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", "Email (e.g., user@example.com)");
        _message.To.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Adds one To recipient from a MailboxAddress.</summary>
    /// <param name="to">Mailbox to add.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(MailboxAddress to)
    {
        ArgumentHelpers.ThrowIfNull(to);
        _message.To.Add(to);
        return this;
    }

    /// <summary>Adds several Cc recipients from an enumerable.</summary>
    /// <param name="cc">Addresses; each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(IEnumerable<string> cc)
    {
        ArgumentHelpers.ThrowIfNull(cc);
        foreach (var i in cc)
            AddCc(i);

        return this;
    }

    /// <summary>Adds several Cc recipients via a params array.</summary>
    /// <param name="cc">Addresses; each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(params string[] cc)
    {
        ArgumentHelpers.ThrowIfNull(cc);
        foreach (var i in cc)
            AddCc(i, i);

        return this;
    }

    /// <summary>Adds one Cc recipient with address and display name.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", "Email (e.g., user@example.com)");
        _message.Cc.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Adds one Cc recipient from a MailboxAddress.</summary>
    /// <param name="cc">Mailbox to add.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(MailboxAddress cc)
    {
        ArgumentHelpers.ThrowIfNull(cc);
        _message.Cc.Add(cc);
        return this;
    }

    /// <summary>Adds several Bcc recipients from an enumerable.</summary>
    /// <param name="bcc">Addresses; each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(IEnumerable<string> bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc);
        foreach (var i in bcc)
            AddBcc(i);

        return this;
    }

    /// <summary>Adds several Bcc recipients via a params array.</summary>
    /// <param name="bcc">Addresses; each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(params string[] bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc);
        foreach (var i in bcc)
            AddBcc(i);

        return this;
    }

    /// <summary>Adds one Bcc recipient with address and display name.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", "Email (e.g., user@example.com)");
        _message.Bcc.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Adds one Bcc recipient from a MailboxAddress.</summary>
    /// <param name="bcc">Mailbox to add.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(MailboxAddress bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc);
        _message.Bcc.Add(bcc);
        return this;
    }

    /// <summary>Sets the From address.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Optional display name; defaults to the address.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetFrom(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", "Email (e.g., user@example.com)");
        _message.From.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Sets From from a MailboxAddress.</summary>
    /// <param name="from">Mailbox to use.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetFrom(MailboxAddress from)
    {
        ArgumentHelpers.ThrowIfNull(from);
        _message.From.Add(from);
        return this;
    }

    /// <summary>Sets the Reply-To address.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Optional display name; defaults to the address.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetReplyTo(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", "Email (e.g., user@example.com)");
        _message.ReplyTo.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Sets Reply-To from a MailboxAddress.</summary>
    /// <param name="replyTo">Mailbox to use.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetReplyTo(MailboxAddress replyTo)
    {
        ArgumentHelpers.ThrowIfNull(replyTo);
        _message.ReplyTo.Add(replyTo);
        return this;
    }

    /// <summary>Sets the subject line.</summary>
    /// <param name="subject">Subject text.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetSubject(string subject)
    {
        ArgumentHelpers.ThrowIfNull(subject);
        _message.Subject = subject;
        return this;
    }

    /// <summary>Sets message priority.</summary>
    /// <param name="priority">Priority (Normal, Low, High, Urgent).</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetPriority(MessagePriority priority)
    {
        _message.Priority = priority;
        return this;
    }

    /// <summary>Replaces the HTML body.</summary>
    /// <param name="html">HTML content.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetHtmlBody(string html)
    {
        _bodyBuilder.HtmlBody = html;
        return this;
    }

    /// <summary>Replaces the plain-text body.</summary>
    /// <param name="text">Plain-text content.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetTextBody(string text)
    {
        _bodyBuilder.TextBody = text;
        return this;
    }

    /// <summary>Appends HTML to the current HTML body.</summary>
    /// <param name="html">HTML to append.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AppendHtmlBody(string html)
    {
        _bodyBuilder.HtmlBody += html;
        return this;
    }

    /// <summary>Appends plain text to the current text body.</summary>
    /// <param name="text">Text to append.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AppendTextBody(string text)
    {
        _bodyBuilder.TextBody += text;
        return this;
    }

    /// <summary>Adds an EmailAttachment, including optional log metadata.</summary>
    /// <param name="attachment">Attachment with name, bytes, and optional metadata.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(EmailAttachment attachment)
    {
        ArgumentHelpers.ThrowIfNull(attachment);
        var contentType = !attachment.ContentType.IsNullOrWhitespace() ? ContentType.Parse(attachment.ContentType) : null;
        if (contentType != null)
            _bodyBuilder.Attachments.Add(attachment.FileName, attachment.Data, contentType);
        else
            _bodyBuilder.Attachments.Add(attachment.FileName, attachment.Data);

        _attachmentMetadata.Add(attachment);
        return this;
    }

    /// <summary>Adds an attachment from bytes.</summary>
    /// <param name="fileName">Attachment file name.</param>
    /// <param name="data">File bytes.</param>
    /// <param name="contentType">Optional MIME type; MimeKit infers it from the extension when omitted.</param>
    /// <param name="metadataJson">Optional JSON metadata stored only in logs.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(
        string fileName,
        byte[] data,
        ContentType? contentType = null,
        string? metadataJson = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentHelpers.ThrowIfNull(data);
        if (contentType != null)
            _bodyBuilder.Attachments.Add(fileName, data, contentType);
        else
            _bodyBuilder.Attachments.Add(fileName, data);

        _attachmentMetadata.Add(new(fileName, data, contentType?.ToString(), metadataJson));
        return this;
    }

    /// <summary>Adds an attachment from a stream.</summary>
    /// <param name="fileName">Attachment file name.</param>
    /// <param name="data">Stream of file bytes.</param>
    /// <param name="contentType">Optional MIME type; MimeKit infers it from the extension when omitted.</param>
    /// <param name="metadataJson">Optional JSON metadata stored only in logs.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(
        string fileName,
        Stream data,
        ContentType? contentType = null,
        string? metadataJson = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentHelpers.ThrowIfNull(data);
        OperationHelpers.ThrowIfNotReadable(data, $"Stream '{nameof(data)}' must be readable.");
        var bytes = ReadStreamToBytes(data);
        if (contentType != null)
            _bodyBuilder.Attachments.Add(fileName, bytes, contentType);
        else
            _bodyBuilder.Attachments.Add(fileName, bytes);

        _attachmentMetadata.Add(new(fileName, bytes, contentType?.ToString(), metadataJson));
        return this;
    }

    /// <summary>Adds several attachments from a name-to-bytes map.</summary>
    /// <param name="files">Map of file name to bytes.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(Dictionary<string, byte[]> files)
    {
        ArgumentHelpers.ThrowIfNull(files);
        foreach (var file in files) {
            _bodyBuilder.Attachments.Add(file.Key, file.Value);
            _attachmentMetadata.Add(new(file.Key, file.Value));
        }

        return this;
    }

    /// <summary>Attaches a file from disk.</summary>
    /// <param name="filePath">Path of the file.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachmentFromFile(string filePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        var fileName = Path.GetFileName(filePath);
        var data = File.ReadAllBytes(filePath);
        _bodyBuilder.Attachments.Add(fileName, data);
        _attachmentMetadata.Add(new(fileName, data));
        return this;
    }

    /// <summary>Packs several files into one ZIP attachment.</summary>
    /// <param name="zipFileName">Name of the ZIP attachment.</param>
    /// <param name="files">Map of archive entry name to bytes.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachmentsAsZip(string zipFileName, Dictionary<string, byte[]> files)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipFileName);
        ArgumentHelpers.ThrowIfNull(files);
        var zipData = ZipFileBuilder.New().AddFiles(files).Build();
        _bodyBuilder.Attachments.Add(zipFileName, zipData, ContentType.Parse(FileTypeInfo.Zip.MimeType));
        _attachmentMetadata.Add(new(zipFileName, zipData, FileTypeInfo.Zip.MimeType));
        return this;
    }

    /// <summary>Packs several disk files into one ZIP attachment.</summary>
    /// <param name="zipFileName">Name of the ZIP attachment.</param>
    /// <param name="filePaths">Paths to include.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachmentsAsZip(string zipFileName, params string[] filePaths)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipFileName);
        ArgumentHelpers.ThrowIfNull(filePaths);
        var zipData = ZipFileBuilder.New().AddFiles(filePaths).Build();
        _bodyBuilder.Attachments.Add(zipFileName, zipData, ContentType.Parse(FileTypeInfo.Zip.MimeType));
        _attachmentMetadata.Add(new(zipFileName, zipData, FileTypeInfo.Zip.MimeType));
        return this;
    }

    /// <summary>Adds a ZIP built through a ZipFileBuilder callback.</summary>
    /// <param name="zipFileName">Name of the ZIP attachment.</param>
    /// <param name="configure">Callback that fills the ZipFileBuilder.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddZippedFile(string zipFileName, Action<ZipFileBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipFileName);
        ArgumentHelpers.ThrowIfNull(configure);
        var zipBuilder = ZipFileBuilder.New();
        configure(zipBuilder);
        var zipData = zipBuilder.Build();
        _bodyBuilder.Attachments.Add(zipFileName, zipData, ContentType.Parse(FileTypeInfo.Zip.MimeType));
        _attachmentMetadata.Add(new(zipFileName, zipData, FileTypeInfo.Zip.MimeType));
        return this;
    }

    /// <summary>Adds a custom header.</summary>
    /// <param name="name">Header name.</param>
    /// <param name="value">Header value.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddHeader(string name, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(value);
        _message.Headers.Add(name, value);
        return this;
    }

    /// <summary>Removes every To recipient.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearTo()
    {
        _message.To.Clear();
        return this;
    }

    /// <summary>Removes every Cc recipient.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearCc()
    {
        _message.Cc.Clear();
        return this;
    }

    /// <summary>Removes every Bcc recipient.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearBcc()
    {
        _message.Bcc.Clear();
        return this;
    }

    /// <summary>Removes every attachment.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearAttachments()
    {
        _bodyBuilder.Attachments.Clear();
        _attachmentMetadata.Clear();
        return this;
    }

    /// <summary>Reads a stream to completion into a byte array.</summary>
    /// <param name="stream">Stream to read.</param>
    /// <returns>The full contents as bytes.</returns>
    private static byte[] ReadStreamToBytes(Stream stream)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>Produces the MimeMessage.</summary>
    /// <returns>A message ready to send.</returns>
    public MimeMessage Build()
    {
        _message.Body = _bodyBuilder.ToMessageBody();
        return _message;
    }

    /// <summary>Starts a new EmailRequestBuilder.</summary>
    /// <returns>A fresh builder.</returns>
    public static EmailRequestBuilder New() => new();

    /// <summary>Diagnostic snapshot of the builder.</summary>
    /// <returns>Text covering subject and recipient count.</returns>
    public override string ToString() => $"Email: {(string.IsNullOrWhiteSpace(_message.Subject) ? "(no subject)" : _message.Subject)} to {_message.To.Count} recipient(s)";
}