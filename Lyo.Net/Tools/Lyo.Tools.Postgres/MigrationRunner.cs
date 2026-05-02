using Lyo.Audit.Postgres.Database;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Comic.Postgres.Database;
using Lyo.Comment.Postgres.Database;
using Lyo.Config.Postgres.Database;
using Lyo.ContactUs.Postgres.Database;
using Lyo.Discord.Postgres.Database;
using Lyo.Email.Postgres.Database;
using Lyo.Endato.Postgres.Database;
using Lyo.Favorite.Postgres.Database;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Job.Postgres.Database;
using Lyo.Note.Postgres.Database;
using Lyo.People.Postgres.Database;
using Lyo.Rating.Postgres.Database;
using Lyo.ShortUrl.Postgres.Database;
using Lyo.Sms.Postgres.Database;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.Tag.Postgres.Database;
using Lyo.Web.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres;

/// <summary>Applied/pending status for a single migration.</summary>
public sealed record MigrationStatus(string Name, bool IsApplied);

/// <summary>
/// Runs EF Core migrations on demand for any postgres context. Reads the connection string from ConnectionStringProvider at call time, so changes made via the menu take
/// effect immediately.
/// </summary>
public sealed class MigrationRunner
{
    private readonly ConnectionStringProvider _connStr;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(ConnectionStringProvider connStr, ILogger<MigrationRunner> logger)
    {
        _connStr = connStr;
        _logger = logger;
    }

    public Task RunAuditAsync(CancellationToken ct = default) => MigrateLatestAsync<AuditDbContext>("audit", ct);

    public Task RunChangeTrackerAsync(CancellationToken ct = default) => MigrateLatestAsync<ChangeTrackerDbContext>("change_tracker", ct);

    public Task RunComicAsync(CancellationToken ct = default) => MigrateLatestAsync<ComicDbContext>("comic", ct);

    public Task RunCommentAsync(CancellationToken ct = default) => MigrateLatestAsync<CommentDbContext>("comment", ct);

    public Task RunConfigAsync(CancellationToken ct = default) => MigrateLatestAsync<ConfigDbContext>("config", ct);

    public Task RunContactUsAsync(CancellationToken ct = default) => MigrateLatestAsync<ContactUsDbContext>("contact", ct);

    public Task RunDiscordAsync(CancellationToken ct = default) => MigrateLatestAsync<DiscordDbContext>("discord", ct);

    public Task RunEmailAsync(CancellationToken ct = default) => MigrateLatestAsync<EmailDbContext>("email", ct);

    public Task RunEndatoAsync(CancellationToken ct = default) => MigrateLatestAsync<EndatoDbContext>("endato", ct);

    public Task RunFavoriteAsync(CancellationToken ct = default) => MigrateLatestAsync<FavoriteDbContext>("favorite", ct);

    public Task RunFileMetadataStoreAsync(CancellationToken ct = default) => MigrateLatestAsync<FileMetadataStoreDbContext>("filestore", ct);

    public Task RunHomeInventoryAsync(CancellationToken ct = default) => MigrateLatestAsync<HomeInventoryDbContext>("home_inventory", ct);

    public Task RunJobAsync(CancellationToken ct = default) => MigrateLatestAsync<JobContext>("job", ct);

    public Task RunNoteAsync(CancellationToken ct = default) => MigrateLatestAsync<NoteDbContext>("note", ct);

    public Task RunPeopleAsync(CancellationToken ct = default) => MigrateLatestAsync<PeopleDbContext>("people", ct);

    public Task RunRatingAsync(CancellationToken ct = default) => MigrateLatestAsync<RatingDbContext>("rating", ct);

    public Task RunReportingAsync(CancellationToken ct = default) => MigrateLatestAsync<ReportingDbContext>("report", ct);

    public Task RunShortUrlAsync(CancellationToken ct = default) => MigrateLatestAsync<ShortUrlDbContext>("url", ct);

    public Task RunSmsAsync(CancellationToken ct = default) => MigrateLatestAsync<SmsDbContext>("sms", ct);

    public Task RunSmsTwilioAsync(CancellationToken ct = default) => MigrateLatestAsync<TwilioSmsDbContext>("sms", ct);

    public Task RunTagAsync(CancellationToken ct = default) => MigrateLatestAsync<TagDbContext>("tag", ct);

    public async Task RunAllAsync(CancellationToken ct = default)
    {
        await RunAuditAsync(ct);
        await RunChangeTrackerAsync(ct);
        await RunComicAsync(ct);
        await RunCommentAsync(ct);
        await RunConfigAsync(ct);
        await RunContactUsAsync(ct);
        await RunDiscordAsync(ct);
        await RunEmailAsync(ct);
        await RunEndatoAsync(ct);
        await RunFavoriteAsync(ct);
        await RunFileMetadataStoreAsync(ct);
        await RunHomeInventoryAsync(ct);
        await RunJobAsync(ct);
        await RunNoteAsync(ct);
        await RunPeopleAsync(ct);
        await RunRatingAsync(ct);
        await RunReportingAsync(ct);
        await RunShortUrlAsync(ct);
        await RunSmsAsync(ct);
        await RunSmsTwilioAsync(ct);
        await RunTagAsync(ct);
    }

    /// <summary>Applies all pending migrations for the given context to the latest version.</summary>
    public async Task MigrateLatestAsync<TContext>(string schema, CancellationToken ct)
        where TContext : DbContext
    {
        _logger.LogInformation("Running migrations for {Context} (schema: {Schema})", typeof(TContext).Name, schema);
        try {
            await using var context = CreateContext<TContext>(schema);
            await EnsureSchemaAsync(context, schema, ct);
            await context.Database.MigrateAsync(ct);
            _logger.LogInformation("Migrations complete for {Context}", typeof(TContext).Name);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Migrations failed for {Context}", typeof(TContext).Name);
            throw;
        }
    }

    /// <summary>Migrates to a specific target migration (can be older than current to roll back). Pass "0" or Migration.InitialDatabase to roll back all migrations.</summary>
    public async Task MigrateToAsync<TContext>(string schema, string targetMigration, CancellationToken ct)
        where TContext : DbContext
    {
        _logger.LogInformation("Migrating {Context} to target '{Target}'", typeof(TContext).Name, targetMigration);
        try {
            await using var context = CreateContext<TContext>(schema);
            await EnsureSchemaAsync(context, schema, ct);
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(targetMigration, ct);
            _logger.LogInformation("Migration to '{Target}' complete for {Context}", targetMigration, typeof(TContext).Name);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Migration to '{Target}' failed for {Context}", targetMigration, typeof(TContext).Name);
            throw;
        }
    }

    /// <summary>Returns all migrations defined in the assembly with applied/pending status.</summary>
    public async Task<IReadOnlyList<MigrationStatus>> GetStatusAsync<TContext>(string schema, CancellationToken ct)
        where TContext : DbContext
    {
        await using var context = CreateContext<TContext>(schema);
        var all = context.Database.GetMigrations().ToList();
        var applied = (await context.Database.GetAppliedMigrationsAsync(ct)).ToHashSet(StringComparer.Ordinal);
        return all.Select(m => new MigrationStatus(m, applied.Contains(m))).ToList();
    }

    /// <summary>Returns the name of the last applied migration, or null if none have been applied.</summary>
    public async Task<string?> GetCurrentVersionAsync<TContext>(string schema, CancellationToken ct)
        where TContext : DbContext
    {
        await using var context = CreateContext<TContext>(schema);
        var applied = await context.Database.GetAppliedMigrationsAsync(ct);
        return applied.LastOrDefault();
    }

    private TContext CreateContext<TContext>(string schema)
        where TContext : DbContext
    {
        var connStr = _connStr.GetOrThrow();
        var options = new DbContextOptionsBuilder<TContext>().UseNpgsql(connStr, n => n.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    private static async Task EnsureSchemaAsync(DbContext context, string schema, CancellationToken ct)
    {
        var safe = schema.Replace("\"", "\"\"");
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{safe}\";", ct);
#pragma warning restore EF1002
    }
}