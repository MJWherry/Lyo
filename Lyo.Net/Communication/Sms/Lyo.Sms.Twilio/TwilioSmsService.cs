using System.Diagnostics;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Twilio.Clients;
using Twilio.Rest.Api.V2010;
using Twilio.Rest.Api.V2010.Account;
#if NETSTANDARD2_0
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#endif

namespace Lyo.Sms.Twilio;

/// <summary>Twilio SMS service implementation using the Twilio API for sending SMS and MMS messages.</summary>
/// <remarks>
/// <para>
/// This class is thread-safe. All instance fields are readonly and operations use thread-safe collections and synchronization primitives from the base class for bulk
/// operations.
/// </para>
/// </remarks>
public sealed class TwilioSmsService : SmsServiceBase<TwilioSmsResult>, ISmsService
{
    /// <summary>Maximum number of media attachments allowed per message (Twilio limit).</summary>
    private const int MaxMediaAttachmentsPerMessage = 10;

    /// <summary>Maximum messages Twilio allows per request.</summary>
    private const int TwilioMaxPageSize = 1000;

    private readonly TwilioRestClient _client;

    private readonly TwilioOptions _options;

    /// <summary>Initializes a new instance of the TwilioSmsService class.</summary>
    /// <param name="options">The Twilio configuration options. Must not be null and must have AccountSid and AuthToken set.</param>
    /// <param name="restClient">The Twilio REST client.</param>
    /// <param name="logger">Optional logger instance for logging operations.</param>
    /// <param name="metrics">Optional metrics instance for tracking SMS operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or restClient is null.</exception>
    /// <exception cref="ArgumentException">Thrown when AccountSid or AuthToken is null or whitespace.</exception>
    public TwilioSmsService(TwilioOptions options, TwilioRestClient restClient, ILogger<TwilioSmsService>? logger = null, IMetrics? metrics = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<TwilioSmsService>(), metrics)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNull(restClient, nameof(restClient));
        _options = options;
        _client = restClient;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.AccountSid, nameof(options) + "." + nameof(options.AccountSid));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.AuthToken, nameof(options) + "." + nameof(options.AuthToken));
        MetricNames[nameof(Sms.Constants.Metrics.SendDuration)] = Constants.Metrics.SendDuration;
        MetricNames[nameof(Sms.Constants.Metrics.SendSuccess)] = Constants.Metrics.SendSuccess;
        MetricNames[nameof(Sms.Constants.Metrics.SendFailure)] = Constants.Metrics.SendFailure;
        MetricNames[nameof(Sms.Constants.Metrics.BulkSendDuration)] = Constants.Metrics.BulkSendDuration;
        MetricNames[nameof(Sms.Constants.Metrics.BulkSendTotal)] = Constants.Metrics.BulkSendTotal;
        MetricNames[nameof(Sms.Constants.Metrics.BulkSendSuccess)] = Constants.Metrics.BulkSendSuccess;
        MetricNames[nameof(Sms.Constants.Metrics.BulkSendFailure)] = Constants.Metrics.BulkSendFailure;
        MetricNames[nameof(Sms.Constants.Metrics.BulkSendLastDurationMs)] = Constants.Metrics.BulkSendLastDurationMs;
    }

    async Task<Result<SmsRequest>> ISmsService<Result<SmsRequest>>.SendSmsAsync(string to, string body, string? from, CancellationToken ct)
        => await SendSmsAsync(to, body, from, ct).ConfigureAwait(false);

    async Task<Result<SmsRequest>> ISmsService<Result<SmsRequest>>.SendMmsAsync(string to, IEnumerable<string> mediaUrls, string? body, string? from, CancellationToken ct)
        => await SendMmsAsync(to, mediaUrls, body, from, ct).ConfigureAwait(false);

    async Task<Result<SmsRequest>> ISmsService<Result<SmsRequest>>.SendMmsAsync(string to, IEnumerable<Uri> mediaUrls, string? body, string? from, CancellationToken ct)
        => await SendMmsAsync(to, mediaUrls, body, from, ct).ConfigureAwait(false);

    async Task<Result<SmsRequest>> ISmsService<Result<SmsRequest>>.SendAsync(SmsMessageBuilder builder, string? from, CancellationToken ct)
        => await SendAsync(builder, from, ct).ConfigureAwait(false);

    async Task<Result<SmsRequest>> ISmsService<Result<SmsRequest>>.SendAsync(SmsRequest request, CancellationToken ct) => await SendAsync(request, ct).ConfigureAwait(false);

    async Task<IReadOnlyList<Result<SmsRequest>>> ISmsService<Result<SmsRequest>>.SendBulkAsync(IEnumerable<SmsMessageBuilder> builders, CancellationToken ct)
        => (await SendBulkAsync(builders, ct).ConfigureAwait(false)).Select(r => (Result<SmsRequest>)r).ToList();

    async Task<IReadOnlyList<Result<SmsRequest>>> ISmsService<Result<SmsRequest>>.SendBulkSmsAsync(IEnumerable<SmsRequest> messages, CancellationToken ct)
        => (await SendBulkSmsAsync(messages, ct).ConfigureAwait(false)).Select(r => (Result<SmsRequest>)r).ToList();

    async Task<Result<SmsRequest>> ISmsService<Result<SmsRequest>>.GetMessageByIdAsync(string messageId, CancellationToken ct)
        => await GetMessageByIdAsync(messageId, ct).ConfigureAwait(false);

    async Task<SmsMessageQueryResults<Result<SmsRequest>>> ISmsService<Result<SmsRequest>>.GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct)
    {
        var r = await GetMessagesAsync(filter, ct).ConfigureAwait(false);
        return new(r.Items.Select(x => (Result<SmsRequest>)x).ToList(), r.PageSize, r.HasMore, r.NextCursor);
    }

    /// <summary>Sends an SMS or MMS message using the Twilio API.</summary>
    /// <param name="request">The message to send. Must have a To address and either a body or media attachments.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A Result&lt;SmsRequest&gt; indicating success or failure with details.</returns>
    /// <remarks>Long messages (>160 characters) are automatically split into multiple segments by Twilio.</remarks>
    /// <inheritdoc />
    protected override TwilioSmsResult CreateFailure(Exception exception, string code, SmsRequest? request = null)
        => TwilioSmsResult.FromException(exception, request ?? new SmsRequest(), _options.AccountSid);

    protected override async Task<TwilioSmsResult> SendCoreAsync(SmsRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.To, nameof(request.To));
            var hasBody = !string.IsNullOrWhiteSpace(request.Body);
            var hasMedia = request.MediaUrls.Count > 0;
            OperationHelpers.ThrowIf(!hasBody && !hasMedia, "Message must have either a body or at least one media attachment.");
            if (hasBody && request.Body!.Length > Options.MaxMessageBodyLength)
                ArgumentHelpers.ThrowIfNotInRange(request.Body.Length, 1, Options.MaxMessageBodyLength, nameof(request.Body));
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to send message");
            return TwilioSmsResult.FromException(ex, request, _options.AccountSid);
        }

        if (!string.IsNullOrWhiteSpace(request.Body) && request.Body!.Length > 160) {
            var segmentCount = (int)Math.Ceiling(request.Body.Length / 160.0);
            Logger.LogWarning("Message body is {Length} characters and will be split into approximately {SegmentCount} segments", request.Body.Length, segmentCount);
        }

        if (request.MediaUrls.Count > 0) {
            try {
                ValidateMediaUrls(request.MediaUrls);
            }
            catch (Exception ex) {
                sw.Stop();
                Logger.LogError(ex, "Media validation failed for message to {To}", request.To);
                return TwilioSmsResult.FromException(ex, request, _options.AccountSid);
            }
        }

        var fromNumber = !string.IsNullOrWhiteSpace(request.From) ? request.From : _options.DefaultFromPhoneNumber;
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(fromNumber, nameof(request.From));
        }
        catch (ArgumentException ex) {
            sw.Stop();
            var error = "From phone number is required but not provided and no default sender is configured";
            Logger.LogError(error);
            return TwilioSmsResult.FromError(error, SmsErrorCodes.MissingFromNumber, request, ex, _options.AccountSid);
        }

        var maskedTo = MaskPhoneNumber(request.To);
        var maskedFrom = MaskPhoneNumber(fromNumber);
        var mediaInfo = request.MediaUrls.Count > 0 ? $" with {request.MediaUrls.Count} media attachment(s)" : string.Empty;
        Logger.LogInformation("Sending SMS{MediaInfo} to {To} from {From}", mediaInfo, maskedTo, maskedFrom);
        try {
            var mediaUrls = request.MediaUrls.Count > 0 ? request.MediaUrls.Select(uri => uri).ToList() : [];
#if NETSTANDARD2_0
            MessageResource messageResource;
            if (mediaUrls.Count > 0) {
                messageResource = await Task.Run(
                        () => MessageResource.Create(new(request.To), from: new(fromNumber), body: request.Body ?? string.Empty, mediaUrl: mediaUrls, client: _client), ct)
                    .ConfigureAwait(false);
            }
            else {
                messageResource = await Task.Run(() => MessageResource.Create(new(request.To), from: new(fromNumber), body: request.Body!, client: _client), ct)
                    .ConfigureAwait(false);
            }
#else
            MessageResource messageResource;
            if (mediaUrls.Count > 0) {
                messageResource = await MessageResource.CreateAsync(
                        new(request.To), from: new(fromNumber), body: request.Body ?? string.Empty, mediaUrl: mediaUrls, client: _client)
                    .ConfigureAwait(false);
            }
            else
                messageResource = await MessageResource.CreateAsync(new(request.To), from: new(fromNumber), body: request.Body!, client: _client).ConfigureAwait(false);
#endif
            sw.Stop();
            Logger.LogInformation(
                "SMS sent successfully to {To} in {Elapsed}ms. MessageId: {MessageId}, Status: {Status}", maskedTo, sw.ElapsedMilliseconds, messageResource.Sid,
                messageResource.Status);

            var sentMessage = new SmsRequest {
                To = messageResource.To ?? request.To, From = messageResource.From?.ToString() ?? fromNumber, Body = messageResource.Body ?? request.Body
            };

            return TwilioSmsResult.FromMessageResource(messageResource, sentMessage);
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            Logger.LogWarning("SMS send operation was cancelled after {Elapsed}ms", sw.ElapsedMilliseconds);
            return TwilioSmsResult.FromException(ex, request, _options.AccountSid);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to send SMS to {To} after {Elapsed}ms", maskedTo, sw.ElapsedMilliseconds);
            var failedRequest = new SmsRequest { To = request.To, From = fromNumber, Body = request.Body };
            return TwilioSmsResult.FromException(ex, failedRequest, _options.AccountSid);
        }
    }

    /// <summary>Retrieves an SMS message by its unique Twilio message identifier.</summary>
    /// <param name="messageId">The Twilio message SID (e.g., "SM1234567890abcdef").</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A Result&lt;SmsRequest&gt; containing the message details if found, or a failure result if not found or an error occurred.</returns>
    /// <exception cref="ArgumentException">Thrown when messageId is null or whitespace.</exception>
    public override async Task<TwilioSmsResult> GetMessageByIdAsync(string messageId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        using var timer = Metrics.StartTimer(Constants.Metrics.ApiGetMessageDuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(messageId, nameof(messageId));
        Logger.LogInformation("Fetching message by ID: {MessageId}", messageId);
        try {
#if NETSTANDARD2_0
            var messageResource = await Task.Run(() => MessageResource.Fetch(messageId, client: _client), ct).ConfigureAwait(false);
#else
            var messageResource = await MessageResource.FetchAsync(messageId, client: _client).ConfigureAwait(false);
#endif
            sw.Stop();
            Logger.LogInformation(
                "Message fetched successfully in {Elapsed}ms. MessageId: {MessageId}, Status: {Status}", sw.ElapsedMilliseconds, messageResource.Sid, messageResource.Status);

            var smsRequest = new SmsRequest { To = messageResource.To ?? string.Empty, From = messageResource.From?.ToString(), Body = messageResource.Body };
            return TwilioSmsResult.FromMessageResource(messageResource, smsRequest);
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            Logger.LogWarning("Get message operation was cancelled after {Elapsed}ms", sw.ElapsedMilliseconds);
            Metrics.RecordError(Constants.Metrics.ApiGetMessageDuration, ex);
            return TwilioSmsResult.FromException(ex, new(), _options.AccountSid);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to fetch message {MessageId} after {Elapsed}ms", messageId, sw.ElapsedMilliseconds);
            Metrics.RecordError(Constants.Metrics.ApiGetMessageDuration, ex);
            return TwilioSmsResult.FromException(ex, new(), _options.AccountSid);
        }
    }

    /// <summary>Retrieves SMS messages matching the specified filter criteria from Twilio with cursor-based pagination.</summary>
    /// <param name="filter">The filter criteria (From, To, DateSentAfter, DateSentBefore, PageSize). Use NextCursor from previous result as DateSentBefore for next page.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>Paginated results with Items, HasMore, and NextCursor for the next page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filter is null.</exception>
    /// <remarks>
    /// <para>Phone numbers are normalized to E.164. Dates are converted to UTC. Twilio returns messages newest-first.</para>
    /// </remarks>
    public override async Task<SmsMessageQueryResults<TwilioSmsResult>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(filter, nameof(filter));
        var sw = Stopwatch.StartNew();
        using var timer = Metrics.StartTimer(Constants.Metrics.ApiGetMessagesDuration);
        var pageSize = Math.Min(Math.Max(filter.PageSize, 1), TwilioMaxPageSize);
        var readMessageOptions = new ReadMessageOptions { PageSize = pageSize };
        if (!string.IsNullOrWhiteSpace(filter.From)) {
            var normalizedFrom = PhoneNumber.Normalize(filter.From);
            if (!string.IsNullOrWhiteSpace(normalizedFrom))
                readMessageOptions.From = new(normalizedFrom);
        }

        if (!string.IsNullOrWhiteSpace(filter.To)) {
            var normalizedTo = PhoneNumber.Normalize(filter.To);
            if (!string.IsNullOrWhiteSpace(normalizedTo))
                readMessageOptions.To = new(normalizedTo);
        }

        var dateSentAfter = filter.DateSentAfter.HasValue
            ? filter.DateSentAfter!.Value.Kind == DateTimeKind.Utc ? filter.DateSentAfter.Value : filter.DateSentAfter.Value.ToUniversalTime()
            : (DateTime?)null;

        var dateSentBefore = filter.DateSentBefore.HasValue
            ? filter.DateSentBefore!.Value.Kind == DateTimeKind.Utc ? filter.DateSentBefore.Value : filter.DateSentBefore.Value.ToUniversalTime()
            : (DateTime?)null;

        if (dateSentAfter.HasValue)
            readMessageOptions.DateSentAfter = dateSentAfter;

        if (dateSentBefore.HasValue)
            readMessageOptions.DateSentBefore = dateSentBefore;

        Logger.LogInformation(
            "Fetching messages: From={From}, To={To}, DateSentAfter={DateSentAfter}, DateSentBefore={DateSentBefore}, PageSize={PageSize}", filter.From, filter.To, dateSentAfter,
            dateSentBefore, pageSize);

        try {
            var results = new List<TwilioSmsResult>();
#if NETSTANDARD2_0
            var messages = await Task.Run(() => MessageResource.Read(readMessageOptions, _client), ct).ConfigureAwait(false);
#else
            var messages = await MessageResource.ReadAsync(readMessageOptions, _client).ConfigureAwait(false);
#endif
            foreach (var messageResource in messages) {
                if (ct.IsCancellationRequested || results.Count >= pageSize)
                    break;

                if (dateSentAfter.HasValue && messageResource.DateSent.HasValue && messageResource.DateSent.Value < dateSentAfter.Value)
                    continue;

                if (dateSentBefore.HasValue && messageResource.DateSent.HasValue && messageResource.DateSent.Value > dateSentBefore.Value)
                    continue;

                var smsRequest = new SmsRequest { To = messageResource.To ?? string.Empty, From = messageResource.From?.ToString(), Body = messageResource.Body };
                results.Add(TwilioSmsResult.FromMessageResource(messageResource, smsRequest));
            }

            sw.Stop();
            var hasMore = results.Count >= pageSize;
            DateTime? nextCursor = null;
            if (hasMore && results.Count > 0) {
                var oldest = results.Where(r => r.DateSent.HasValue).OrderBy(r => r.DateSent!.Value).FirstOrDefault();
                if (oldest?.DateSent is { } ds)
                    nextCursor = ds;
            }

            Logger.LogInformation("Fetched {Count} messages in {Elapsed}ms, HasMore={HasMore}", results.Count, sw.ElapsedMilliseconds, hasMore);
            return new(results, pageSize, hasMore, nextCursor);
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            Logger.LogWarning("Get messages operation was cancelled after {Elapsed}ms", sw.ElapsedMilliseconds);
            Metrics.RecordError(Constants.Metrics.ApiGetMessagesDuration, ex);
            return new([TwilioSmsResult.FromException(ex, new(), _options.AccountSid)], pageSize, false);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Failed to fetch messages after {Elapsed}ms", sw.ElapsedMilliseconds);
            Metrics.RecordError(Constants.Metrics.ApiGetMessagesDuration, ex);
            return new([TwilioSmsResult.FromException(ex, new(), _options.AccountSid)], pageSize, false);
        }
    }

    /// <summary>Tests the connection to Twilio by fetching account details.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>True if the connection is successful and account details can be retrieved, false otherwise.</returns>
    /// <remarks>
    /// <para>This method validates that the AccountSid and AuthToken are correct by attempting to fetch account information from Twilio.</para>
    /// </remarks>
    protected override async Task<bool> TestConnectionCoreAsync(CancellationToken ct = default)
    {
        Logger.LogInformation("Testing Twilio connection for AccountSid: {AccountSid}", _options.AccountSid);
        using var timer = Metrics.StartTimer(Constants.Metrics.TestConnectionDuration);
        try {
#if NETSTANDARD2_0
            var account = await Task.Run(() => AccountResource.Fetch(client: _client), ct).ConfigureAwait(false);
#else
            var account = await AccountResource.FetchAsync(client: _client).ConfigureAwait(false);
#endif
            if (account != null) {
                Logger.LogInformation("Twilio connection test successful. Account Status: {Status}", account.Status);
                return true;
            }

            Logger.LogWarning("Twilio connection test returned null account");
            return false;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Twilio connection test failed");
            Metrics.RecordError(Constants.Metrics.TestConnectionDuration, ex);
            return false;
        }
    }

    /// <summary>Masks a phone number for privacy-safe logging (shows only last 4 digits).</summary>
    /// <param name="phoneNumber">The phone number to mask.</param>
    /// <returns>A masked phone number showing only the last 4 digits, or "***" if the number is too short.</returns>
    private static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "***";

        if (phoneNumber.Length <= 4)
            return "***";

        var lastFour = phoneNumber.Substring(phoneNumber.Length - 4);
        return $"***{lastFour}";
    }

    /// <summary>Validates media URLs according to Twilio requirements.</summary>
    /// <param name="mediaUrls">The list of media URLs to validate.</param>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when the number of media URLs exceeds Twilio's limit.</exception>
    /// <remarks>Validates that the number of media URLs does not exceed Twilio's limit (10 per message). Size validation is handled by Twilio (typically >5MB for MMS, error code 11751).</remarks>
    private void ValidateMediaUrls(IReadOnlyList<Uri> mediaUrls)
    {
        if (mediaUrls.Count == 0)
            return;

        if (mediaUrls.Count > MaxMediaAttachmentsPerMessage) {
            throw new ArgumentOutsideRangeException(
                nameof(mediaUrls), mediaUrls.Count, 1, MaxMediaAttachmentsPerMessage,
                $"Maximum of {MaxMediaAttachmentsPerMessage} media attachments allowed per message. Provided: {mediaUrls.Count}");
        }

        Logger.LogDebug("Validated {Count} media URLs", mediaUrls.Count);
    }
}