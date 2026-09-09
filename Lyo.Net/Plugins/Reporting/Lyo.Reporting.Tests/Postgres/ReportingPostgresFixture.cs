using Lyo.Api;
using Lyo.Api.Mapping;
using Lyo.Cache;
using Lyo.FileStorage.Abstractions;
using Lyo.IO.Temp;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Reporting.Tests.Postgres;

public sealed class ReportingPostgresFixture : PostgresServiceFixtureBase<ReportingContext>
{
    public FakeFileStorageService FakeFileStorage { get; } = new();

    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddIOTempService();
        services.AddSingleton<IFileStorageService>(FakeFileStorage);
        // Much of the suite generates from ad-hoc ReportDataJson, which the shipped default now rejects.
        services.AddPostgresReportingManagement(
            new PostgresReportingOptions { ConnectionString = connectionString, EnableAutoMigrations = true, AllowAdHocGeneration = true });
    }

    protected override ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        Assert.IsType<ReportingLyoMapper>(ServiceProvider.GetRequiredService<ILyoMapper>());
        return ValueTask.CompletedTask;
    }
}
