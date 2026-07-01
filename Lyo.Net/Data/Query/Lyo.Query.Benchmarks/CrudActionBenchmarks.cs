using BenchmarkDotNet.Attributes;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Benchmarking;
using Lyo.Cache;
using Lyo.Common.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Common.Request;
using Lyo.Testing.Containers;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Query.Benchmarks;

/// <summary>
/// End-to-end CRUD benchmarks for the Lyo.Api query pipeline against a real PostgreSQL database (Testcontainers). Requires Docker; the container is started in
/// <see cref="GlobalSetup" />. Isolated in its own class so it can be excluded with a BenchmarkDotNet filter when Docker is unavailable.
/// </summary>
[BenchmarkDescription(
    "End-to-end CRUD against a real PostgreSQL database (Testcontainers): paged Query, single Get, Patch, Create, and create-then-Delete of JobDefinition rows. The table is pre-seeded with at least 2x Amount (min 100) rows so paging is exercised.")]
[BenchmarkParameter("Amount", Unit = "rows", Description = "Page size requested by the Query case (10 or 50); also scales the seeded row count.")]
[BenchmarkSla(
    MaxMeanMs = 25,
    Standard = "Single CRUD operations against a local PostgreSQL (paged read, get, patch, create, delete) should complete within tens of milliseconds end-to-end.")]
public class CrudActionBenchmarks
{
    private ICreateService<JobContext> _create = null!;
    private IDeleteService<JobContext> _delete = null!;
    private IPatchService<JobContext> _patch = null!;
    private PostgresTestContainer _postgres = null!;
    private ServiceProvider _provider = null!;
    private IQueryService<JobContext> _query = null!;
    private IServiceScope _scope = null!;
    private Guid _seedId;

    [Params(10, 50)]
    public int Amount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _postgres = new();
        _postgres.StartAsync().GetAwaiter().GetResult();
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _postgres.ConnectionString, EnableAutoMigrations = true });
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        _provider = services.BuildServiceProvider();
        using (var migrateScope = _provider.CreateScope()) {
            var factory = migrateScope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
            using var ctx = factory.CreateDbContext();
            ctx.Database.Migrate();
        }

        _scope = _provider.CreateScope();
        _query = _scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        _patch = _scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();
        _create = _scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        _delete = _scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();

        // Seed enough definitions to exercise paging.
        for (var i = 0; i < Math.Max(Amount * 2, 100); i++) {
            var id = SeedDefinition($"Seed-{i}").GetAwaiter().GetResult();
            if (i == 0)
                _seedId = id;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
        _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task<Guid> SeedDefinition(string name)
    {
        var req = new JobDefinitionReq {
            Name = name,
            Description = "benchmark",
            Type = "Test",
            WorkerType = "csharp",
            Enabled = true
        };

        var result = await _create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(req, ctx => ctx.Entity.Id = Guid.NewGuid());
        return result.Data!.Id;
    }

    [Benchmark]
    [BenchmarkDescription("Paged Query of Amount JobDefinitions ordered by Name (full read pipeline incl. mapping).")]
    public async Task<int> Query()
    {
        var req = new QueryReq { Amount = Amount };
        var result = await _query.Query<JobDefinition, JobDefinitionRes>(req, d => d.Name, SortDirection.Asc);
        return result.Items?.Count ?? 0;
    }

    [Benchmark]
    [BenchmarkDescription("Get a single seeded JobDefinition by primary key.")]
    public async Task<JobDefinitionRes?> Get() => await _query.Get<JobDefinition, JobDefinitionRes>([_seedId]);

    [Benchmark]
    [BenchmarkDescription("Patch one property of a single seeded JobDefinition by key.")]
    public async Task<bool> Patch()
    {
        var request = new PatchRequest(new[] { new object[] { _seedId } }) { Properties = { ["Description"] = $"patched-{Guid.NewGuid():N}" } };
        var result = await _patch.PatchAsync<JobDefinition, JobDefinitionRes>(request);
        return result.IsSuccess;
    }

    [Benchmark]
    [BenchmarkDescription("Create a new JobDefinition row (insert + mapping).")]
    public async Task<bool> Create()
    {
        var req = new JobDefinitionReq {
            Name = $"Created-{Guid.NewGuid():N}",
            Type = "Test",
            WorkerType = "csharp",
            Enabled = true
        };

        var result = await _create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(req, ctx => ctx.Entity.Id = Guid.NewGuid());
        return result.IsSuccess;
    }

    [Benchmark]
    [BenchmarkDescription("Create then immediately delete a JobDefinition (insert + delete round-trip).")]
    public async Task<bool> CreateThenDelete()
    {
        var id = Guid.NewGuid();
        var req = new JobDefinitionReq {
            Name = $"Temp-{id:N}",
            Type = "Test",
            WorkerType = "csharp",
            Enabled = true
        };

        await _create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(req, ctx => ctx.Entity.Id = id);
        var result = await _delete.DeleteAsync<JobDefinition, JobDefinitionRes>([id]);
        return result.IsSuccess;
    }
}