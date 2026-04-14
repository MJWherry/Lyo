using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;
using Lyo.Common;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;

namespace Lyo.Email;

/// <summary>Email service implementation using MailKit for sending emails via SMTP.</summary>
/// <remarks>
/// <para>This class is thread-safe and can be used concurrently from multiple threads.</para>
/// <para>Each operation creates its own SMTP client connection, ensuring no shared mutable state between concurrent operations.</para>
/// <para>All instance fields are readonly, and the service maintains no mutable state between method calls.</para>
/// </remarks>
public sealed class EmailService : IEmailService
{
    private readonly EmailServiceOptions _emailServiceOptions;

    private readonly ILogger<EmailService> _logger;

    /// <summary>Gets the metric names dictionary.</summary>
    private readonly Dictionary<string, string> _metricNames;

    private readonly IMetrics _metrics;

    /// <summary>Email service implementation using MailKit for sending emails via SMTP.</summary>
    /// <remarks>
    /// <para>This class is thread-safe and can be used concurrently from multiple threads.</para>
    /// <para>Each operation creates its own SMTP client connection, ensuring no shared mutable state between concurrent operations.</para>
    /// <para>All instance fields are readonly, and the service maintains no mutable state between method calls.</para>
    /// </remarks>
    public EmailService(EmailServiceOptions emailServiceOptions, ILogger<EmailService>? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(emailServiceOptions, nameof(emailServiceOptions));
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

        async ValueTask<string> SendEmailCore(CancellationToken ct)
        {
            using var client = await CreateAndConnectSmtpClientAsync(ct).ConfigureAwait(false);
            var result = await client.SendAsync(message, ct).ConfigureAwait(false);
            try {
                if (client.IsConnected) {
                    await client.DisconnectAsync(true, ct).ConfigureAwait(false);
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
            var emailResult = EmailResult.FromSuccess(emailRequest, result, null, DateTime.UtcNow);
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

            // Provide more specific error messages based on exception type
            var userFriendlyMessage = ex switch {
                SocketException => "Unable to connect to SMTP server. Please check your network connection and SMTP server settings.",
                TimeoutException => "SMTP server connection timed out. The server may be busy or unreachable.",
                IOException => "An I/O error occurred while sending the email. Please try again.",
                AuthenticationException => "SMTP authentication failed. Please verify your username and password.",
                var _ => $"An error occurred while sending the email: {ex.Message}"
            };

            var failureResult = EmailResult.FromException(ex, emailRequest, EmailErrorCodes.SendFailed);
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

        async ValueTask<string> SendEmailCore(CancellationToken ct)
        {
            using var client = await CreateAndConnectSmtpClientAsync(ct).ConfigureAwait(false);
            var result = await client.SendAsync(message, ct).ConfigureAwait(false);
            try {
                if (client.IsConnected) {
                    await client.DisconnectAsync(true, ct).ConfigureAwait(false);
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
            var emailResult = EmailResult.FromSuccess(request, result, null, DateTime.UtcNow);
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

            // Provide more specific error messages based on exception type
            var failureResult = EmailResult.FromException(ex, request, EmailErrorCodes.SendFailed);
            OnEmailSent(failureResult);
            return failureResult;
        }
    }

    /// <inheritdoc />
    /// <remarks>Always uses a single SMTP connection for all emails in the bulk operation. If pooling is enabled, the connection is obtained from the pool.</remarks>
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
            OnBulkEmailCompleted(r, sw.Elapsed);
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
                    var emailResult = EmailResult.FromSuccess(emailRequest, result, null, DateTime.UtcNow);
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
        OnBulkEmailCompleted(resultsList, sw.Elapsed);
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

        async ValueTask<bool> TestConnectionCore(CancellationToken ct)
        {
            using var client = await CreateAndConnectSmtpClientAsync(ct).ConfigureAwait(false);
            await client.DisconnectAsync(true, ct).ConfigureAwait(false);
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

    /// <summary>Event fired before an email send operation starts.</summary>
    public event EventHandler<EmailSendingEventArgs>? EmailSending;

    /// <summary>Event fired when an email send operation completes (success or failure).</summary>
    public event EventHandler<EmailSentEventArgs>? EmailSent;

    /// <summary>Event fired before a bulk email operation starts.</summary>
    public event EventHandler<EmailBulkSendingEventArgs>? BulkSending;

    /// <summary>Event fired when a bulk email operation completes.</summary>
    public event EventHandler<BulkEmailSentEventArgs>? BulkEmailSent;

    /// <summary>Event fired when a connection test completes.</summary>
    public event EventHandler<ConnectionTestedEventArgs>? ConnectionTested;

    /// <summary>Creates a new SMTP client, connects to the server, and authenticates.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A connected and authenticated SMTP client.</returns>
    private async Task<SmtpClient> CreateAndConnectSmtpClientAsync(CancellationToken ct = default)
    {
        var client = new SmtpClient();
        var connectSw = Stopwatch.StartNew();
        _logger.LogDebug("Connecting to SMTP server {Host}:{Port}", _emailServiceOptions.Host, _emailServiceOptions.Port);
        await client.ConnectAsync(_emailServiceOptions.Host, _emailServiceOptions.Port, _emailServiceOptions.UseSsl, ct).ConfigureAwait(false);
        connectSw.Stop();
        _metrics.RecordTiming(_metricNames[nameof(Constants.Metrics.SmtpConnectDuration)], connectSw.Elapsed);
        var authSw = Stopwatch.StartNew();
        _logger.LogDebug("Authenticating as {Username}", _emailServiceOptions.Username);
        await client.AuthenticateAsync(_emailServiceOptions.Username, _emailServiceOptions.Password, ct).ConfigureAwait(false);
        authSw.Stop();
        _metrics.RecordTiming(_metricNames[nameof(Constants.Metrics.SmtpAuthenticateDuration)], authSw.Elapsed);
        return client;
    }

    /// <summary>Gets a summary string of recipients for logging purposes.</summary>
    /// <param name="message">The MimeMessage to extract recipients from.</param>
    /// <returns>A formatted string describing recipient counts (e.g., "2 To, 1 Cc").</returns>
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

    /// <summary>Builds a MimeMessage from an EmailRequest.</summary>
    /// <param name="request">The email request to convert.</param>
    /// <returns>A MimeMessage built from the request.</returns>
    private MimeMessage BuildMimeMessageFromRequest(EmailRequest request)
    {
        var message = new MimeMessage();
        var bodyBuilder = new BodyBuilder();

        // Set From address
        if (!string.IsNullOrWhiteSpace(request.FromAddress))
            message.From.Add(new MailboxAddress(request.FromName ?? request.FromAddress, request.FromAddress));
        else
            message.From.Add(new MailboxAddress(_emailServiceOptions.DefaultFromName, _emailServiceOptions.DefaultFromAddress));

        // Set To addresses
        if (request.ToAddresses != null) {
            foreach (var to in request.ToAddresses)
                message.To.Add(new MailboxAddress(to, to));
        }

        // Set Cc addresses
        if (request.CcAddresses != null) {
            foreach (var cc in request.CcAddresses)
                message.Cc.Add(new MailboxAddress(cc, cc));
        }

        // Set Bcc addresses
        if (request.BccAddresses != null) {
            foreach (var bcc in request.BccAddresses)
                message.Bcc.Add(new MailboxAddress(bcc, bcc));
        }

        // Set Subject
        if (!string.IsNullOrWhiteSpace(request.Subject))
            message.Subject = request.Subject;

        // Add attachments
        if (request.Attachments != null) {
            ArgumentHelpers.ThrowIfNotInRange(
                request.Attachments.Count, 0, _emailServiceOptions.MaxAttachmentCountPerEmail, nameof(request.Attachments),
                $"Maximum of {_emailServiceOptions.MaxAttachmentCountPerEmail} attachments allowed per email. Provided: {request.Attachments.Count}");

            foreach (var attachment in request.Attachments)
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Data);
        }

        // Set body (empty if no content provided)
        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    /// <summary>Extracts email metadata from a MimeMessage for result tracking.</summary>
    /// <param name="message">The MimeMessage to extract metadata from.</param>
    /// <param name="defaultFromAddress">Default from address to use if message doesn't have one.</param>
    /// <param name="defaultFromName">Default from name to use if message doesn't have one.</param>
    /// <returns>An EmailRequest containing extracted email metadata.</returns>
    private static EmailRequest ExtractEmailMetadata(MimeMessage message, string? defaultFromAddress = null, string? defaultFromName = null)
    {
        var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? defaultFromAddress;
        var fromName = message.From.Mailboxes.FirstOrDefault()?.Name ?? defaultFromName;
        var toAddresses = message.To.Mailboxes.Select(m => m.Address).ToList();
        var ccAddresses = message.Cc.Mailboxes.Select(m => m.Address).ToList();
        var bccAddresses = message.Bcc.Mailboxes.Select(m => m.Address).ToList();

        // Extract attachments - we only get filenames from MimeMessage; metadata (FileStorageId, etc.) comes from builder when available
        IReadOnlyList<EmailAttachment>? attachments = null;
        if (message.Body is Multipart multipart) {
            var list = new List<EmailAttachment>();
            foreach (var part in multipart) {
                if (part is MimePart mimePart && mimePart.IsAttachment) {
                    var fileName = mimePart.FileName ?? "attachment";
                    list.Add(new(fileName, []));
                }
            }

            if (list.Count > 0)
                attachments = list;
        }

        return new(
            fromAddress, fromName, toAddresses.Count > 0 ? toAddresses : null, ccAddresses.Count > 0 ? ccAddresses : null, bccAddresses.Count > 0 ? bccAddresses : null,
            message.Subject, attachments);
    }

    /// <summary>Raises the EmailSending event.</summary>
    /// <param name="emailRequest">The email request being sent.</param>
    private void OnEmailSending(EmailRequest emailRequest) => EmailSending?.Invoke(this, new(emailRequest));

    /// <summary>Raises the EmailSent event.</summary>
    /// <param name="result">The email send result.</param>
    private void OnEmailSent(Result<EmailRequest> result) => EmailSent?.Invoke(this, new(result));

    /// <summary>Raises the BulkSending event.</summary>
    /// <param name="messages">The list of email requests being sent in bulk.</param>
    private void OnBulkSending(IReadOnlyList<EmailRequest> messages) => BulkSending?.Invoke(this, new(messages));

    /// <summary>Raises the BulkEmailSent event.</summary>
    /// <param name="results">The list of email send results.</param>
    /// <param name="elapsedTime">The total elapsed time for the bulk operation.</param>
    private void OnBulkEmailCompleted(IReadOnlyList<Result<EmailRequest>> results, TimeSpan elapsedTime)
        => BulkEmailSent?.Invoke(this, new(BulkResult<EmailRequest>.FromResults(results)));

    /// <summary>Raises the ConnectionTested event.</summary>
    /// <param name="isSuccess">Whether the connection test succeeded.</param>
    /// <param name="elapsedTime">The elapsed time for the connection test.</param>
    /// <param name="exception">The exception if the test failed, null otherwise.</param>
    private void OnConnectionTested(bool isSuccess, TimeSpan elapsedTime, Exception? exception = null) => ConnectionTested?.Invoke(this, new(isSuccess, elapsedTime, exception));
}