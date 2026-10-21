using Lyo.Common.Core.Identifiers;
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
    public PostgresContactUsService(ContactUsServiceOptions options, IDbContextFactory<ContactUsDbContext> contextFactory, ILogger<PostgresContactUsService>? logger = null)
        : base(options, logger)
        => _contextFactory = ArgumentHelpers.ThrowIfNullReturn(contextFactory);

    /// <inheritdoc />
    protected override async Task<ContactUsSubmitResult> SubmitCoreAsync(ContactUsRequest request, CancellationToken ct)
    {
        var id = LyoGuid.CreateV7();
        var entity = ContactSubmissionEntity.FromRequest(id, request);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ContactSubmissions.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        Logger.LogInformation("Contact form submitted: {Id} from {Email}", id, request.Email);
        return ContactUsSubmitResult.FromSuccess(id);
    }

    /// <inheritdoc />
    protected override async Task<ContactSubmission?> GetByIdCoreAsync(Guid id, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.ContactSubmissions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
        return entity?.ToSubmission();
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ContactSubmission>> ListCoreAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.ContactSubmissions.AsNoTracking().OrderByDescending(e => e.CreatedTimestamp).ToArrayAsync(ct).ConfigureAwait(false);
        return Array.ConvertAll(entities, static entity => entity.ToSubmission());
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