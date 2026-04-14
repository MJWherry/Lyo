using Lyo.Common;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;

namespace Lyo.Sms;

/// <summary>Service interface for sending and retrieving SMS messages.</summary>
/// <typeparam name="TResult">The type of result returned by send operations (e.g. <see cref="Result{SmsRequest}" /> or a provider-specific subtype).</typeparam>
public interface ISmsService<TResult>
    where TResult : Result<SmsRequest>
{
    /// <summary>Sends an SMS message.</summary>
    /// <param name="to">The recipient phone number (E.164 format or US format).</param>
    /// <param name="body">The message body text (required).</param>
    /// <param name="from">The sender phone number (optional, uses default if not provided).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the SMS send operation.</returns>
    Task<TResult> SendSmsAsync(string to, string body, string? from = null, CancellationToken ct = default);

    /// <summary>Sends an MMS message with media attachments.</summary>
    /// <param name="to">The recipient phone number (E.164 format or US format).</param>
    /// <param name="mediaUrls">The collection of media URLs to send (required).</param>
    /// <param name="body">Optional message body text.</param>
    /// <param name="from">The sender phone number (optional, uses default if not provided).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the MMS send operation.</returns>
    Task<TResult> SendMmsAsync(string to, IEnumerable<string> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default);

    /// <summary>Sends an MMS message with media attachments.</summary>
    /// <param name="to">The recipient phone number (E.164 format or US format).</param>
    /// <param name="mediaUrls">The collection of media URIs to send (required).</param>
    /// <param name="body">Optional message body text.</param>
    /// <param name="from">The sender phone number (optional, uses default if not provided).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the MMS send operation.</returns>
    Task<TResult> SendMmsAsync(string to, IEnumerable<Uri> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default);

    /// <summary>Sends a message (SMS or MMS) using a message builder.</summary>
    /// <param name="builder">The message builder containing message details.</param>
    /// <param name="from">Optional sender phone number override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the send operation.</returns>
    Task<TResult> SendAsync(SmsMessageBuilder builder, string? from = null, CancellationToken ct = default);

    /// <summary>Sends a message (SMS or MMS).</summary>
    /// <param name="request">The message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the send operation.</returns>
    Task<TResult> SendAsync(SmsRequest request, CancellationToken ct = default);

    /// <summary>Sends multiple messages (SMS or MMS) in bulk.</summary>
    /// <param name="builders">Collection of message builders.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of results for each message.</returns>
    Task<IReadOnlyList<TResult>> SendBulkAsync(IEnumerable<SmsMessageBuilder> builders, CancellationToken ct = default);

    /// <summary>Sends multiple SMS messages in bulk.</summary>
    /// <param name="messages">Collection of SMS messages.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of results for each message.</returns>
    Task<IReadOnlyList<TResult>> SendBulkSmsAsync(IEnumerable<SmsRequest> messages, CancellationToken ct = default);

    /// <summary>Sends multiple messages (SMS or MMS) in bulk using a bulk builder.</summary>
    /// <param name="bulkBuilder">The bulk builder containing messages and default sender.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bulk result containing summary and individual results.</returns>
    Task<BulkResult<SmsRequest>> SendBulkAsync(BulkSmsBuilder bulkBuilder, CancellationToken ct = default);

    /// <summary>Retrieves an SMS message by its unique identifier.</summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The SMS message result.</returns>
    Task<TResult> GetMessageByIdAsync(string messageId, CancellationToken ct = default);

    /// <summary>Retrieves SMS messages matching the specified filter criteria with pagination.</summary>
    /// <param name="filter">Filter criteria (From, To, DateSentAfter, DateSentBefore, PageSize). Use NextCursor from result as DateSentBefore for next page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated collection of SMS message results.</returns>
    Task<SmsMessageQueryResults<TResult>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default);

    /// <summary>Tests the connection to the SMS service provider.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}

/// <summary>Non-generic SMS service interface returning base <see cref="Result{SmsRequest}" /> for backward compatibility.</summary>
public interface ISmsService : ISmsService<Result<SmsRequest>> { }