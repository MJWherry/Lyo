using Lyo.Email;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Job.Client;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Security;
using Lyo.Job.Worker;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Parameters;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Workers.Notification;

/// <summary>
/// Sends an email as a workflow step.
/// <para>
/// Calls <see cref="IEmailService" /> directly rather than going through <c>INotificationPublisher</c>. The publisher catches and logs every handler exception by design, so a
/// refused send would still finish the run as <c>Success</c> — the exact failure mode this package exists to remove.
/// </para>
/// </summary>
/// <remarks>Parameters: <c>To</c> (required, comma or semicolon separated), <c>Subject</c> (required), <c>Body</c> (required), <c>Cc</c>, <c>IsHtml</c>, <c>From</c>, <c>FromName</c>.</remarks>
public class EmailNotificationWorker : JobWorkerBase
{
    private readonly IEmailService _email;
    private readonly EmailNotificationWorkerOptions _options;

    public EmailNotificationWorker(
        IMqService mq,
        IJobClient jobClient,
        IJobEventPublisher events,
        IEmailService email,
        EmailNotificationWorkerOptions options,
        ILogger<EmailNotificationWorker>? logger = null,
        IMetrics? metrics = null,
        IJobParameterEncryptionService? parameterEncryption = null)
        : base(mq, jobClient, events, options.WorkerType, logger, metrics, options.MaxRequeueCount, options.DlqName, parameterEncryption)
    {
        ArgumentHelpers.ThrowIfNull(email);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _email = email;
        _options = options;
    }

    protected override async Task ExecuteAsync(IJobWorkerContext ctx)
    {
        if (!NotificationParameters.TryRequired(ctx, Constants.ParameterKey.To, out var to) ||
            !NotificationParameters.TryRequired(ctx, Constants.ParameterKey.Subject, out var subject) ||
            !NotificationParameters.TryRequired(ctx, Constants.ParameterKey.Body, out var body))
            return;

        var isHtml = ctx.Run.JobRunParameters.GetBool(Constants.ParameterKey.IsHtml) ?? false;
        var cc = NotificationParameters.Optional(ctx, Constants.ParameterKey.Cc);
        var request = new EmailRequest(
            NotificationParameters.Optional(ctx, Constants.ParameterKey.From) ?? _options.DefaultFromAddress,
            NotificationParameters.Optional(ctx, Constants.ParameterKey.FromName) ?? _options.DefaultFromName, Addresses(NotificationParameters.Recipients(to)),
            cc is null ? null : Addresses(NotificationParameters.Recipients(cc)), Subject: subject, TextBody: isHtml ? null : body, HtmlBody: isHtml ? body : null);

        try {
            var result = await _email.SendEmailAsync(request, ctx.CancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess) {
                ctx.Results.AddError($"Email to '{to}' was not sent: {NotificationParameters.FormatErrors(result.Errors)}");
                return;
            }

            ctx.Results.AddResult(Constants.ResultKey.Recipient, to);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            ctx.Logger.LogError(ex, "Email notification run {RunId} failed sending to {Recipient}", ctx.Run.Id, to);
            ctx.Results.AddError($"Email to '{to}' failed: {ex.Message}");
        }
    }

    private static IReadOnlyList<EmailAddress> Addresses(IReadOnlyList<string> values) => values.Select(static value => new EmailAddress(value)).ToArray();
}