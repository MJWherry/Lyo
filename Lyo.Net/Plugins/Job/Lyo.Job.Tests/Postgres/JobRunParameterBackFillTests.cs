using Lyo.Api;
using Lyo.Cache;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Parameters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// A manual run supplies only the parameters the caller cares about, so <c>CreateJobRun</c> back-fills the rest from the definition. Expression defaults are resolved on the way
/// in,
/// which is what lets a <c>System.DateTime</c> parameter default to "yesterday" and still satisfy the type check that follows.
/// </summary>
[Trait("Category", "Integration")]
public class JobRunParameterBackFillTests
{
    private const string ExpressionTemplate = "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}";

    private readonly JobPostgresFixture _fixture;

    public JobRunParameterBackFillTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExpressionDefault_IsResolvedIntoTheRunSnapshot()
    {
        using var sp = BuildServiceProvider(_ => "2026-08-25");
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp, ExpressionParameter, LiteralParameter);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false), TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        var asOfDate = Assert.Single(created.Data!.JobRunParameters!, p => p.Key == "AsOfDate");
        Assert.Equal(LyoTypeInfo.DateTime.FullName, asOfDate.Type);
        Assert.Equal("\"2026-08-25\"", asOfDate.Value);

        // Literal defaults are back-filled by the same pass, so a caller omitting everything still gets the full declared set.
        Assert.Equal("200", Assert.Single(created.Data.JobRunParameters!, p => p.Key == "PageSize").Value);
    }

    [Fact]
    public async Task SuppliedValue_IsNotOverwrittenByTheExpressionDefault()
    {
        using var sp = BuildServiceProvider(_ => "2026-08-25");
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp, ExpressionParameter);
        var request = new JobRunReq(definitionId, "test-user", false) {
            JobRunParameters = {
                new() {
                    Key = "AsOfDate",
                    Type = LyoTypeInfo.DateTime.FullName,
                    Value = "\"2020-01-01\"",
                    Enabled = true
                }
            }
        };

        var created = await scope.ServiceProvider.GetRequiredService<JobService>().CreateJobRun(request, TestContext.Current.CancellationToken);
        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        Assert.Equal("\"2020-01-01\"", Assert.Single(created.Data!.JobRunParameters!, p => p.Key == "AsOfDate").Value);
    }

    [Fact]
    public async Task ExpressionDefault_ResolvesThroughTheRegisteredFormatter()
    {
        // The resolver the API actually runs with comes from AddFormatterService, so exercise that wiring instead of only a stub.
        using var sp = BuildServiceProvider(null, services => services.AddFormatterService());
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp, ExpressionParameter);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false), TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        var expected = $"\"{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}\"";
        Assert.Equal(expected, Assert.Single(created.Data!.JobRunParameters!, p => p.Key == "AsOfDate").Value);
    }

    [Fact]
    public async Task ExpressionDefaultWithNoResolver_FailsWithAnExplicitMessage()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp, ExpressionParameter);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false), TestContext.Current.CancellationToken);

        // Silently dropping the parameter would hand the worker a null date; the caller needs to know the host is missing the formatter.
        Assert.False(created.IsSuccess);
        Assert.Contains("no template resolver is registered", created.Error!.GetFullMessage());
    }

    [Fact]
    public async Task ExpressionDefaultRenderingTheWrongType_IsRejected()
    {
        using var sp = BuildServiceProvider(_ => "not a date");
        using var scope = sp.CreateScope();
        var definitionId = await CreateDefinitionAsync(sp, ExpressionParameter);
        var created = await scope.ServiceProvider.GetRequiredService<JobService>()
            .CreateJobRun(new(definitionId, "test-user", false), TestContext.Current.CancellationToken);

        Assert.False(created.IsSuccess);
        Assert.Contains("not a valid System.DateTime", created.Error!.GetFullMessage());
    }

    private static JobParameter ExpressionParameter(Guid definitionId)
        => new() {
            Id = LyoGuid.CreateCombPostgres(),
            JobDefinitionId = definitionId,
            Key = "AsOfDate",
            Type = LyoTypeInfo.DateTime.FullName,
            DefaultKind = nameof(LyoParameterDefaultKind.Expression),
            DefaultTemplate = ExpressionTemplate,
            CreatedTimestamp = DateTime.UtcNow
        };

    private static JobParameter LiteralParameter(Guid definitionId)
        => new() {
            Id = LyoGuid.CreateCombPostgres(),
            JobDefinitionId = definitionId,
            Key = "PageSize",
            Type = LyoTypeInfo.Int.FullName,
            Value = "200",
            CreatedTimestamp = DateTime.UtcNow
        };

    private ServiceProvider BuildServiceProvider(LyoTemplateResolver? resolver = null, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _fixture.ConnectionString });
        services.AddSingleton<IJobEventPublisher>(_ => new FakeJobEventPublisher());
        services.AddScoped<JobService>();
        if (resolver is not null)
            services.AddSingleton(resolver);

        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> CreateDefinitionAsync(ServiceProvider sp, params Func<Guid, JobParameter>[] parameters)
    {
        var definitionId = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync(sp);
        db.JobDefinitions.Add(
            new() {
                Id = definitionId,
                Name = $"BackFill-{definitionId:N}"[..32],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        foreach (var parameter in parameters)
            db.JobParameters.Add(parameter(definitionId));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return definitionId;
    }

    private static async Task<JobContext> CreateDbContextAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }
}
