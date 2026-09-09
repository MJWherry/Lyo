using Lyo.ContactUs.Models;

namespace Lyo.ContactUs;

/// <summary>Contract for contact form submissions.</summary>
public interface IContactUsService
{
    /// <summary>Posts a contact form.</summary>
    /// <param name="request">Submitted contact-form payload.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Result that carries the submission id when it succeeds.</returns>
    Task<ContactUsSubmitResult> SubmitAsync(ContactUsRequest request, CancellationToken ct = default);

    /// <summary>Probes the connection to the contact form service.</summary>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when the connection check succeeds; otherwise false.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}