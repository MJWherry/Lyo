using Lyo.Cache;
using Lyo.Query.Services.WhereClause;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Query.Postgres.Tests;

public sealed class FuzzyQueryPostgresFixture : PostgresServiceFixtureBase<FuzzyQueryDbContext>
{
    public IWhereClauseService WhereClause => ServiceProvider.GetRequiredService<IWhereClauseService>();

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddLyoQueryPostgres();
        services.AddDbContextFactory<FuzzyQueryDbContext>(o => o.UseNpgsql(connectionString));
    }

    protected override async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FuzzyQueryDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;", cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}