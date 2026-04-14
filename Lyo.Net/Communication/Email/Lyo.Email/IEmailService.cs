using Lyo.Common;
using Lyo.Email.Builders;
using Lyo.Email.Models;

namespace Lyo.Email;

/// <summary>Service interface for sending emails via SMTP.</summary>
public interface IEmailService
{
    /// <summary>Sends an email using the provided builder with a custom from address.</summary>
    /// <param name="requestBuilder">The email builder containing the email content and recipients.</param>
    /// <param name="fromAddress">The email address to send from. This overrides any From address set in the builder.</param>
    /// <param name="fromName">Optional display name for the from address.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A Result&lt;EmailRequest&gt; indicating success or failure with details.</returns>
    Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default);

    /// <summary>Sends an email using the provided builder with the default from address from options.</summary>
    /// <param name="requestBuilder">The email builder containing the email content and recipients.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A Result&lt;EmailRequest&gt; indicating success or failure with details.</returns>
    /// <remarks>If the builder has a From address set, it will be used. Otherwise, the default From address from EmailServiceOptions will be used.</remarks>
    Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default);

    /// <summary>Sends an email using the provided EmailRequest.</summary>
    /// <param name="request">The email request containing the email content and recipients.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A Result&lt;EmailRequest&gt; indicating success or failure with details.</returns>
    /// <remarks>If the request has a From address set, it will be used. Otherwise, the default From address from EmailServiceOptions will be used.</remarks>
    Task<Result<EmailRequest>> SendEmailAsync(EmailRequest request, CancellationToken ct = default);

    /// <summary>Sends multiple emails in bulk. Emails are sent sequentially.</summary>
    /// <param name="builders">The list of email builders to send.</param>
    /// <param name="ct">Cancellation token to cancel the operation. If cancelled, processing stops and partial results are returned.</param>
    /// <returns>A list of Result&lt;EmailRequest&gt; objects, one for each email sent.</returns>
    Task<IReadOnlyList<Result<EmailRequest>>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default);

    /// <summary>Sends multiple emails in bulk using a BulkEmailBuilder.</summary>
    /// <param name="bulkRequestBuilder">The bulk email builder containing messages and default sender.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A BulkResult&lt;EmailRequest&gt; containing summary and individual results.</returns>
    Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default);

    /// <summary>Tests the SMTP connection by connecting and authenticating.</summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>True if the connection test succeeds, false otherwise. Throws OperationCanceledException if cancelled.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}