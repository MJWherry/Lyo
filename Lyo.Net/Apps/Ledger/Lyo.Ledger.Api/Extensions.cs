using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Mapping;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Ledger.Postgres;
using Lyo.Ledger.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Ledger.Api;

/// <summary>HTTP mapping for the Lyo Ledger API. Register store DI with <c>AddLedgerApiFromConfiguration</c> first.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Ledger Postgres store, CRUD services, and an identity mapper.</summary>
        public IServiceCollection AddLedgerApi(Action<PostgresLedgerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLedgerOptions();
            configure(options);
            return services.AddLedgerApi(options);
        }

        /// <summary>Registers Ledger from configuration section <see cref="PostgresLedgerOptions.SectionName" />.</summary>
        public IServiceCollection AddLedgerApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLedgerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddPostgresLedgerStoreFromConfiguration(configuration, configSectionName);
            return services.AddLedgerApiCore();
        }

        /// <summary>Registers the Ledger Postgres store, CRUD services, and an identity mapper.</summary>
        public IServiceCollection AddLedgerApi(PostgresLedgerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresLedgerStore(options);
            return services.AddLedgerApiCore();
        }

        private IServiceCollection AddLedgerApiCore()
        {
            services.AddLyoCrudServices<LedgerDbContext>();
            services.TryAddSingleton<ILyoMapper, IdentityLyoMapper>();
            return services;
        }
    }

    /// <summary>Maps Ledger CRUD and post/void/AR endpoints. Invoke after <c>AddLedgerApi</c>.</summary>
    public static WebApplication BuildLedgerGroup(this WebApplication app)
    {
        app.CreateBuilder<LedgerDbContext, LedgerAccountEntity, LedgerAccountEntity, LedgerAccountEntity, Guid>("Ledger/Accounts", "Ledger")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance,
                new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<LedgerDbContext, LedgerJournalEntity, LedgerJournalEntity, LedgerJournalEntity, Guid>("Ledger/Journals", "Ledger")
            .WithCrud(ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new())
            .Build();

        app.CreateBuilder<LedgerDbContext, LedgerJournalLineEntity, LedgerJournalLineEntity, LedgerJournalLineEntity, Guid>("Ledger/JournalLines", "Ledger")
            .WithCrud(ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new())
            .Build();

        app.MapPost(
                "/Ledger/Journals/post", async (PostLedgerJournalRequest req, ILedgerStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var journal = new LedgerJournalRecord {
                        Memo = req.Memo,
                        Source = string.IsNullOrWhiteSpace(req.SourceEntityType) || string.IsNullOrWhiteSpace(req.SourceEntityId)
                            ? null
                            : EntityRef.ForKey(req.SourceEntityType, req.SourceEntityId)
                    };

                    var posted = await store.PostJournalAsync(journal, req.Lines ?? [], req.TenantId, ct).ConfigureAwait(false);
                    return Results.Ok(posted);
                })
            .WithTags("Ledger");

        app.MapPost(
                "/Ledger/Journals/{id:guid}/post",
                async (Guid id, Guid? tenantId, ILedgerStore store, CancellationToken ct) => Results.Ok(await store.PostJournalAsync(id, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Ledger");

        app.MapPost(
                "/Ledger/Journals/{id:guid}/void",
                async (Guid id, Guid? tenantId, ILedgerStore store, CancellationToken ct) => Results.Ok(await store.VoidJournalAsync(id, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Ledger");

        app.MapGet(
                "/Ledger/Clients/{clientId:guid}/ar-balance",
                async (Guid clientId, Guid? tenantId, ILedgerStore store, CancellationToken ct)
                    => Results.Ok(await store.GetClientArBalanceAsync(clientId, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Ledger");

        app.MapPost(
                "/Ledger/ChartOfAccounts/ensure", async (Guid? tenantId, ILedgerStore store, CancellationToken ct) => {
                    await store.EnsureChartOfAccountsAsync(tenantId, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithTags("Ledger");

        return app;
    }
}

/// <summary>Body for <c>POST /Ledger/Journals/post</c>.</summary>
public sealed class PostLedgerJournalRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Header memo.</summary>
    public string? Memo { get; set; }

    /// <summary>Source entity type (invoice, payment).</summary>
    public string? SourceEntityType { get; set; }

    /// <summary>Source entity id.</summary>
    public string? SourceEntityId { get; set; }

    /// <summary>Lines that must balance.</summary>
    public List<LedgerJournalLineRecord> Lines { get; set; } = [];
}

/// <summary>Passthrough mapper so query endpoints can use entity-as-both DTOs.</summary>
internal sealed class IdentityLyoMapper : ILyoMapper
{
    public TResult Map<TResult>(object source)
    {
        if (source is TResult typed)
            return typed;

        throw new InvalidOperationException($"No mapping from {source.GetType().Name} to {typeof(TResult).Name}.");
    }

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        if (source is TDest)
            return;

        throw new InvalidOperationException($"No mapping from {typeof(TSource).Name} to {typeof(TDest).Name}.");
    }
}