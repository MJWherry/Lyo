using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Api.Services.Crud.Create;
using Lyo.Cache;
using Lyo.Common.Records;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Testing.Containers;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Postgres.Tests;

public sealed class JobPostgresFixture : PostgresContainerFixtureBase
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public ICreateService<JobContext> CreateService { get; private set; } = null!;

    public FakeJobEventPublisher FakePublisher { get; private set; } = null!;

    public Guid JobDefinitionId { get; private set; }

    public JobService JobService { get; private set; } = null!;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        FakePublisher = new();
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.ConfigureJobMappings();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        services.AddSingleton<IJobEventPublisher>(_ => FakePublisher);
        services.AddScoped<JobService>();
        ServiceProvider = services.BuildServiceProvider();
        using (var scope = ServiceProvider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
            await using var context = await factory.CreateDbContextAsync(cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }

        using (var scope = ServiceProvider.CreateScope()) {
            CreateService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
            await CreateJobDefinitionAsync(cancellationToken);
        }

        using (var scope = ServiceProvider.CreateScope())
            JobService = scope.ServiceProvider.GetRequiredService<JobService>();
    }

    private async Task CreateJobDefinitionAsync(CancellationToken cancellationToken)
    {
        var req = new JobDefinitionReq {
            Name = "TestJob",
            Description = "Integration test job",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true
        };

        var result = await CreateService.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
            }, ct: cancellationToken);

        Assert.True(result.IsSuccess);
        JobDefinitionId = result.Data!.Id;
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}