using Lyo.ChangeTracker.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ChangeTracker.Postgres.Tests;

public sealed class ChangeTrackerPostgresFixture : PostgresServiceFixtureBase<ChangeTrackerDbContext>
{
    public IChangeTracker ChangeTracker { get; private set; } = null!;

    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddDbContextFactory<ChangeTrackerDbContext>(
            opts => opts.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresChangeTrackerOptions.Schema)));

    protected override ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        ChangeTracker = new PostgresChangeTracker(
            ContextFactory, new EntityRefOptions(),
            new PostgresChangeTrackerOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantOptional } });

        return ValueTask.CompletedTask;
    }
}
