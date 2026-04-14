using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

public sealed record CrudConfiguration<TDbContext, TDbEntity, TRequest>
    where TDbContext : DbContext where TDbEntity : class
{
    // Lifecycle hooks (context-based)
    public Action<GetContext<TDbEntity, TDbContext>>? BeforeGet { get; init; }

    public Action<GetContext<TDbEntity, TDbContext>>? AfterGet { get; init; }

    public Action<CreateContext<TRequest, TDbEntity, TDbContext>>? BeforeCreate { get; init; }

    public Action<CreateContext<TRequest, TDbEntity, TDbContext>>? AfterCreate { get; init; }

    public Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? BeforeUpdate { get; init; }

    public Action<UpdateContext<TRequest, TDbEntity, TDbContext>>? AfterUpdate { get; init; }

    public Action<PatchContext<TDbEntity, TDbContext>>? BeforePatch { get; init; }

    public Action<PatchContext<TDbEntity, TDbContext>>? AfterPatch { get; init; }

    public Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? BeforeUpsert { get; init; }

    public Action<UpsertContext<TRequest, TDbEntity, TDbContext>>? AfterUpsert { get; init; }

    public Action<DeleteContext<TDbEntity, TDbContext>>? BeforeDelete { get; init; }

    public Action<DeleteContext<TDbEntity, TDbContext>>? AfterDelete { get; init; }

    // Configuration options
    public string[]? DeleteIncludes { get; init; }

    public MetadataConfiguration<TDbContext, TDbEntity> Metadata { get; init; } = new();

    // Per-endpoint authorization (null = use builder default)
    public EndpointAuth? QueryAuth { get; init; }

    public EndpointAuth? QueryHistoryAuth { get; init; }

    public EndpointAuth? GetAuth { get; init; }

    public EndpointAuth? CreateAuth { get; init; }

    public EndpointAuth? CreateBulkAuth { get; init; }

    public EndpointAuth? UpdateAuth { get; init; }

    public EndpointAuth? UpdateBulkAuth { get; init; }

    public EndpointAuth? PatchAuth { get; init; }

    public EndpointAuth? PatchBulkAuth { get; init; }

    /// <summary>When set, restricts <see cref="PatchRequest.Properties"/> for both Patch and PatchBulk (same rules as typed hooks).</summary>
    public PatchPropertyAuthorization? PatchPropertyAuthorization { get; init; }

    public EndpointAuth? UpsertAuth { get; init; }

    public EndpointAuth? UpsertBulkAuth { get; init; }

    public EndpointAuth? DeleteAuth { get; init; }

    public EndpointAuth? DeleteBulkAuth { get; init; }

    public EndpointAuth? ExportAuth { get; init; }

    public EndpointAuth? MetadataAuth { get; init; }
}