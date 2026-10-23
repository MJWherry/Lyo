using System.Diagnostics;
using Lyo.Email;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace Lyo.Email.Smtp;

/// <summary>MailKit SMTP implementation of <see cref="IEmailService" />. Each single send opens its own connection. A bulk send reuses one connection.</summary>
public sealed class SmtpEmailService : EmailServiceBase
{
    private readonly SmtpEmailOptions _options;

    /// <summary>Constructs the SMTP sender.</summary>
    /// <param name="options">SMTP settings. Call <see cref="SmtpEmailOptions.Validate" /> before passing them in. The DI helpers do that.</param>
    /// <param name="logger">Logger. A null logger is used when this is omitted.</param>
    /// <param name="metrics">Metrics sink. Ignored unless <see cref="EmailServiceOptions.EnableMetrics" /> is true.</param>
    public SmtpEmailService(SmtpEmailOptions options, ILogger<SmtpEmailService>? logger = null, IMetrics? metrics = null)
        : base(options, logger, metrics)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task<EmailResult> SendCoreAsync(EmailRequest request, CancellationToken ct)
    {
        var message = SmtpEmailMessageMapper.ToMimeMessage(request, _options);
        var client = await ConnectAsync(ct).ConfigureAwait(false);
        try {
            var response = await client.SendAsync(message, ct).ConfigureAwait(false);
            return EmailResult.FromSuccess(request, response, message.MessageId, DateTime.UtcNow);
        }
        finally {
            await DisconnectAsync(client).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<EmailResult>> SendBulkCoreAsync(IReadOnlyList<EmailRequest> requests, CancellationToken ct)
    {
        SmtpClient client;
        try {
            client = await ConnectAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) {
            return requests.Select(request => EmailResult.FromException(ex, request, EmailErrorCodes.OperationCancelled)).ToArray();
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Bulk email send failed while connecting to {Host}:{Port}", _options.Host, _options.Port);
            return requests.Select(request => EmailResult.FromException(ex, request, EmailErrorCodes.SendFailed)).ToArray();
        }

        var results = new List<EmailResult>(requests.Count);
        try {
            foreach (var request in requests) {
                if (ct.IsCancellationRequested) {
                    results.Add(EmailResult.FromException(new OperationCanceledException(ct), request, EmailErrorCodes.OperationCancelled));
                    break;
                }

                var message = SmtpEmailMessageMapper.ToMimeMessage(request, _options);
                try {
                    var response = await client.SendAsync(message, ct).ConfigureAwait(false);
                    results.Add(EmailResult.FromSuccess(request, response, message.MessageId, DateTime.UtcNow));
                }
                catch (OperationCanceledException ex) {
                    results.Add(EmailResult.FromException(ex, request, EmailErrorCodes.OperationCancelled));
                    break;
                }
                catch (Exception ex) {
                    results.Add(MapSendFailure(ex, request));
                }
            }
        }
        finally {
            await DisconnectAsync(client).ConfigureAwait(false);
        }

        return results;
    }

    /// <inheritdoc />
    protected override async Task<bool> TestConnectionCoreAsync(CancellationToken ct)
    {
        var client = await ConnectAsync(ct).ConfigureAwait(false);
        await DisconnectAsync(client).ConfigureAwait(false);
        return true;
    }

    private async Task<SmtpClient> ConnectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var client = new SmtpClient();
        try {
            var connectSw = Stopwatch.StartNew();
            Logger.LogDebug("Connecting to SMTP server {Host}:{Port}", _options.Host, _options.Port);
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, ct).ConfigureAwait(false);
            connectSw.Stop();
            Metrics.RecordTiming(Constants.Metrics.SmtpConnectDuration, connectSw.Elapsed);
            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password)) {
                var authSw = Stopwatch.StartNew();
                Logger.LogDebug("Authenticating as {Username}", _options.Username);
                var username = _options.Username!;
                var password = _options.Password!;
                await client.AuthenticateAsync(username, password, ct).ConfigureAwait(false);
                authSw.Stop();
                Metrics.RecordTiming(Constants.Metrics.SmtpAuthenticateDuration, authSw.Elapsed);
            }

            return client;
        }
        catch {
            client.Dispose();
            throw;
        }
    }

    private async Task DisconnectAsync(SmtpClient client)
    {
        try {
            if (client.IsConnected)
                await client.DisconnectAsync(true, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Error disconnecting from SMTP server");
        }
        finally {
            client.Dispose();
        }
    }
}
