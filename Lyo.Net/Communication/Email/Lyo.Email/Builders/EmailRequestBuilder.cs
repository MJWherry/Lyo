using System.Diagnostics;
using Lyo.Common.Core.Archives;
using Lyo.Common.Metadata.Records;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Email.Builders;

/// <summary>Fluent helper for an email: recipients, bodies, attachments, and custom headers. <see cref="Build" /> returns an <see cref="EmailRequest" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EmailRequestBuilder
{
    private readonly List<EmailAttachment> _attachments = [];

    private readonly List<EmailAddress> _bcc = [];

    private readonly List<EmailAddress> _cc = [];

    private readonly List<EmailHeader> _headers = [];

    private readonly List<EmailAddress> _replyTo = [];

    private readonly List<EmailAddress> _to = [];

    private EmailAddress? _from;

    private string? _htmlBody;

    private EmailPriority? _priority;

    private string? _subject;

    private string? _textBody;

    /// <summary>Adds several To recipients from an enumerable. Each address is also the display name.</summary>
    /// <param name="to">Addresses.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(IEnumerable<string> to)
    {
        ArgumentHelpers.ThrowIfNull(to);
        foreach (var address in to)
            AddTo(address);

        return this;
    }

    /// <summary>Adds several To recipients. Two string arguments bind to <see cref="AddTo(string, string?)" />.</summary>
    /// <param name="to">Addresses. Each value is used as both address and display name.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(params string[] to)
    {
        ArgumentHelpers.ThrowIfNull(to);
        foreach (var address in to)
            AddTo(address);

        return this;
    }

    /// <summary>Adds one To recipient.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name. The address is used when this is omitted.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(string email, string? name = null)
    {
        RequireEmail(email);
        _to.Add(new(email, name ?? email));
        return this;
    }

    /// <summary>Adds one To recipient.</summary>
    /// <param name="to">Address to add.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddTo(EmailAddress to)
    {
        ArgumentHelpers.ThrowIfNull(to);
        RequireEmail(to.Address);
        _to.Add(to);
        return this;
    }

    /// <summary>Adds several Cc recipients. Each address is also the display name.</summary>
    /// <param name="cc">Addresses.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(IEnumerable<string> cc)
    {
        ArgumentHelpers.ThrowIfNull(cc);
        foreach (var address in cc)
            AddCc(address);

        return this;
    }

    /// <summary>Adds several Cc recipients. Each value is used as both address and display name.</summary>
    /// <param name="cc">Addresses.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(params string[] cc)
    {
        ArgumentHelpers.ThrowIfNull(cc);
        foreach (var address in cc)
            AddCc(address);

        return this;
    }

    /// <summary>Adds one Cc recipient.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name. The address is used when this is omitted.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(string email, string? name = null)
    {
        RequireEmail(email);
        _cc.Add(new(email, name ?? email));
        return this;
    }

    /// <summary>Adds one Cc recipient.</summary>
    /// <param name="cc">Address to add.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddCc(EmailAddress cc)
    {
        ArgumentHelpers.ThrowIfNull(cc);
        RequireEmail(cc.Address);
        _cc.Add(cc);
        return this;
    }

    /// <summary>Adds several Bcc recipients. Each address is also the display name.</summary>
    /// <param name="bcc">Addresses.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(IEnumerable<string> bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc);
        foreach (var address in bcc)
            AddBcc(address);

        return this;
    }

    /// <summary>Adds several Bcc recipients. Each value is used as both address and display name.</summary>
    /// <param name="bcc">Addresses.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(params string[] bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc);
        foreach (var address in bcc)
            AddBcc(address);

        return this;
    }

    /// <summary>Adds one Bcc recipient.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name. The address is used when this is omitted.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(string email, string? name = null)
    {
        RequireEmail(email);
        _bcc.Add(new(email, name ?? email));
        return this;
    }

    /// <summary>Adds one Bcc recipient.</summary>
    /// <param name="bcc">Address to add.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddBcc(EmailAddress bcc)
    {
        ArgumentHelpers.ThrowIfNull(bcc);
        RequireEmail(bcc.Address);
        _bcc.Add(bcc);
        return this;
    }

    /// <summary>Sets the From address. A later call replaces the previous From.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name. The address is used when this is omitted.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetFrom(string email, string? name = null)
    {
        RequireEmail(email);
        _from = new(email, name ?? email);
        return this;
    }

    /// <summary>Sets From. A later call replaces the previous From.</summary>
    /// <param name="from">Address to use.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetFrom(EmailAddress from)
    {
        ArgumentHelpers.ThrowIfNull(from);
        RequireEmail(from.Address);
        _from = from;
        return this;
    }

    /// <summary>Appends a Reply-To address.</summary>
    /// <param name="email">Address.</param>
    /// <param name="name">Display name. The address is used when this is omitted.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetReplyTo(string email, string? name = null)
    {
        RequireEmail(email);
        _replyTo.Add(new(email, name ?? email));
        return this;
    }

    /// <summary>Appends a Reply-To address.</summary>
    /// <param name="replyTo">Address to append.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetReplyTo(EmailAddress replyTo)
    {
        ArgumentHelpers.ThrowIfNull(replyTo);
        RequireEmail(replyTo.Address);
        _replyTo.Add(replyTo);
        return this;
    }

    /// <summary>Sets the subject line. Empty is allowed. Null is not.</summary>
    /// <param name="subject">Subject text.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetSubject(string subject)
    {
        ArgumentHelpers.ThrowIfNull(subject);
        _subject = subject;
        return this;
    }

    /// <summary>Sets message priority.</summary>
    /// <param name="priority">Priority.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetPriority(EmailPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>Replaces the HTML body.</summary>
    /// <param name="html">HTML content.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetHtmlBody(string html)
    {
        _htmlBody = html;
        return this;
    }

    /// <summary>Replaces the plain-text body.</summary>
    /// <param name="text">Plain-text content.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder SetTextBody(string text)
    {
        _textBody = text;
        return this;
    }

    /// <summary>Appends HTML to the current HTML body.</summary>
    /// <param name="html">HTML to append.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AppendHtmlBody(string html)
    {
        _htmlBody = (_htmlBody ?? string.Empty) + html;
        return this;
    }

    /// <summary>Appends plain text to the current text body.</summary>
    /// <param name="text">Text to append.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AppendTextBody(string text)
    {
        _textBody = (_textBody ?? string.Empty) + text;
        return this;
    }

    /// <summary>Adds an attachment, including optional log metadata. Metadata stays off the wire.</summary>
    /// <param name="attachment">Attachment with name, bytes, and optional metadata.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(EmailAttachment attachment)
    {
        ArgumentHelpers.ThrowIfNull(attachment);
        _attachments.Add(attachment);
        return this;
    }

    /// <summary>Adds an attachment from bytes.</summary>
    /// <param name="fileName">Attachment file name.</param>
    /// <param name="data">File bytes.</param>
    /// <param name="contentType">Optional MIME type.</param>
    /// <param name="metadataJson">Optional JSON metadata stored only on the request.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(string fileName, byte[] data, string? contentType = null, string? metadataJson = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentHelpers.ThrowIfNull(data);
        _attachments.Add(new(fileName, data, contentType, metadataJson));
        return this;
    }

    /// <summary>Adds an attachment from a stream. The stream is read to completion.</summary>
    /// <param name="fileName">Attachment file name.</param>
    /// <param name="data">Stream of file bytes.</param>
    /// <param name="contentType">Optional MIME type.</param>
    /// <param name="metadataJson">Optional JSON metadata stored only on the request.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(string fileName, Stream data, string? contentType = null, string? metadataJson = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentHelpers.ThrowIfNull(data);
        OperationHelpers.ThrowIfNotReadable(data, $"Stream '{nameof(data)}' must be readable.");
        return AddAttachment(fileName, ReadStreamToBytes(data), contentType, metadataJson);
    }

    /// <summary>Adds several attachments from a name-to-bytes map.</summary>
    /// <param name="files">Map of file name to bytes.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachment(Dictionary<string, byte[]> files)
    {
        ArgumentHelpers.ThrowIfNull(files);
        foreach (var file in files)
            _attachments.Add(new(file.Key, file.Value));

        return this;
    }

    /// <summary>Attaches a file from disk.</summary>
    /// <param name="filePath">Path of the file.</param>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder AddAttachmentFromFile(string filePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        var fileName = Path.GetFileName(filePath);
        _attachments.Add(new(fileName, File.ReadAllBytes(filePath)));
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
        var zipData = ZipFileBuilder.Create().AddFiles(files).Build();
        _attachments.Add(new(zipFileName, zipData, FileTypeInfo.Zip.MimeType));
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
        var zipData = ZipFileBuilder.Create().AddFiles(filePaths).Build();
        _attachments.Add(new(zipFileName, zipData, FileTypeInfo.Zip.MimeType));
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
        var zipBuilder = ZipFileBuilder.Create();
        configure(zipBuilder);
        _attachments.Add(new(zipFileName, zipBuilder.Build(), FileTypeInfo.Zip.MimeType));
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
        _headers.Add(new(name, value));
        return this;
    }

    /// <summary>Removes every To recipient.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearTo()
    {
        _to.Clear();
        return this;
    }

    /// <summary>Removes every Cc recipient.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearCc()
    {
        _cc.Clear();
        return this;
    }

    /// <summary>Removes every Bcc recipient.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearBcc()
    {
        _bcc.Clear();
        return this;
    }

    /// <summary>Removes every attachment.</summary>
    /// <returns>This builder for further calls.</returns>
    public EmailRequestBuilder ClearAttachments()
    {
        _attachments.Clear();
        return this;
    }

    /// <summary>Produces the request. Later edits to this builder do not change the returned instance.</summary>
    /// <returns>A request ready to send.</returns>
    public EmailRequest Build()
        => new(
            _from?.Address, _from?.Name, Copy(_to), Copy(_cc), Copy(_bcc), _subject, _attachments.Count > 0 ? _attachments.ToArray() : null, _textBody, _htmlBody,
            _replyTo.Count > 0 ? _replyTo.ToArray() : null, _headers.Count > 0 ? _headers.ToArray() : null, _priority);

    /// <summary>Starts a new EmailRequestBuilder.</summary>
    /// <returns>A fresh builder.</returns>
    public static EmailRequestBuilder New() => new();

    /// <summary>Diagnostic snapshot of the builder.</summary>
    /// <returns>Text covering subject and recipient count.</returns>
    public override string ToString() => $"Email: {(string.IsNullOrWhiteSpace(_subject) ? "(no subject)" : _subject)} to {_to.Count} recipient(s)";

    private static void RequireEmail(string email) => FormatHelpers.ThrowIfInvalidFormat(email, RegexPatterns.EmailRegex, "Invalid email format: {0}", "Email (e.g., user@example.com)");

    private static IReadOnlyList<EmailAddress>? Copy(List<EmailAddress> addresses) => addresses.Count > 0 ? addresses.ToArray() : null;

    private static byte[] ReadStreamToBytes(Stream stream)
    {
        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
