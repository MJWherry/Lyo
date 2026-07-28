using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Cache;
using Lyo.FileStorage.Abstractions;
using Lyo.IO.Temp;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Reporting.Tests.Postgres;

public sealed class ReportingPostgresFixture : PostgresContainerFixtureBase
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public FakeFileStorageService FakeFileStorage { get; private set; } = null!;

    public IServiceScope CreateScope() => ServiceProvider.CreateScope();

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        FakeFileStorage = new();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddIOTempService();
        services.AddSingleton<IFileStorageService>(FakeFileStorage);
        services.AddPostgresReportingManagement(new PostgresReportingOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        ServiceProvider = services.BuildServiceProvider();
        using (var scope = ServiceProvider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingContext>>();
            await using var context = await factory.CreateDbContextAsync(cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }

        Assert.IsType<ReportingLyoMapper>(ServiceProvider.GetRequiredService<ILyoMapper>());
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}