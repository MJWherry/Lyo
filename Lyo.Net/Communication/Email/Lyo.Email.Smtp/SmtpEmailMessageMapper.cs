using Lyo.Email.Models;
using MimeKit;

namespace Lyo.Email.Smtp;

/// <summary>Maps an <see cref="EmailRequest" /> onto a MimeKit message. Display names, content types, reply-to, headers, and priority are kept.</summary>
internal static class SmtpEmailMessageMapper
{
    /// <summary>Builds the MIME message. A null From on the request uses the options default on this message only.</summary>
    /// <param name="request">Request to convert.</param>
    /// <param name="options">SMTP options, including the default sender.</param>
    /// <returns>The MIME message.</returns>
    internal static MimeMessage ToMimeMessage(EmailRequest request, SmtpEmailOptions options)
    {
        var message = new MimeMessage();
        var fromAddress = string.IsNullOrWhiteSpace(request.FromAddress) ? options.DefaultFromAddress : request.FromAddress!;
        var fromName = string.IsNullOrWhiteSpace(request.FromAddress) ? options.DefaultFromName : request.FromName ?? fromAddress;
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        Add(message.To, request.ToAddresses);
        Add(message.Cc, request.CcAddresses);
        Add(message.Bcc, request.BccAddresses);
        if (request.ReplyTo != null) {
            foreach (var replyTo in request.ReplyTo)
                message.ReplyTo.Add(Mailbox(replyTo));
        }

        if (request.Subject != null)
            message.Subject = request.Subject;

        if (request.Priority is { } priority)
            message.Priority = ToMimePriority(priority);

        if (request.Headers != null) {
            foreach (var header in request.Headers)
                message.Headers.Add(header.Name, header.Value);
        }

        var body = new BodyBuilder { HtmlBody = request.HtmlBody, TextBody = request.TextBody };
        if (request.Attachments != null) {
            foreach (var attachment in request.Attachments) {
                var contentType = attachment.ContentType!;
                if (!string.IsNullOrWhiteSpace(contentType))
                    body.Attachments.Add(attachment.FileName, attachment.Data, ContentType.Parse(contentType));
                else
                    body.Attachments.Add(attachment.FileName, attachment.Data);
            }
        }

        message.Body = body.ToMessageBody();
        return message;
    }

    private static void Add(InternetAddressList list, IReadOnlyList<EmailAddress>? addresses)
    {
        if (addresses == null)
            return;

        foreach (var address in addresses)
            list.Add(Mailbox(address));
    }

    private static MailboxAddress Mailbox(EmailAddress address) => new(address.Name ?? address.Address, address.Address);

    private static MessagePriority ToMimePriority(EmailPriority priority)
        => priority switch {
            EmailPriority.NonUrgent => MessagePriority.NonUrgent,
            EmailPriority.Urgent => MessagePriority.Urgent,
            _ => MessagePriority.Normal
        };
}
