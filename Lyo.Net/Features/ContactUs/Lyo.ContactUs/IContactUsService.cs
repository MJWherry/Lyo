using Lyo.ContactUs.Models;

namespace Lyo.ContactUs;

/// <summary>Service interface for contact form submissions.</summary>
public interface IContactUsService
{
    /// <summary>Submits a contact form.</summary>
    /// <param name="request">The contact form request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result containing the submission ID on success.</returns>
    Task<ContactUsSubmitResult> SubmitAsync(ContactUsRequest request, CancellationToken ct = default);

    /// <summary>Tests the connection to the contact form service.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}