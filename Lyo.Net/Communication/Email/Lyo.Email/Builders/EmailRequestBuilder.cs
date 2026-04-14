using System.Diagnostics;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Email.Models;
using Lyo.Exceptions;
using MimeKit;

namespace Lyo.Email.Builders;

/// <summary>Fluent builder for constructing email messages with support for recipients, attachments, and custom headers.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EmailRequestBuilder
{
    private readonly List<EmailAttachment> _attachmentMetadata = [];
    private readonly BodyBuilder _bodyBuilder = new();

    private readonly MimeMessage _message = new();

    /// <summary>Gets the attachment metadata for logging (fileName, fileStorageId, templateId, etc.). Empty if no attachments.</summary>
    public IReadOnlyList<EmailAttachment>? AttachmentMetadata => _attachmentMetadata.Count > 0 ? _attachmentMetadata : null;

    /// <summary>Adds multiple To recipients from an enumerable collection.</summary>
    /// <param name="to">Collection of email addresses. Each address will be used as both email and display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddTo(IEnumerable<string> to)
    {
        foreach (var i in to)
            AddTo(i);

        return this;
    }

    /// <summary>Adds multiple To recipients using params array.</summary>
    /// <param name="to">Array of email addresses. Each address will be used as both email and display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    /// <remarks>Note: When passing exactly 2 string arguments, this will match AddTo(string email, string name) instead. Use 3+ arguments or an array to use this overload.</remarks>
    public EmailRequestBuilder AddTo(params string[] to)
    {
        ArgumentHelpers.ThrowIfNull(to, nameof(to));
        foreach (var i in to)
            AddTo(i);

        return this;
    }

    /// <summary>Adds a To recipient with email address and display name.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="name">The display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddTo(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", nameof(email), "Email (e.g., user@example.com)");
        _message.To.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Adds a To recipient using a MailboxAddress.</summary>
    /// <param name="to">The MailboxAddress to add.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddTo(MailboxAddress to)
    {
        ArgumentHelpers.ThrowIfNull(to, nameof(to));
        _message.To.Add(to);
        return this;
    }

    /// <summary>Adds multiple Cc recipients from an enumerable collection.</summary>
    /// <param name="cc">Collection of email addresses. Each address will be used as both email and display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddCc(IEnumerable<string> cc)
    {
        ArgumentHelpers.ThrowIfNull(cc, nameof(cc));
        foreach (var i in cc)
            AddCc(i);

        return this;
    }

    /// <summary>Adds multiple Cc recipients using params array.</summary>
    /// <param name="cc">Array of email addresses. Each address will be used as both email and display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddCc(params string[] cc)
    {
        ArgumentHelpers.ThrowIfNull(cc, nameof(cc));
        foreach (var i in cc)
            AddCc(i, i);

        return this;
    }

    /// <summary>Adds a Cc recipient with email address and display name.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="name">The display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddCc(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", nameof(email), "Email (e.g., user@example.com)");
        _message.Cc.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Adds a Cc recipient using a MailboxAddress.</summary>
    /// <param name="cc">The MailboxAddress to add.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddCc(MailboxAddress cc)
    {
        ArgumentHelpers.ThrowIfNull(cc, nameof(cc));
        _message.Cc.Add(cc);
        return this;
    }

    /// <summary>Adds multiple Bcc recipients from an enumerable collection.</summary>
    /// <param name="bcc">Collection of email addresses. Each address will be used as both email and display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddBcc(IEnumerable<string> bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc, nameof(bcc));
        foreach (var i in bcc)
            AddBcc(i);

        return this;
    }

    /// <summary>Adds multiple Bcc recipients using params array.</summary>
    /// <param name="bcc">Array of email addresses. Each address will be used as both email and display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddBcc(params string[] bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc, nameof(bcc));
        foreach (var i in bcc)
            AddBcc(i);

        return this;
    }

    /// <summary>Adds a Bcc recipient with email address and display name.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="name">The display name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddBcc(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", nameof(email), "Email (e.g., user@example.com)");
        _message.Bcc.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Adds a Bcc recipient using a MailboxAddress.</summary>
    /// <param name="bcc">The MailboxAddress to add.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddBcc(MailboxAddress bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc, nameof(bcc));
        _message.Bcc.Add(bcc);
        return this;
    }

    /// <summary>Sets the From address for the email.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="name">Optional display name. If not provided, the email address will be used as the name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetFrom(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", nameof(email), "Email (e.g., user@example.com)");
        _message.From.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Sets the From address using a MailboxAddress.</summary>
    /// <param name="from">The MailboxAddress to use.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetFrom(MailboxAddress from)
    {
        ArgumentHelpers.ThrowIfNull(from, nameof(from));
        _message.From.Add(from);
        return this;
    }

    /// <summary>Sets the Reply-To address for the email.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="name">Optional display name. If not provided, the email address will be used as the name.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetReplyTo(string email, string? name = null)
    {
        FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", nameof(email), "Email (e.g., user@example.com)");
        _message.ReplyTo.Add(new MailboxAddress(name ?? email, email));
        return this;
    }

    /// <summary>Sets the Reply-To address using a MailboxAddress.</summary>
    /// <param name="replyTo">The MailboxAddress to use.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetReplyTo(MailboxAddress replyTo)
    {
        ArgumentHelpers.ThrowIfNull(replyTo, nameof(replyTo));
        _message.ReplyTo.Add(replyTo);
        return this;
    }

    /// <summary>Sets the email subject.</summary>
    /// <param name="subject">The subject text.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetSubject(string subject)
    {
        ArgumentHelpers.ThrowIfNull(subject, nameof(subject));
        _message.Subject = subject;
        return this;
    }

    /// <summary>Sets the email priority.</summary>
    /// <param name="priority">The message priority (Normal, Low, High, Urgent).</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetPriority(MessagePriority priority)
    {
        _message.Priority = priority;
        return this;
    }

    /// <summary>Sets the HTML body content. Replaces any existing HTML body.</summary>
    /// <param name="html">The HTML content.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetHtmlBody(string html)
    {
        _bodyBuilder.HtmlBody = html;
        return this;
    }

    /// <summary>Sets the plain text body content. Replaces any existing text body.</summary>
    /// <param name="text">The plain text content.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder SetTextBody(string text)
    {
        _bodyBuilder.TextBody = text;
        return this;
    }

    /// <summary>Appends HTML content to the existing HTML body.</summary>
    /// <param name="html">The HTML content to append.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AppendHtmlBody(string html)
    {
        _bodyBuilder.HtmlBody += html;
        return this;
    }

    /// <summary>Appends plain text content to the existing text body.</summary>
    /// <param name="text">The text content to append.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AppendTextBody(string text)
    {
        _bodyBuilder.TextBody += text;
        return this;
    }

    /// <summary>Adds an attachment from an EmailAttachment (includes optional metadata for logging).</summary>
    /// <param name="attachment">The attachment with file name, data, and optional metadata.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachment(EmailAttachment attachment)
    {
        ArgumentHelpers.ThrowIfNull(attachment, nameof(attachment));
        var contentType = !string.IsNullOrWhiteSpace(attachment.ContentType) ? ContentType.Parse(attachment.ContentType) : null;
        if (contentType != null)
            _bodyBuilder.Attachments.Add(attachment.FileName, attachment.Data, contentType);
        else
            _bodyBuilder.Attachments.Add(attachment.FileName, attachment.Data);

        _attachmentMetadata.Add(attachment);
        return this;
    }

    /// <summary>Adds an attachment from byte array data.</summary>
    /// <param name="fileName">The name of the attachment file.</param>
    /// <param name="data">The file data as a byte array.</param>
    /// <param name="contentType">Optional content type. If not provided, MimeKit will detect it from the file extension.</param>
    /// <param name="fileStorageId">Optional ID for correlation with external file storage (stored in logs only, not sent).</param>
    /// <param name="templateId">Optional template ID used for formatting (stored in logs only).</param>
    /// <param name="metadataJson">Optional JSON metadata for extensibility (stored in logs only).</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachment(
        string fileName,
        byte[] data,
        ContentType? contentType = null,
        string? fileStorageId = null,
        Guid? templateId = null,
        string? metadataJson = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        if (contentType != null)
            _bodyBuilder.Attachments.Add(fileName, data, contentType);
        else
            _bodyBuilder.Attachments.Add(fileName, data);

        _attachmentMetadata.Add(new(fileName, data, fileStorageId, templateId, contentType?.ToString(), metadataJson));
        return this;
    }

    /// <summary>Adds an attachment from a stream.</summary>
    /// <param name="fileName">The name of the attachment file.</param>
    /// <param name="data">The file data as a stream.</param>
    /// <param name="contentType">Optional content type. If not provided, MimeKit will detect it from the file extension.</param>
    /// <param name="fileStorageId">Optional ID for correlation with external file storage (stored in logs only, not sent).</param>
    /// <param name="templateId">Optional template ID used for formatting (stored in logs only).</param>
    /// <param name="metadataJson">Optional JSON metadata for extensibility (stored in logs only).</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachment(
        string fileName,
        Stream data,
        ContentType? contentType = null,
        string? fileStorageId = null,
        Guid? templateId = null,
        string? metadataJson = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        OperationHelpers.ThrowIfNotReadable(data, $"Stream '{nameof(data)}' must be readable.");
        var bytes = ReadStreamToBytes(data);
        if (contentType != null)
            _bodyBuilder.Attachments.Add(fileName, bytes, contentType);
        else
            _bodyBuilder.Attachments.Add(fileName, bytes);

        _attachmentMetadata.Add(new(fileName, bytes, fileStorageId, templateId, contentType?.ToString(), metadataJson));
        return this;
    }

    /// <summary>Adds multiple attachments from a dictionary of file names and byte arrays.</summary>
    /// <param name="files">Dictionary where key is the file name and value is the file data.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachment(Dictionary<string, byte[]> files)
    {
        ArgumentHelpers.ThrowIfNull(files, nameof(files));
        foreach (var file in files) {
            _bodyBuilder.Attachments.Add(file.Key, file.Value);
            _attachmentMetadata.Add(new(file.Key, file.Value));
        }

        return this;
    }

    /// <summary>Adds an attachment from a file path.</summary>
    /// <param name="filePath">The path to the file to attach.</param>
    /// <param name="fileStorageId">Optional ID for correlation with external file storage (stored in logs only).</param>
    /// <param name="templateId">Optional template ID (stored in logs only).</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachmentFromFile(string filePath, string? fileStorageId = null, Guid? templateId = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
        var fileName = Path.GetFileName(filePath);
        var data = File.ReadAllBytes(filePath);
        _bodyBuilder.Attachments.Add(fileName, data);
        _attachmentMetadata.Add(new(fileName, data, fileStorageId, templateId));
        return this;
    }

    /// <summary>Adds multiple files as a single ZIP attachment.</summary>
    /// <param name="zipFileName">The name for the ZIP file attachment.</param>
    /// <param name="files">Dictionary where key is the file name within the ZIP and value is the file data.</param>
    /// <param name="fileStorageId">Optional ID for correlation with external file storage (stored in logs only).</param>
    /// <param name="templateId">Optional template ID (stored in logs only).</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachmentsAsZip(string zipFileName, Dictionary<string, byte[]> files, string? fileStorageId = null, Guid? templateId = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipFileName, nameof(zipFileName));
        ArgumentHelpers.ThrowIfNull(files, nameof(files));
        var zipData = ZipFileBuilder.New().AddFiles(files).Build();
        _bodyBuilder.Attachments.Add(zipFileName, zipData, ContentType.Parse(FileTypeInfo.Zip.MimeType));
        _attachmentMetadata.Add(new(zipFileName, zipData, fileStorageId, templateId, FileTypeInfo.Zip.MimeType));
        return this;
    }

    /// <summary>Adds multiple files from file paths as a single ZIP attachment.</summary>
    /// <param name="zipFileName">The name for the ZIP file attachment.</param>
    /// <param name="filePaths">Array of file paths to include in the ZIP.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddAttachmentsAsZip(string zipFileName, params string[] filePaths)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipFileName, nameof(zipFileName));
        ArgumentHelpers.ThrowIfNull(filePaths, nameof(filePaths));
        var zipData = ZipFileBuilder.New().AddFiles(filePaths).Build();
        _bodyBuilder.Attachments.Add(zipFileName, zipData, ContentType.Parse(FileTypeInfo.Zip.MimeType));
        _attachmentMetadata.Add(new(zipFileName, zipData, null, null, FileTypeInfo.Zip.MimeType));
        return this;
    }

    /// <summary>Adds a ZIP attachment configured using a ZipFileBuilder action.</summary>
    /// <param name="zipFileName">The name for the ZIP file attachment.</param>
    /// <param name="configure">Action to configure the ZipFileBuilder with files and options.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddZippedFile(string zipFileName, Action<ZipFileBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipFileName, nameof(zipFileName));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var zipBuilder = ZipFileBuilder.New();
        configure(zipBuilder);
        var zipData = zipBuilder.Build();
        _bodyBuilder.Attachments.Add(zipFileName, zipData, ContentType.Parse(FileTypeInfo.Zip.MimeType));
        _attachmentMetadata.Add(new(zipFileName, zipData, null, null, FileTypeInfo.Zip.MimeType));
        return this;
    }

    /// <summary>Adds a custom header to the email message.</summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder AddHeader(string name, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        _message.Headers.Add(name, value);
        return this;
    }

    /// <summary>Clears all To recipients.</summary>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder ClearTo()
    {
        _message.To.Clear();
        return this;
    }

    /// <summary>Clears all Cc recipients.</summary>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder ClearCc()
    {
        _message.Cc.Clear();
        return this;
    }

    /// <summary>Clears all Bcc recipients.</summary>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder ClearBcc()
    {
        _message.Bcc.Clear();
        return this;
    }

    /// <summary>Clears all attachments.</summary>
    /// <returns>The EmailRequestBuilder instance for method chaining.</returns>
    public EmailRequestBuilder ClearAttachments()
    {
        _bodyBuilder.Attachments.Clear();
        _attachmentMetadata.Clear();
        return this;
    }

    private static byte[] ReadStreamToBytes(Stream stream)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>Builds and returns the MimeMessage.</summary>
    /// <returns>A MimeMessage ready to be sent.</returns>
    public MimeMessage Build()
    {
        _message.Body = _bodyBuilder.ToMessageBody();
        return _message;
    }

    /// <summary>Creates a new EmailRequestBuilder instance.</summary>
    /// <returns>A new EmailRequestBuilder instance.</returns>
    public static EmailRequestBuilder New() => new();

    public override string ToString() => $"Email: {(string.IsNullOrWhiteSpace(_message.Subject) ? "(no subject)" : _message.Subject)} to {_message.To.Count} recipient(s)";
}