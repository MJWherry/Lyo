using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Config.Postgres;
using Lyo.Config.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Config.Seed;

/// <summary>EF seed of plaintext config definitions, one binding each, and revision 1 of both. Schema <c>config</c>. Nothing is encrypted.</summary>
public sealed class ConfigEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Config";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresConfigOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<ConfigDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new ConfigDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var definitions = graph.Entity(
            options.Count, i => new ConfigDefinitionEntity {
                Id = LyoGuid.CreateV7(),
                SubjectEntityType = "workspace",
                Key = $"theme.{i}",
                ForValueType = "System.String",
                IsEncrypted = false,
                DefaultValueJson = $"\"{faker.Internet.Color()}\"",
                CreatedTimestamp = now
            });

        var bindings = definitions.ForEach(
            _ => 1, (definition, _) => new ConfigBindingEntity {
                Id = LyoGuid.CreateV7(),
                DefinitionId = definition.Id,
                Key = definition.Key,
                SubjectEntityType = definition.SubjectEntityType,
                SubjectEntityId = $"workspace-{definition.Key}",
                ValueType = definition.ForValueType,
                CreatedTimestamp = now
            });

        definitions.ForEach(
            _ => 1, (definition, _) => new ConfigDefinitionRevisionEntity {
                DefinitionId = definition.Id,
                Revision = 1,
                Key = definition.Key,
                ForValueType = definition.ForValueType,
                IsEncrypted = definition.IsEncrypted,
                DefaultValueJson = definition.DefaultValueJson,
                CreatedTimestamp = now
            });

        bindings.ForEach(
            _ => 1, (binding, _) => new ConfigBindingRevisionEntity {
                BindingId = binding.Id,
                Revision = 1,
                ValueJson = $"\"{faker.Internet.Color()}\"",
                CreatedTimestamp = now
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<ConfigDbContext> ef)
            throw new SeedException("Config OnClear requires EfSeedTransport<ConfigDbContext>.");

        var db = ef.Context;
        await db.ConfigBindingRevisions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ConfigDefinitionRevisions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ConfigBindings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ConfigDefinitions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}