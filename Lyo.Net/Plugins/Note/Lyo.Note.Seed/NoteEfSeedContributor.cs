using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Note.Postgres;
using Lyo.Note.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Note.Seed;

/// <summary>EF seed of notes, each on a different page from a different person.</summary>
public sealed class NoteEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Subject type written on every seeded row.</summary>
    public const string SubjectType = "page";

    /// <summary>Tenant id written on every seeded row.</summary>
    public static readonly Guid TenantId = new("11111111-1111-7111-8111-111111111111");

    /// <inheritdoc />
    public override string Name => "Note";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresNoteOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<NoteDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new NoteDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(
            options.Count, i => new NoteEntity {
                Id = LyoGuid.CreateV7(),
                TenantId = TenantId,
                SubjectEntityType = SubjectType,
                SubjectEntityId = $"page-{i}",
                ActorEntityType = "person",
                ActorEntityId = $"person-{i}",
                Visibility = EntityRefVisibility.Public,
                CreatedAt = DateTime.UtcNow,
                Content = faker.Lorem.Paragraph()
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is EfSeedTransport<NoteDbContext> ef) {
            await ef.Context.Notes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            return;
        }

        // The unit-test recorder is not an EF context. An empty persist is the clear marker so Replace order stays visible.
        await session.Transport.PersistAsync<NoteEntity>([], ct).ConfigureAwait(false);
    }
}