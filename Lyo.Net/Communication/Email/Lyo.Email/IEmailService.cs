using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Result;

namespace Lyo.Email;

/// <summary>Contract for sending mail. A host registers one provider, SMTP or SendGrid, as this service.</summary>
public interface IEmailService
{
    /// <summary>Sends using <paramref name="requestBuilder" /> and an explicit from address.</summary>
    /// <param name="requestBuilder">Builder holding body and recipients.</param>
    /// <param name="fromAddress">Sender address. Replaces any From already on the builder when it is not whitespace.</param>
    /// <param name="fromName">Display name for that sender. The address is used when this is omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>The send outcome. A cancelled send is a failed result, not a thrown exception.</returns>
    Task<EmailResult> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default);

    /// <summary>Sends using <paramref name="requestBuilder" />. A From on the builder wins. Otherwise the provider options supply the default From.</summary>
    /// <param name="requestBuilder">Builder holding body and recipients.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>The send outcome. The result still carries the From that was used, including when the send fails.</returns>
    Task<EmailResult> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default);

    /// <summary>Sends a populated request. A null From on the request stays null on the result. The provider may still use its default From on the wire.</summary>
    /// <param name="request">Request holding body and recipients.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>The send outcome.</returns>
    Task<EmailResult> SendEmailAsync(EmailRequest request, CancellationToken ct = default);

    /// <summary>Sends many emails. An empty list returns an empty result and does not contact the provider.</summary>
    /// <param name="builders">Builders to send.</param>
    /// <param name="ct">Token used to cancel. Cancellation stops further sends and returns what finished.</param>
    /// <returns>One result per message that was built or sent.</returns>
    Task<IReadOnlyList<EmailResult>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default);

    /// <summary>Sends a batch described by a bulk builder.</summary>
    /// <param name="bulkRequestBuilder">Builder holding messages and a default sender.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A bulk result with a summary and per-message results.</returns>
    Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default);

    /// <summary>Checks that the provider can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True on a successful probe. False when the probe fails. Throws <see cref="OperationCanceledException" /> when cancelled.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}
