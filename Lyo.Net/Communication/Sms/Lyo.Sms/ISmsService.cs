using Lyo.Result;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;

namespace Lyo.Sms;

/// <summary>Contract for sending and looking up SMS messages.</summary>
/// <typeparam name="TResult">Send-result type (for example <see cref="Result{SmsRequest}" /> or a provider subtype).</typeparam>
public interface ISmsService<TResult>
    where TResult : Result<SmsRequest>
{
    /// <summary>Sends an SMS.</summary>
    /// <param name="to">Recipient (E.164 or US form).</param>
    /// <param name="body">Body text (required).</param>
    /// <param name="from">Sender; the default is used when omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome of the send.</returns>
    Task<TResult> SendSmsAsync(string to, string body, string? from = null, CancellationToken ct = default);

    /// <summary>Sends an MMS with media URLs.</summary>
    /// <param name="to">Recipient (E.164 or US form).</param>
    /// <param name="mediaUrls">Media URLs to attach (required).</param>
    /// <param name="body">Optional body text.</param>
    /// <param name="from">Sender; the default is used when omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome of the send.</returns>
    Task<TResult> SendMmsAsync(string to, IEnumerable<string> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default);

    /// <summary>Sends an MMS with media URIs.</summary>
    /// <param name="to">Recipient (E.164 or US form).</param>
    /// <param name="mediaUrls">Media URIs to attach (required).</param>
    /// <param name="body">Optional body text.</param>
    /// <param name="from">Sender; the default is used when omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome of the send.</returns>
    Task<TResult> SendMmsAsync(string to, IEnumerable<Uri> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default);

    /// <summary>Sends SMS or MMS from a builder.</summary>
    /// <param name="builder">Builder holding the message.</param>
    /// <param name="from">Optional sender override.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome of the send.</returns>
    Task<TResult> SendAsync(SmsMessageBuilder builder, string? from = null, CancellationToken ct = default);

    /// <summary>Sends SMS or MMS from a request.</summary>
    /// <param name="request">Message to send.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome of the send.</returns>
    Task<TResult> SendAsync(SmsRequest request, CancellationToken ct = default);

    /// <summary>Sends many SMS or MMS messages from builders.</summary>
    /// <param name="builders">Builders to send.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One result per message.</returns>
    Task<IReadOnlyList<TResult>> SendBulkAsync(IEnumerable<SmsMessageBuilder> builders, CancellationToken ct = default);

    /// <summary>Sends many SMS messages.</summary>
    /// <param name="messages">Messages to send.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One result per message.</returns>
    Task<IReadOnlyList<TResult>> SendBulkSmsAsync(IEnumerable<SmsRequest> messages, CancellationToken ct = default);

    /// <summary>Sends a batch described by a bulk builder.</summary>
    /// <param name="bulkBuilder">Builder holding messages and a default sender.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Bulk result with a summary and per-message outcomes.</returns>
    Task<BulkResult<SmsRequest>> SendBulkAsync(BulkSmsBuilder bulkBuilder, CancellationToken ct = default);

    /// <summary>Looks up a message by id.</summary>
    /// <param name="messageId">Unique message id.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>The matching message result.</returns>
    Task<TResult> GetMessageByIdAsync(string messageId, CancellationToken ct = default);

    /// <summary>Lists messages that match <paramref name="filter" />, paged.</summary>
    /// <param name="filter">From, To, DateSentAfter, DateSentBefore, PageSize. Pass the result NextCursor as DateSentBefore for the next page.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One page of message results.</returns>
    Task<SmsMessageQueryResults<TResult>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default);

    /// <summary>Checks that the SMS provider can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the provider answers; otherwise false.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}

/// <summary>Non-generic SMS contract that returns <see cref="Result{SmsRequest}" /> for older callers.</summary>
public interface ISmsService : ISmsService<Result<SmsRequest>> { }