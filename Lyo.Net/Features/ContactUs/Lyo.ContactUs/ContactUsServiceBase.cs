using Lyo.ContactUs.Models;
using Microsoft.Extensions.Logging;
using static Lyo.ContactUs.ContactUsErrorCodes;

namespace Lyo.ContactUs;

/// <summary>Base implementation of contact form service with validation and logging.</summary>
public abstract class ContactUsServiceBase : IContactUsService
{
    /// <summary>Gets the logger instance.</summary>
    protected ILogger? Logger { get; }

    /// <summary>Gets the service options.</summary>
    protected ContactUsServiceOptions Options { get; }

    /// <summary>Initializes a new instance of the <see cref="ContactUsServiceBase" /> class.</summary>
    protected ContactUsServiceBase(ContactUsServiceOptions options, ILogger? logger = null)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContactUsSubmitResult> SubmitAsync(ContactUsRequest request, CancellationToken ct = default)
    {
        try {
            ct.ThrowIfCancellationRequested();
            var validation = ValidateRequest(request);
            if (!validation.IsSuccess)
                return ContactUsSubmitResult.FromError(validation.Errors?[0].Message ?? "Validation failed", ValidationFailed, validation.Errors?[0].Exception);

            return await SubmitCoreAsync(request, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            return ContactUsSubmitResult.FromError("Operation was cancelled", OperationCancelled);
        }
        catch (Exception ex) {
            Logger?.LogError(ex, "Failed to submit contact form from {Email}", request?.Email);
            return ContactUsSubmitResult.FromException(ex, SubmitFailed);
        }
    }

    /// <inheritdoc />
    public abstract Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>Validates the contact form request.</summary>
    protected virtual ContactUsSubmitResult ValidateRequest(ContactUsRequest request)
    {
        if (request == null)
            return ContactUsSubmitResult.FromError("Request cannot be null", ValidationFailed);

        if (string.IsNullOrWhiteSpace(request.Name))
            return ContactUsSubmitResult.FromError("Name is required", ValidationFailed);

        if (string.IsNullOrWhiteSpace(request.Email))
            return ContactUsSubmitResult.FromError("Email is required", ValidationFailed);

        if (request.Email.Length > 320)
            return ContactUsSubmitResult.FromError("Email is too long", ValidationFailed);

        if (string.IsNullOrWhiteSpace(request.Subject))
            return ContactUsSubmitResult.FromError("Subject is required", ValidationFailed);

        if (string.IsNullOrWhiteSpace(request.Message))
            return ContactUsSubmitResult.FromError("Message is required", ValidationFailed);

        if (request.Message.Length < Options.MinMessageLength)
            return ContactUsSubmitResult.FromError($"Message must be at least {Options.MinMessageLength} characters", ValidationFailed);

        if (request.Message.Length > Options.MaxMessageLength)
            return ContactUsSubmitResult.FromError($"Message must be at most {Options.MaxMessageLength} characters", ValidationFailed);

        return ContactUsSubmitResult.FromSuccess(Guid.Empty); // Validation passed; caller invokes SubmitCoreAsync
    }

    /// <summary>Submits the contact form (storage-specific implementation).</summary>
    protected abstract Task<ContactUsSubmitResult> SubmitCoreAsync(ContactUsRequest request, CancellationToken ct);
}