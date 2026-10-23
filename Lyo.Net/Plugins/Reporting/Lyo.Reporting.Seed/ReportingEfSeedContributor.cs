using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Reporting.Seed;

/// <summary>
/// EF seed of <see cref="ReportDesignTemplates.Samples" /> as report definitions, including parameters, plus one succeeded generation and its parameter values per
/// definition. <see cref="SeedOptions.Count" /> is ignored. Schema <c>reporting</c>.
/// </summary>
public sealed class ReportingEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Reporting";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresReportingOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<ReportingContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new ReportingContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var mapper = new ReportingLyoMapper();
        var now = DateTime.UtcNow;
        graph.Entity(
            ReportDesignTemplates.Samples.Select(sample => {
                var entity = mapper.Map<ReportDefinition>(sample.ToDefinitionReq());
                entity.Id = LyoGuid.CreateV7();
                entity.CreatedTimestamp = now;
                foreach (var parameter in entity.Parameters) {
                    parameter.Id = LyoGuid.CreateV7();
                    parameter.ReportDefinitionId = entity.Id;
                    parameter.CreatedTimestamp = now;
                }

                return entity;
            }));

        graph.After(async (session, ct) => {
            var generations = new List<ReportGeneration>();
            var parameters = new List<ReportGenerationParameter>();
            foreach (var definition in session.Generated<ReportDefinition>()) {
                var generation = new ReportGeneration {
                    Id = LyoGuid.CreateV7(),
                    ReportDefinitionId = definition.Id,
                    ReportDataJson = definition.ReportDataJson,
                    Format = string.IsNullOrWhiteSpace(definition.DefaultFormat) ? nameof(ReportFormat.Html) : definition.DefaultFormat,
                    Status = nameof(ReportGenerationStatus.Succeeded),
                    CreatedBy = "scheduler",
                    CreatedTimestamp = now,
                    StartedTimestamp = now,
                    FinishedTimestamp = now
                };

                generations.Add(generation);
                foreach (var parameter in definition.Parameters) {
                    parameters.Add(
                        new() {
                            Id = LyoGuid.CreateV7(),
                            ReportGenerationId = generation.Id,
                            Key = parameter.Key,
                            Type = parameter.Type,
                            Value = parameter.Value,
                            Description = parameter.Description
                        });
                }
            }

            await session.PersistAsync(generations, ct).ConfigureAwait(false);
            await session.PersistAsync(parameters, ct).ConfigureAwait(false);
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<ReportingContext> ef)
            throw new SeedException("Reporting OnClear requires EfSeedTransport<ReportingContext>.");

        var db = ef.Context;
        await db.ReportGenerationParameters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ReportGenerations.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ReportDefinitionParameters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ReportDefinitions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}