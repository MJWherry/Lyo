using Lyo.Api.Services.Crud;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common.Enums;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Tests.Services.Validation;

[Collection(ApiPostgresCollection.Name)]
public sealed class ProjectedQueryModelValidatorTests
{
    private readonly ApiPostgresFixture _fixture;

    public ProjectedQueryModelValidatorTests(ApiPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Validate_ValidWhereAndSort_Succeeds()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var result = ProjectedQueryModelValidator.Validate(
            new ProjectedQueryValidatorInput<JobContext, JobDefinition> {
                Db = db,
                Loader = loader,
                Filter = filter,
                PathCache = new QueryPathValidationCache(),
                Include = [],
                SortBy = [new SortBy("Name", SortDirection.Asc)],
                Where = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "x")
            });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Validate_InvalidWhereField_Fails()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var result = ProjectedQueryModelValidator.Validate(
            new ProjectedQueryValidatorInput<JobContext, JobDefinition> {
                Db = db,
                Loader = loader,
                Filter = filter,
                PathCache = new QueryPathValidationCache(),
                Include = [],
                SortBy = [],
                Where = WhereClauseBuilder.Condition("NotAField", ComparisonOperatorEnum.Equals, "x")
            });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => e.Message.Contains("Where field", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_InvalidSortField_Fails()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var result = ProjectedQueryModelValidator.Validate(
            new ProjectedQueryValidatorInput<JobContext, JobDefinition> {
                Db = db,
                Loader = loader,
                Filter = filter,
                PathCache = new QueryPathValidationCache(),
                Include = [],
                SortBy = [new SortBy("Nope", SortDirection.Asc)],
                Where = null
            });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => e.Message.Contains("Sort field", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_InvalidInclude_Fails()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var result = ProjectedQueryModelValidator.Validate(
            new ProjectedQueryValidatorInput<JobContext, JobDefinition> {
                Db = db,
                Loader = loader,
                Filter = filter,
                PathCache = new QueryPathValidationCache(),
                Include = ["NotANavigation"],
                SortBy = [],
                Where = null
            });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => e.Code == Lyo.Api.Models.Constants.ApiErrorCodes.InvalidInclude);
    }

    [Fact]
    public void Validate_IncludeWithoutDb_Fails()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();

        var result = ProjectedQueryModelValidator.Validate(
            new ProjectedQueryValidatorInput<JobContext, JobDefinition> {
                Db = null,
                Loader = loader,
                Filter = filter,
                PathCache = new QueryPathValidationCache(),
                Include = ["JobRuns"],
                SortBy = [],
                Where = null
            });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => e.Message.Contains("Database context", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_ValidInclude_Succeeds()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var loader = scope.ServiceProvider.GetRequiredService<IEntityLoaderService>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var result = ProjectedQueryModelValidator.Validate(
            new ProjectedQueryValidatorInput<JobContext, JobDefinition> {
                Db = db,
                Loader = loader,
                Filter = filter,
                PathCache = new QueryPathValidationCache(),
                Include = ["JobRuns"],
                SortBy = [],
                Where = null
            });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PathCache_ReusesFilterValidation()
    {
        using var scope = _fixture.CreateScope();
        var filter = scope.ServiceProvider.GetRequiredService<IWhereClauseService>();
        var cache = new QueryPathValidationCache();

        Assert.True(cache.TryValidateFilterPropertyPath<JobDefinition>(filter, "Name", out _));
        Assert.True(cache.TryValidateFilterPropertyPath<JobDefinition>(filter, "Name", out _));
        Assert.False(cache.TryValidateFilterPropertyPath<JobDefinition>(filter, "Missing", out var msg));
        Assert.False(string.IsNullOrWhiteSpace(msg));
        await Task.CompletedTask;
    }
}
