using Lyo.Api.Services.Crud;
using Lyo.Api.Tests.Fixtures;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Tests;

[Collection(ApiPostgresCollection.Name)]
public class EntityLoaderPostgresTests
{
    private readonly ApiPostgresFixture _fixture;

    public EntityLoaderPostgresTests(ApiPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task LoadNestedCollections_WithInclude_AddsIncludeToQuery()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("LoaderTest").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var query = context.Set<JobRun>().AsQueryable();
        var withInclude = loader.LoadNestedCollections(context, query, ["JobDefinition"]);
        var results = await withInclude.Take(5).ToArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(results.Length >= 1);
        foreach (var run in results)
            Assert.NotNull(run.JobDefinition);
    }

    [Fact]
    public async Task LoadIncludes_OnEntity_LoadsNavigation()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("LoadIncludesTest").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var run = await context.Set<JobRun>().FindAsync([runId], TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(run);
        Assert.False(context.Entry(run).Navigation("JobDefinition").IsLoaded);
        await loader.LoadIncludes(context, run, ["JobDefinition"], TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(context.Entry(run).Navigation("JobDefinition").IsLoaded);
        Assert.NotNull(run.JobDefinition);
        Assert.Equal(defId, run.JobDefinition.Id);
    }

    [Fact]
    public void GetReferencedTypes_ReturnsNavigationTypes()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        using var context = factory.CreateDbContext();
        var types = loader.GetReferencedTypes<JobContext, JobRun>(context, ["JobDefinition", "JobRunLogs"]);
        Assert.Contains(typeof(JobDefinition), types);
        Assert.Contains(typeof(JobRunLog), types);
    }

    [Fact]
    public void GetReferencedTypes_WithNestedPath_ReturnsAllTypes()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        using var context = factory.CreateDbContext();
        var types = loader.GetReferencedTypes<JobContext, JobRun>(context, ["JobDefinition.JobParameters"]);
        Assert.Contains(typeof(JobDefinition), types);
        Assert.Contains(typeof(JobParameter), types);
    }
}