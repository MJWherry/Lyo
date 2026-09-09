using Lyo.Audit.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Audit.Postgres.Tests;

public sealed class AuditPostgresFixture : PostgresServiceFixtureBase<AuditDbContext>
{
    public IAuditRecorder Recorder { get; private set; } = null!;

    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddDbContextFactory<AuditDbContext>(
            opts => opts.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "audit")));

    protected override ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        Recorder = new PostgresAuditRecorder(
            ContextFactory, new EntityRefOptions(), new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantOptional } });

        return ValueTask.CompletedTask;
    }
}
