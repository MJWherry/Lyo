using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Api.Services.Crud.Create;
using Lyo.Cache;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Tests.Postgres;

public sealed class JobPostgresFixture : PostgresServiceFixtureBase<JobContext>
{
    public ICreateService<JobContext> CreateService { get; private set; } = null!;

    public FakeJobEventPublisher FakePublisher { get; } = new();

    public Guid JobDefinitionId { get; private set; }

    public JobService JobService { get; private set; } = null!;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddSingleton<IJobEventPublisher>(_ => FakePublisher);
        services.AddScoped<JobService>();
    }

    protected override async ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        using (var scope = CreateScope()) {
            CreateService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
            await CreateJobDefinitionAsync(cancellationToken);
        }

        using (var scope = CreateScope())
            JobService = scope.ServiceProvider.GetRequiredService<JobService>();

        Assert.IsType<JobLyoMapper>(ServiceProvider.GetRequiredService<ILyoMapper>());
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
}
