using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.Validation.Postgres;
using Lyo.Validation.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Validation.Seed;

/// <summary>EF seed of validation schemas with a small JSON body. Schema <c>validation</c>.</summary>
public sealed class ValidationEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Validation";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresValidationOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<ValidationDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new ValidationDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var now = DateTime.UtcNow;
        graph.Entity(options.Count, i => new ValidationSchemaEntity {
            Id = LyoGuid.CreateV7(),
            Key = $"contact.schema.{i}",
            TargetTypeName = "ContactForm",
            Description = "Requires a name",
            ConstraintsJson = """{"required":["name"]}""",
            CreatedTimestamp = now
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<ValidationDbContext> ef)
            throw new SeedException("Validation OnClear requires EfSeedTransport<ValidationDbContext>.");

        await ef.Context.Schemas.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
