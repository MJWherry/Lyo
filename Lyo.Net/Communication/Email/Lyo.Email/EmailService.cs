using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;
using Lyo.Common.Core.Extensions;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Result;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;

namespace Lyo.Email;

/// <summary>MailKit SMTP implementation of the email service.</summary>
/// <remarks>
/// <para>Thread-safe; multiple threads may call it at once.</para>
/// <para>Each call opens its own SMTP connection so concurrent work does not share mutable client state.</para>
/// <para>Instance fields are readonly and the service keeps no mutable state across calls.</para>
/// </remarks>
public sealed class EmailService : IEmailService
{
    private readonly EmailServiceOptions _emailServiceOptions;

    private readonly ILogger<EmailService> _logger;

    /// <summary>Map of logical metric slots to concrete metric names.</summary>
    private readonly Dictionary<string, string> _metricNames;

    private readonly IMetrics _metrics;

    /// <summary>MailKit SMTP implementation of the email service.</summary>
    /// <remarks>
    /// <para>Thread-safe; multiple threads may call it at once.</para>
    /// <para>Each call opens its own SMTP connection so concurrent work does not share mutable client state.</para>
    /// <para>Instance fields are readonly and the service keeps no mutable state across calls.</para>
    /// </remarks>
    public EmailService(EmailServiceOptions emailServiceOptions, ILogger<EmailService>? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(emailServiceOptions);
        _emailServiceOptions = emailServiceOptions;
        _logger = logger ?? NullLogger<EmailService>.Instance;
        _metrics = emailServiceOptions.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = new() {
            { nameof(Constants.Metrics.SendDuration), Constants.Metrics.SendDuration },
            { nameof(Constants.Metrics.SendSuccess), Constants.Metrics.SendSuccess },
            { nameof(Constants.Metrics.SendFailure), Constants.Metrics.SendFailure },
            { nameof(Constants.Metrics.SendCancelled), Constants.Metrics.SendCancelled },
            { nameof(Constants.Metrics.SendLastDurationMs), Constants.Metrics.SendLastDurationMs },
            { nameof(Constants.Metrics.BulkSendDuration), Constants.Metrics.BulkSendDuration },
            { nameof(Constants.Metrics.BulkSendTotal), Constants.Metrics.BulkSendTotal },
            { nameof(Constants.Metrics.BulkSendSuccess), Constants.Metrics.BulkSendSuccess },
            { nameof(Constants.Metrics.BulkSendFailure), Constants.Metrics.BulkSendFailure },
            { nameof(Constants.Metrics.BulkSendLastDurationMs), Constants.Metrics.BulkSendLastDurationMs },
            { nameof(Constants.Metrics.SmtpConnectDuration), Constants.Metrics.SmtpConnectDuration },
            { nameof(Constants.Metrics.SmtpAuthenticateDuration), Constants.Metrics.SmtpAuthenticateDuration },
            { nameof(Constants.Metrics.TestConnectionDuration), Constants.Metrics.TestConnectionDuration },
            { nameof(Constants.Metrics.TestConnectionSuccess), Constants.Metrics.TestConnectionSuccess },
            { nameof(Constants.Metrics.TestConnectionFailure), Constants.Metrics.TestConnectionFailure }
        };
    }

    /// <inheritdoc />
    public async Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.SendDuration)]);
        var sw = Stopwatch.StartNew();
        var message = requestBuilder.Build();
        if (!string.IsNullOrWhiteSpace(fromAddress)) {
            message.From.Clear();
            message.From.Add(new MailboxAddress(fromName ?? fromAddress, fromAddress));
        }

        var recipients = GetRecipientSummary(message);
        var emailRequest = ExtractEmailMetadata(message, fromAddress, fromName);
        if (requestBuilder.AttachmentMetadata != null) {
            ArgumentHelpers.ThrowIfNotInRange(
                requestBuilder.AttachmentMetadata.Count, 0, _emailServiceOptions.MaxAttachmentCountPerEmail, nameof(requestBuilder),
                $"Maximum of {_emailServiceOptions.MaxAttachmentCountPerEmail} attachments allowed per email. Provided: {requestBuilder.AttachmentMetadata.Count}");

            emailRequest = emailRequest with { Attachments = requestBuilder.AttachmentMetadata };
        }

        OnEmailSending(emailRequest);
        _logger.LogInformation("Sending email to {Recipients} with subject: {Subject}", recipients, message.Subject);

        async ValueTask<string> SendEmailCore(CancellationToken ct2)
        {
            using var client = await CreateAndConnectSmtpClientAsync(ct2).ConfigureAwait(false);
            var result = await client.SendAsync(message, ct2).ConfigureAwait(false);
            try {
                if (client.IsConnected) {
                    await client.DisconnectAsync(true, ct2).ConfigureAwait(false);
                    _logger.LogDebug("Disconnected from SMTP server");
                }
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error disconnecting from SMTP server");
            }

            return result;
        }

        try {
            var result = await SendEmailCore(ct).ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("Email sent successfully to {Recipients} in {Elapsed}ms. Result: {Result}", recipients, sw.ElapsedMilliseconds, result);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendSuccess)]);
            _metrics.RecordGauge(_metricNames[nameof(Constants.Metrics.SendLastDurationMs)], sw.ElapsedMilliseconds);
            var emailResult = EmailResult.FromSuccess(emailRequest, result, message.MessageId, DateTime.UtcNow);
            OnEmailSent(emailResult);
            return emailResult;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            var errorMessage = $"Email send operation was cancelled after {sw.ElapsedMilliseconds}ms. Subject: '{message.Subject}', Recipients: {recipients}";
            _logger.LogWarning(ex, errorMessage);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendCancelled)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.SendDuration)], ex);
            var cancelledResult = EmailResult.FromException(ex, emailRequest, EmailErrorCodes.OperationCancelled);
            OnEmailSent(cancelledResult);
            return cancelledResult;
        }
        catch (Exception ex) {
            sw.Stop();
            var errorMessage = $"Failed to send email to {recipients} after {sw.ElapsedMilliseconds}ms. Subject: '{message.Subject}'. Error: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendFailure)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.SendDuration)], ex);

            // Prefer a more specific message for known exception types
            var userFriendlyMessage = ex switch {
                SocketException => "Unable to connect to SMTP server. Please check your network connection and SMTP server settings.",
                TimeoutException => "SMTP server connection timed out. The server may be busy or unreachable.",
                IOException => "An I/O error occurred while sending the email. Please try again.",
                AuthenticationException => "SMTP authentication failed. Please verify your username and password.",
                var _ => $"An error occurred while sending the email: {ex.Message}"
            };

            var failureResult = EmailResult.FromError(userFriendlyMessage, emailRequest, ex);
            OnEmailSent(failureResult);
            return failureResult;
        }
    }

    /// <inheritdoc />
    public async Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default)
    {
        var message = requestBuilder.Build();
        if (message.From.Any())
            return await SendEmailAsync(requestBuilder, string.Empty, null, ct).ConfigureAwait(false);

        return await SendEmailAsync(requestBuilder, _emailServiceOptions.DefaultFromAddress, _emailServiceOptions.DefaultFromName, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<EmailRequest>> SendEmailAsync(EmailRequest request, CancellationToken ct = default)
    {
        if (request.Attachments != null) {
            ArgumentHelpers.ThrowIfNotInRange(
                request.Attachments.Count, 0, _emailServiceOptions.MaxAttachmentCountPerEmail, nameof(request),
                $"Maximum of {_emailServiceOptions.MaxAttachmentCountPerEmail} attachments allowed per email. Provided: {request.Attachments.Count}");
        }

        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.SendDuration)]);
        var sw = Stopwatch.StartNew();
        var message = BuildMimeMessageFromRequest(request);
        var recipients = GetRecipientSummary(message);
        OnEmailSending(request);
        _logger.LogInformation("Sending email to {Recipients} with subject: {Subject}", recipients, message.Subject);

        async ValueTask<string> SendEmailCore(CancellationToken ct2)
        {
            using var client = await CreateAndConnectSmtpClientAsync(ct2).ConfigureAwait(false);
            var result = await client.SendAsync(message, ct2).ConfigureAwait(false);
            try {
                if (client.IsConnected) {
                    await client.DisconnectAsync(true, ct2).ConfigureAwait(false);
                    _logger.LogDebug("Disconnected from SMTP server");
                }
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error disconnecting from SMTP server");
            }

            return result;
        }

        try {
            var result = await SendEmailCore(ct).ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("Email sent successfully to {Recipients} in {Elapsed}ms. Result: {Result}", recipients, sw.ElapsedMilliseconds, result);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendSuccess)]);
            _metrics.RecordGauge(_metricNames[nameof(Constants.Metrics.SendLastDurationMs)], sw.ElapsedMilliseconds);
            var emailResult = EmailResult.FromSuccess(request, result, message.MessageId, DateTime.UtcNow);
            OnEmailSent(emailResult);
            return emailResult;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            var errorMessage = $"Email send operation was cancelled after {sw.ElapsedMilliseconds}ms. Subject: '{message.Subject}', Recipients: {recipients}";
            _logger.LogWarning(ex, errorMessage);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendCancelled)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.SendDuration)], ex);
            var cancelledResult = EmailResult.FromException(ex, request, EmailErrorCodes.OperationCancelled);
            OnEmailSent(cancelledResult);
            return cancelledResult;
        }
        catch (Exception ex) {
            sw.Stop();
            var errorMessage = $"Failed to send email to {recipients} after {sw.ElapsedMilliseconds}ms. Subject: '{message.Subject}'. Error: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendFailure)]);
            _metrics.RecordError(_metricNames[nameof(Constants.Metrics.SendDuration)], ex);

            // Prefer a more specific message for known exception types
            var failureResult = EmailResult.FromException(ex, request, EmailErrorCodes.SendFailed);
            OnEmailSent(failureResult);
            return failureResult;
        }
    }

    /// <inheritdoc />
    /// <remarks>The whole bulk batch shares one SMTP connection. When pooling is on, that connection comes from the pool.</remarks>
    public async Task<IReadOnlyList<Result<EmailRequest>>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default)
    {
        var builderList = builders.ToList();
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.BulkSendDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(builderList.Count, 0, _emailServiceOptions.MaxBulkEmailLimit, nameof(builders));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk request exceeds maximum limit of {_emailServiceOptions.MaxBulkEmailLimit} messages. Requested: {builderList.Count}";
            _logger.LogError(error);
            throw;
        }

        _logger.LogInformation("Starting bulk email send for {Count} emails", builderList.Count);
        var results = new ConcurrentBag<Result<EmailRequest>>();
        var messageList = new List<EmailRequest>();
        var builderMessageMap = new Dictionary<EmailRequestBuilder, (EmailRequest? Message, Exception? BuildException)>();
        foreach (var builder in builderList) {
            try {
                var mimeMessage = builder.Build();
                var emailMessage = ExtractEmailMetadata(mimeMessage, _emailServiceOptions.DefaultFromAddress, _emailServiceOptions.DefaultFromName);
                if (builder.AttachmentMetadata != null) {
                    ArgumentHelpers.ThrowIfNotInRange(
                        builder.AttachmentMetadata.Count, 0, _emailServiceOptions.MaxAttachmentCountPerEmail, nameof(builders),
                        $"Maximum of {_emailServiceOptions.MaxAttachmentCountPerEmail} attachments allowed per email. Builder had: {builder.AttachmentMetadata.Count}");

                    emailMessage = emailMessage with { Attachments = builder.AttachmentMetadata };
                }

                messageList.Add(emailMessage);
                builderMessageMap[builder] = (emailMessage, null);
            }
            catch (Exception ex) {
                builderMessageMap[builder] = (null, ex);
            }
        }

        if (messageList.Count > 0)
            OnBulkSending(messageList);

        foreach (var kvp in builderMessageMap) {
            var (_, buildException) = kvp.Value;
            if (buildException == null)
                continue;

            var failedMessage = new EmailRequest(null, null, ["unknown"], null, null, "Failed to build");
            var errorResult = EmailResult.FromException(buildException, failedMessage, EmailErrorCodes.BuildFailed);
            results.Add(errorResult);
        }

        if (builderList.Count == 0) {
            sw.Stop();
            var r = results.ToList();
            _logger.LogInformation("Bulk email send completed: 0/0 successful in {Elapsed}ms", sw.ElapsedMilliseconds);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendTotal)], 0);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendSuccess)], 0);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendFailure)], 0);
            _metrics.RecordGauge(_metricNames[nameof(Constants.Metrics.BulkSendLastDurationMs)], sw.ElapsedMilliseconds);
            OnBulkEmailCompleted(r);
            return r;
        }

        SmtpClient? client = null;
        try {
            client = await CreateAndConnectSmtpClientAsync(ct).ConfigureAwait(false);
            foreach (var builder in builderList) {
                if (ct.IsCancellationRequested) {
                    _logger.LogWarning("Bulk email send cancelled after {Sent} of {Total} emails", results.Count, builderList.Count);
                    break;
                }

                var (_, buildException) = builderMessageMap[builder];
                if (buildException != null)
                    continue;

                MimeMessage message;
                try {
                    message = builder.Build();
                }
                catch (Exception ex) {
                    var failedMessage = new EmailRequest(null, null, ["unknown"], null, null, "Failed to build");
                    var errorResult = EmailResult.FromException(ex, failedMessage, EmailErrorCodes.BuildFailed);
                    results.Add(errorResult);
                    continue;
                }

                if (!message.From.Any()) {
                    message.From.Clear();
                    message.From.Add(new MailboxAddress(_emailServiceOptions.DefaultFromName, _emailServiceOptions.DefaultFromAddress));
                }

                var recipients = GetRecipientSummary(message);
                var emailRequest = ExtractEmailMetadata(message, _emailServiceOptions.DefaultFromAddress, _emailServiceOptions.DefaultFromName);
                if (builder.AttachmentMetadata != null)
                    emailRequest = emailRequest with { Attachments = builder.AttachmentMetadata };

                OnEmailSending(emailRequest);
                var messageSw = Stopwatch.StartNew();
                try {
                    var result = await client.SendAsync(message, ct).ConfigureAwait(false);
                    messageSw.Stop();
                    _logger.LogInformation("Email sent successfully to {Recipients} in {Elapsed}ms. Result: {Result}", recipients, messageSw.ElapsedMilliseconds, result);
                    _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendSuccess)]);
                    var emailResult = EmailResult.FromSuccess(emailRequest, result, message.MessageId, DateTime.UtcNow);
                    results.Add(emailResult);
                    OnEmailSent(emailResult);
                }
                catch (OperationCanceledException ex) {
                    messageSw.Stop();
                    _logger.LogWarning("Email send operation was cancelled after {Elapsed}ms", messageSw.ElapsedMilliseconds);
                    _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendCancelled)]);
                    var cancelledResult = EmailResult.FromException(ex, emailRequest, EmailErrorCodes.OperationCancelled);
                    results.Add(cancelledResult);
                    OnEmailSent(cancelledResult);
                    break;
                }
                catch (Exception ex) {
                    messageSw.Stop();
                    _logger.LogError(ex, "Failed to send email to {Recipients} after {Elapsed}ms. Subject: {Subject}", recipients, messageSw.ElapsedMilliseconds, message.Subject);
                    _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendFailure)]);
                    var failureResult = EmailResult.FromException(ex, emailRequest, EmailErrorCodes.SendFailed);
                    results.Add(failureResult);
                    OnEmailSent(failureResult);
                }
            }
        }
        catch (OperationCanceledException ex) {
            _logger.LogWarning("Bulk email send cancelled before connection established");
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendCancelled)]);
            foreach (var builder in builderList) {
                var (message, buildException) = builderMessageMap[builder];
                if (buildException != null)
                    continue;

                if (message != null) {
                    var cancelledResult = EmailResult.FromException(ex, message, EmailErrorCodes.OperationCancelled);
                    results.Add(cancelledResult);
                }
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Bulk email send failed during connection establishment");
            _metrics.IncrementCounter(Constants.Metrics.BulkSendFailure);
            foreach (var builder in builderList) {
                var (message, buildException) = builderMessageMap[builder];
                if (buildException != null || message == null)
                    continue;

                var failureResult = EmailResult.FromException(ex, message, EmailErrorCodes.SendFailed);
                results.Add(failureResult);
            }
        }
        finally {
            if (client != null) {
                try {
                    if (client.IsConnected) {
                        await client.DisconnectAsync(true, ct).ConfigureAwait(false);
                        _logger.LogDebug("Disconnected from SMTP server");
                    }
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Error disconnecting from SMTP server");
                }
                finally {
                    client.Dispose();
                }
            }
        }

        sw.Stop();
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.IsSuccess);
        var failureCount = builderList.Count - successCount;
        _logger.LogInformation("Bulk email send completed: {Success}/{Total} successful in {Elapsed}ms", successCount, builderList.Count, sw.ElapsedMilliseconds);
        _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendTotal)], builderList.Count);
        _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendSuccess)], successCount);
        _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendFailure)], failureCount);
        _metrics.RecordGauge(_metricNames[nameof(Constants.Metrics.BulkSendLastDurationMs)], sw.ElapsedMilliseconds);
        OnBulkEmailCompleted(resultsList);
        return resultsList;
    }

    /// <inheritdoc />
    public async Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var builders = bulkRequestBuilder.Build().ToList();
        var results = await SendBulkEmailAsync(builders, ct).ConfigureAwait(false);
        sw.Stop();
        return BulkResult<EmailRequest>.FromResults(results);
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(_metricNames[nameof(Constants.Metrics.TestConnectionDuration)]);
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Testing SMTP connection to {Host}:{Port}", _emailServiceOptions.Host, _emailServiceOptions.Port);

        async ValueTask<bool> TestConnectionCore(CancellationToken ct2)
        {
            using var client = await CreateAndConnectSmtpClientAsync(ct2).ConfigureAwait(false);
            await client.DisconnectAsync(true, ct2).ConfigureAwait(false);
            return true;
        }

        try {
            var result = await TestConnectionCore(ct).ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("SMTP connection test successful");
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionSuccess)]);
            OnConnectionTested(true, sw.Elapsed);
            return result;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            _logger.LogWarning("SMTP connection test was cancelled after {Elapsed}ms", sw.ElapsedMilliseconds);
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionFailure)]);
            _metrics.RecordError(Constants.Metrics.TestConnectionDuration, ex);
            OnConnectionTested(false, sw.Elapsed, ex);
            throw;
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "SMTP connection test failed");
            _metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionFailure)]);
            _metrics.RecordError(Constants.Metrics.TestConnectionDuration, ex);
            OnConnectionTested(false, sw.Elapsed, ex);
            return false;
        }
    }

    /// <summary>Raised just before a send begins.</summary>
    public event EventHandler<EmailSendingEventArgs>? EmailSending;

    /// <summary>Raised after a send finishes, whether it succeeded or failed.</summary>
    public event EventHandler<EmailSentEventArgs>? EmailSent;

    /// <summary>Raised just before a bulk send begins.</summary>
    public event EventHandler<EmailBulkSendingEventArgs>? BulkSending;

    /// <summary>Raised after a bulk send finishes.</summary>
    public event EventHandler<BulkEmailSentEventArgs>? BulkEmailSent;

    /// <summary>Raised after a connection probe finishes.</summary>
    public event EventHandler<ConnectionTestedEventArgs>? ConnectionTested;

    /// <summary>Opens a new SMTP client, connects, and authenticates.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A connected, authenticated SMTP client.</returns>
    private async Task<SmtpClient> CreateAndConnectSmtpClientAsync(CancellationToken ct = default)
    {
        var client = new SmtpClient();
        var connectSw = Stopwatch.StartNew();
        _logger.LogDebug("Connecting to SMTP server {Host}:{Port}", _emailServiceOptions.Host, _emailServiceOptions.Port);
        await client.ConnectAsync(_emailServiceOptions.Host, _emailServiceOptions.Port, _emailServiceOptions.UseSsl, ct).ConfigureAwait(false);
        connectSw.Stop();
        _metrics.RecordTiming(_metricNames[nameof(Constants.Metrics.SmtpConnectDuration)], connectSw.Elapsed);
        var authSw = Stopwatch.StartNew();
        if (!_emailServiceOptions.Username.IsNullOrWhitespace() && !_emailServiceOptions.Password.IsNullOrWhitespace()) {
            _logger.LogDebug("Authenticating as {Username}", _emailServiceOptions.Username);
            await client.AuthenticateAsync(_emailServiceOptions.Username!, _emailServiceOptions.Password!, ct).ConfigureAwait(false);
            authSw.Stop();
            _metrics.RecordTiming(_metricNames[nameof(Constants.Metrics.SmtpAuthenticateDuration)], authSw.Elapsed);
        }

        return client;
    }

    /// <summary>Short recipient summary for logs.</summary>
    /// <param name="message">Message whose recipients are counted.</param>
    /// <returns>Formatted counts such as "2 To, 1 Cc".</returns>
    private static string GetRecipientSummary(MimeMessage message)
    {
        var toCount = message.To.Count;
        var ccCount = message.Cc.Count;
        var bccCount = message.Bcc.Count;
        var parts = new List<string>();
        if (toCount > 0)
            parts.Add($"{toCount} To");

        if (ccCount > 0)
            parts.Add($"{ccCount} Cc");

        if (bccCount > 0)
            parts.Add($"{bccCount} Bcc");

        return string.Join(", ", parts);
    }

    /// <summary>Turns an EmailRequest into a MimeMessage.</summary>
    /// <param name="request">Request to convert.</param>
    /// <returns>The built MimeMessage.</returns>
    internal MimeMessage BuildMimeMessageFromRequest(EmailRequest request)
    {
        var message = new MimeMessage();
        var bodyBuilder = new BodyBuilder();

        // Apply From
        message.From.Add(
            !request.FromAddress.IsNullOrWhitespace()
                ? new MailboxAddress(request.FromName ?? request.FromAddress, request.FromAddress)
                : new MailboxAddress(_emailServiceOptions.DefaultFromName, _emailServiceOptions.DefaultFromAddress));

        // Apply To
        if (request.ToAddresses != null) {
            foreach (var to in request.ToAddresses)
                message.To.Add(new MailboxAddress(to, to));
        }

        // Apply Cc
        if (request.CcAddresses != null) {
            foreach (var cc in request.CcAddresses)
                message.Cc.Add(new MailboxAddress(cc, cc));
        }

        // Apply Bcc
        if (request.BccAddresses != null) {
            foreach (var bcc in request.BccAddresses)
                message.Bcc.Add(new MailboxAddress(bcc, bcc));
        }

        // Apply Subject
        if (!request.Subject.IsNullOrWhitespace())
            message.Subject = request.Subject;

        // Attach files
        if (request.Attachments != null) {
            ArgumentHelpers.ThrowIfNotInRange(
                request.Attachments.Count, 0, _emailServiceOptions.MaxAttachmentCountPerEmail, nameof(request.Attachments),
                $"Maximum of {_emailServiceOptions.MaxAttachmentCountPerEmail} attachments allowed per email. Provided: {request.Attachments.Count}");

            foreach (var attachment in request.Attachments)
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Data);
        }

        // Apply body
        if (!request.HtmlBody.IsNullOrWhitespace())
            bodyBuilder.HtmlBody = request.HtmlBody;

        if (!request.TextBody.IsNullOrWhitespace())
            bodyBuilder.TextBody = request.TextBody;

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    /// <summary>Pulls send metadata from a MimeMessage for result tracking.</summary>
    /// <param name="message">Message to read.</param>
    /// <param name="defaultFromAddress">Fallback From when the message has none.</param>
    /// <param name="defaultFromName">Fallback From name when the message has none.</param>
    /// <returns>An EmailRequest filled from the message.</returns>
    private static EmailRequest ExtractEmailMetadata(MimeMessage message, string? defaultFromAddress = null, string? defaultFromName = null)
    {
        var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? defaultFromAddress;
        var fromName = message.From.Mailboxes.FirstOrDefault()?.Name ?? defaultFromName;
        var toAddresses = message.To.Mailboxes.Select(m => m.Address).ToList();
        var ccAddresses = message.Cc.Mailboxes.Select(m => m.Address).ToList();
        var bccAddresses = message.Bcc.Mailboxes.Select(m => m.Address).ToList();

        // Attachment names come from MimeMessage; extra metadata (ContentType, MetadataJson) is taken from the builder when present
        IReadOnlyList<EmailAttachment>? attachments = null;
        var list = new List<EmailAttachment>();
        foreach (var part in message.Attachments) {
            if (part is not MimePart mimePart)
                continue;

            var fileName = mimePart.FileName ?? "attachment";
            list.Add(new(fileName, []));
        }

        if (list.Count > 0)
            attachments = list;

        var textBody = message.TextBody;
        var htmlBody = message.HtmlBody;

        return new(
            fromAddress, fromName, toAddresses.Count > 0 ? toAddresses : null, ccAddresses.Count > 0 ? ccAddresses : null, bccAddresses.Count > 0 ? bccAddresses : null,
            message.Subject, attachments, string.IsNullOrEmpty(textBody) ? null : textBody, string.IsNullOrEmpty(htmlBody) ? null : htmlBody);
    }

    /// <summary>Invokes EmailSending.</summary>
    /// <param name="emailRequest">Request about to be sent.</param>
    private void OnEmailSending(EmailRequest emailRequest) => EmailSending?.Invoke(this, new(emailRequest));

    /// <summary>Invokes EmailSent.</summary>
    /// <param name="result">Send outcome.</param>
    private void OnEmailSent(EmailResult result) => EmailSent?.Invoke(this, new(result));

    /// <summary>Invokes BulkSending.</summary>
    /// <param name="messages">Requests in the bulk batch.</param>
    private void OnBulkSending(IReadOnlyList<EmailRequest> messages) => BulkSending?.Invoke(this, new(messages));

    /// <summary>Invokes BulkEmailSent.</summary>
    /// <param name="results">Per-message outcomes.</param>
    private void OnBulkEmailCompleted(IReadOnlyList<Result<EmailRequest>> results) => BulkEmailSent?.Invoke(this, new(BulkResult<EmailRequest>.FromResults(results)));

    /// <summary>Invokes ConnectionTested.</summary>
    /// <param name="isSuccess">Whether the probe succeeded.</param>
    /// <param name="elapsedTime">How long the probe took.</param>
    /// <param name="exception">Failure exception, or null on success.</param>
    private void OnConnectionTested(bool isSuccess, TimeSpan elapsedTime, Exception? exception = null) => ConnectionTested?.Invoke(this, new(isSuccess, elapsedTime, exception));
}