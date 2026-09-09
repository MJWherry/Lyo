using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Result;

namespace Lyo.Email;

/// <summary>Contract for sending mail over SMTP.</summary>
public interface IEmailService
{
    /// <summary>Sends using <paramref name="requestBuilder" /> and an explicit from address.</summary>
    /// <param name="requestBuilder">Builder holding body and recipients.</param>
    /// <param name="fromAddress">Sender address; overrides any From already on the builder.</param>
    /// <param name="fromName">Optional display name for that sender.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A Result&lt;EmailRequest&gt; with success or failure details.</returns>
    Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default);

    /// <summary>Sends using <paramref name="requestBuilder" /> and the default from address from options.</summary>
    /// <param name="requestBuilder">Builder holding body and recipients.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A Result&lt;EmailRequest&gt; with success or failure details.</returns>
    /// <remarks>A From already on the builder wins; otherwise EmailServiceOptions supplies the default From.</remarks>
    Task<Result<EmailRequest>> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default);

    /// <summary>Sends a populated EmailRequest.</summary>
    /// <param name="request">Request holding body and recipients.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A Result&lt;EmailRequest&gt; with success or failure details.</returns>
    /// <remarks>A From already on the request wins; otherwise EmailServiceOptions supplies the default From.</remarks>
    Task<Result<EmailRequest>> SendEmailAsync(EmailRequest request, CancellationToken ct = default);

    /// <summary>Sends many emails one after another.</summary>
    /// <param name="builders">Builders to send.</param>
    /// <param name="ct">Token used to cancel. Cancellation stops further sends and returns what finished.</param>
    /// <returns>One Result&lt;EmailRequest&gt; per message.</returns>
    Task<IReadOnlyList<Result<EmailRequest>>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default);

    /// <summary>Sends a batch described by a BulkEmailBuilder.</summary>
    /// <param name="bulkRequestBuilder">Builder holding messages and a default sender.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A BulkResult&lt;EmailRequest&gt; with a summary and per-message results.</returns>
    Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default);

    /// <summary>Connects and authenticates to verify SMTP.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True on a successful probe; false otherwise. Throws OperationCanceledException if cancelled.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}