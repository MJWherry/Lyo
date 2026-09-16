using Lyo.Job.Client;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Security;
using Lyo.Job.Worker;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Sms;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Workers.Notification.Constants;
using Lyo.Exceptions;

namespace Lyo.Job.Workers.Notification;

/// <summary>
/// Sends an SMS as a workflow step. Calls <see cref="ISmsService" /> directly for the same reason as the email worker: a send the provider refused has to fail the run, and
/// <c>INotificationPublisher</c> would swallow it into a success.
/// </summary>
/// <remarks>Parameters: <c>To</c> (required, E.164), <c>Body</c> (required), <c>From</c>.</remarks>
public class SmsNotificationWorker : JobWorkerBase
{
    private readonly SmsNotificationWorkerOptions _options;
    private readonly ISmsService _sms;

    public SmsNotificationWorker(
        IMqService mq,
        IJobClient jobClient,
        IJobEventPublisher events,
        ISmsService sms,
        SmsNotificationWorkerOptions options,
        ILogger<SmsNotificationWorker>? logger = null,
        IMetrics? metrics = null,
        IJobParameterEncryptionService? parameterEncryption = null)
        : base(mq, jobClient, events, options.WorkerType, logger, metrics, options.MaxRequeueCount, options.DlqName, parameterEncryption)
    {
        ArgumentHelpers.ThrowIfNull(sms);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _sms = sms;
        _options = options;
    }

    protected override async Task ExecuteAsync(IJobWorkerContext ctx)
    {
        if (!NotificationParameters.TryRequired(ctx, Constants.ParameterKey.To, out var to)
            || !NotificationParameters.TryRequired(ctx, Constants.ParameterKey.Body, out var body))
            return;

        var from = NotificationParameters.Optional(ctx, Constants.ParameterKey.From) ?? _options.DefaultFrom;
        try {
            var result = await _sms.SendSmsAsync(to, body, from, ctx.CancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess) {
                ctx.Results.AddError($"SMS to '{to}' was not sent: {NotificationParameters.FormatErrors(result.Errors)}");
                return;
            }

            ctx.Results.AddResult(Constants.ResultKey.Recipient, to);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            ctx.Logger.LogError(ex, "SMS notification run {RunId} failed sending to {Recipient}", ctx.Run.Id, to);
            ctx.Results.AddError($"SMS to '{to}' failed: {ex.Message}");
        }
    }
}
