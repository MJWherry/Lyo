using BenchmarkDotNet.Attributes;
using Lyo.Api.Mapping;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query.Root;
using Lyo.Benchmarking;
using Lyo.Cache;
using Lyo.Common.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Testing.Containers;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Query.Benchmarks;

/// <summary>
/// End-to-end root <c>/Query</c> (From/Joins) benchmarks against PostgreSQL (Testcontainers).
/// Isolated so Docker-less CI can filter this class out with BenchmarkDotNet filters.
/// </summary>
[BenchmarkDescription(
    "Root From/Joins Query against PostgreSQL: flat select, left join with fan-out collapse, and From-side paging with exact count. Seeds JobDefinition + JobRun rows.")]
[BenchmarkParameter("Amount", Unit = "rows", Description = "From-side page size (10 or 50).")]
[BenchmarkSla(
    MaxMeanMs = 40,
    Standard = "Root join queries against local PostgreSQL should complete within tens of milliseconds for modest pages.")]
public class RootQueryBenchmarks
{
    private ICreateService<JobContext> _create = null!;
    private PostgresTestContainer _postgres = null!;
    private ServiceProvider _provider = null!;
    private RootQueryEntityRegistry _registry = null!;
    private IRootQueryService<JobContext> _rootQuery = null!;
    private IServiceScope _scope = null!;
    private Guid _seedDefId;

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
        services.AddSingleton(new QueryOptions { DefaultPageSize = 100, MaxPageSize = 2000 });
        _provider = services.BuildServiceProvider();
        using (var migrateScope = _provider.CreateScope()) {
            var factory = migrateScope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
            using var ctx = factory.CreateDbContext();
            ctx.Database.Migrate();
            _registry = RootQueryEntityRegistry.FromDbContext(ctx, [typeof(JobDefinition), typeof(JobRun)]);
        }

        _scope = _provider.CreateScope();
        _rootQuery = _scope.ServiceProvider.GetRequiredService<IRootQueryService<JobContext>>();
        _create = _scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();

        for (var i = 0; i < Math.Max(Amount * 2, 100); i++) {
            var defId = SeedDefinition($"RootSeed-{i}").GetAwaiter().GetResult();
            if (i == 0)
                _seedDefId = defId;
            // Fan-out: first definitions get 2 runs each.
            if (i < Amount) {
                SeedRun(defId, $"user-a-{i}").GetAwaiter().GetResult();
                SeedRun(defId, $"user-b-{i}").GetAwaiter().GetResult();
            }
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
            Description = "root-benchmark",
            Type = "Test",
            WorkerType = "csharp",
            Enabled = true
        };
        var result = await _create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(req, ctx => ctx.Entity.Id = Guid.NewGuid());
        return result.Data!.Id;
    }

    private async Task SeedRun(Guid jobDefinitionId, string createdBy)
    {
        var req = new JobRunReq(jobDefinitionId, createdBy, false);
        await _create.CreateAsync<JobRunReq, JobRun, JobRunRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.State = Lyo.Job.Models.Enums.JobState.Queued;
                ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
            });
    }

    [Benchmark]
    [BenchmarkDescription("Root Query with no joins: sparse Select of Name over a From-side page.")]
    public async Task<int> FlatSelect()
    {
        var req = QueryReqBuilder.New()
            .From("d", nameof(JobDefinition))
            .AddSelects("d.Name")
            .SetPagination(0, Amount)
            .Build();
        var result = await _rootQuery.QueryAsync(req, _registry);
        return result.Items?.Count ?? 0;
    }

    [Benchmark]
    [BenchmarkDescription("Root left join JobDefinition→JobRun with fan-out collapse; page Amount From rows.")]
    public async Task<int> LeftJoinFanOutCollapse()
    {
        var req = QueryReqBuilder.New()
            .From("d", nameof(JobDefinition))
            .Join(
                "r", nameof(JobRun), JoinType.Left, on => {
                    on.Add(new JoinOn { From = "d.Id", To = "r.JobDefinitionId" });
                }, "r")
            .AddSelects("d.Name", "r.CreatedBy")
            .AddSort("d.Name", SortDirection.Asc)
            .SetPagination(0, Amount)
            .Build();
        var result = await _rootQuery.QueryAsync(req, _registry);
        return result.Items?.Count ?? 0;
    }

    [Benchmark]
    [BenchmarkDescription("Root left join with Exact total count (From-side count + join page).")]
    public async Task<int> LeftJoinExactCount()
    {
        var req = QueryReqBuilder.New()
            .From("d", nameof(JobDefinition))
            .Join(
                "r", nameof(JobRun), JoinType.Left, on => {
                    on.Add(new JoinOn { From = "d.Id", To = "r.JobDefinitionId" });
                }, "r")
            .AddSelects("d.Name", "r.CreatedBy")
            .SetPagination(0, Amount)
            .SetTotalCountMode(QueryTotalCountMode.Exact)
            .Build();
        var result = await _rootQuery.QueryAsync(req, _registry);
        return result.Total ?? 0;
    }

    [Benchmark]
    [BenchmarkDescription("Root Query filtered to a single seeded definition with left join.")]
    public async Task<int> FilteredJoinByKey()
    {
        var req = QueryReqBuilder.New()
            .From("d", nameof(JobDefinition))
            .Join(
                "r", nameof(JobRun), JoinType.Left, on => {
                    on.Add(new JoinOn { From = "d.Id", To = "r.JobDefinitionId" });
                }, "r")
            .AddSelects("d.Name", "r.CreatedBy")
            .AddWhere(w => w.Equals("d.Id", _seedDefId))
            .SetPagination(0, Amount)
            .Build();
        var result = await _rootQuery.QueryAsync(req, _registry);
        return result.Items?.Count ?? 0;
    }
}
