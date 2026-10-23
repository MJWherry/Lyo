using System.Globalization;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGridAddress = SendGrid.Helpers.Mail.EmailAddress;

namespace Lyo.Email.SendGrid;

/// <summary>Twilio SendGrid implementation of <see cref="IEmailService" />. One API call per message.</summary>
public sealed class SendGridEmailService : EmailServiceBase
{
    private readonly ISendGridClient _client;

    private readonly SendGridEmailOptions _options;

    /// <summary>Constructs the SendGrid sender.</summary>
    /// <param name="options">SendGrid settings. Call <see cref="SendGridEmailOptions.Validate" /> before passing them in. The DI helpers do that.</param>
    /// <param name="client">SendGrid client. The DI helper registers one singleton <see cref="SendGridClient" />.</param>
    /// <param name="logger">Logger. A null logger is used when this is omitted.</param>
    /// <param name="metrics">Metrics sink. Ignored unless <see cref="EmailServiceOptions.EnableMetrics" /> is true.</param>
    public SendGridEmailService(SendGridEmailOptions options, ISendGridClient client, ILogger<SendGridEmailService>? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(client);
        _options = options;
        _client = client;
    }

    /// <inheritdoc />
    protected override async Task<EmailResult> SendCoreAsync(EmailRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = ToSendGridMessage(request);
        var response = await _client.SendEmailAsync(message, ct).ConfigureAwait(false);
        var providerResponse = await ReadProviderResponseAsync(response).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return EmailResult.FromError(providerResponse, request) with { ProviderResponse = providerResponse };

        return EmailResult.FromSuccess(request, providerResponse, ReadMessageId(response), DateTime.UtcNow);
    }

    /// <inheritdoc />
    protected override async Task<bool> TestConnectionCoreAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var response = await _client.RequestAsync(method: BaseClient.Method.GET, urlPath: "scopes", cancellationToken: ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private SendGridMessage ToSendGridMessage(EmailRequest request)
    {
        var message = new SendGridMessage();
        var fromAddress = string.IsNullOrWhiteSpace(request.FromAddress) ? _options.DefaultFromAddress : request.FromAddress!;
        var fromName = string.IsNullOrWhiteSpace(request.FromAddress) ? _options.DefaultFromName : request.FromName ?? fromAddress;
        message.SetFrom(new(fromAddress, fromName));
        AddRecipients(message, request);
        if (request.Subject != null)
            message.Subject = request.Subject;

        if (!string.IsNullOrEmpty(request.TextBody))
            message.AddContent(MimeType.Text, request.TextBody);

        if (!string.IsNullOrEmpty(request.HtmlBody))
            message.AddContent(MimeType.Html, request.HtmlBody);

        if (request.Attachments != null) {
            foreach (var attachment in request.Attachments)
                message.AddAttachment(attachment.FileName, Convert.ToBase64String(attachment.Data), attachment.ContentType, "attachment");
        }

        ApplyReplyTo(message, request.ReplyTo);
        ApplyHeaders(message, request);
        message.SetClickTracking(false, false);
        message.SetOpenTracking(false);
        message.SetSubscriptionTracking(false);
        return message;
    }

    private static void AddRecipients(SendGridMessage message, EmailRequest request)
    {
        if (request.ToAddresses != null) {
            foreach (var address in request.ToAddresses)
                message.AddTo(ToSendGrid(address));
        }

        if (request.CcAddresses != null) {
            foreach (var address in request.CcAddresses)
                message.AddCc(ToSendGrid(address));
        }

        if (request.BccAddresses != null) {
            foreach (var address in request.BccAddresses)
                message.AddBcc(ToSendGrid(address));
        }
    }

    private static void ApplyReplyTo(SendGridMessage message, IReadOnlyList<Models.EmailAddress>? replyTo)
    {
        if (replyTo is not { Count: > 0 })
            return;

        if (replyTo.Count == 1) {
            message.ReplyTo = ToSendGrid(replyTo[0]);
            return;
        }

        message.ReplyTos = [.. replyTo.Select(ToSendGrid)];
    }

    private static void ApplyHeaders(SendGridMessage message, EmailRequest request)
    {
        var writePriority = request.Priority is EmailPriority.NonUrgent or EmailPriority.Urgent;
        if (request.Headers is not { Count: > 0 } && !writePriority)
            return;

        message.Headers ??= new(StringComparer.OrdinalIgnoreCase);
        if (request.Headers != null) {
            foreach (var header in request.Headers)
                message.Headers[header.Name] = header.Value;
        }

        if (writePriority)
            message.Headers["Priority"] = request.Priority == EmailPriority.Urgent ? "urgent" : "non-urgent";
    }

    private static SendGridAddress ToSendGrid(Models.EmailAddress address) => new(address.Address, address.Name ?? address.Address);

    private static string? ReadMessageId(Response response)
    {
        if (response.Headers == null || !response.Headers.TryGetValues("X-Message-Id", out var values))
            return null;

        return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
    }

    private static async Task<string> ReadProviderResponseAsync(Response response)
    {
        var code = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
        if (response.Body is null)
            return code;

        var body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);
        return string.IsNullOrEmpty(body) ? code : code + " " + body;
    }
}
