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

    /// <summary>Loads one stored submission.</summary>
    /// <param name="id">Submission id returned by <see cref="SubmitAsync" />.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>The stored submission, or null when no row has that id.</returns>
    Task<ContactSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists stored submissions, newest first.</summary>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Every submission, ordered by <see cref="ContactSubmission.CreatedTimestamp" /> descending.</returns>
    Task<IReadOnlyList<ContactSubmission>> ListAsync(CancellationToken ct = default);

    /// <summary>Probes the connection to the contact form service.</summary>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>True when the connection check succeeds; otherwise false.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}