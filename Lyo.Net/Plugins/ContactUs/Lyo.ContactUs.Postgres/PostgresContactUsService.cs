using Lyo.ContactUs.Models;
using Lyo.ContactUs.Postgres.Database;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Lyo.ContactUs.Postgres;

/// <summary>Postgres-backed contact form service.</summary>
public sealed class PostgresContactUsService : ContactUsServiceBase
{
    private readonly IDbContextFactory<ContactUsDbContext> _contextFactory;

    /// <summary>Constructs the <see cref="PostgresContactUsService" /> class.</summary>
    public PostgresContactUsService(
        ContactUsServiceOptions options,
        IDbContextFactory<ContactUsDbContext> contextFactory,
        ILogger<PostgresContactUsService>? logger = null)
        : base(options, logger)
        => _contextFactory = ArgumentHelpers.ThrowIfNullReturn(contextFactory);

    /// <inheritdoc />
    protected override async Task<ContactUsSubmitResult> SubmitCoreAsync(ContactUsRequest request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var entity = ContactSubmissionEntity.FromRequest(id, request);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ContactSubmissions.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        Logger.LogInformation("Contact form submitted: {Id} from {Email}", id, request.Email);
        return ContactUsSubmitResult.FromSuccess(id);
    }

    /// <inheritdoc />
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            return await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Contact form database connection test failed");
            return false;
        }
    }
}