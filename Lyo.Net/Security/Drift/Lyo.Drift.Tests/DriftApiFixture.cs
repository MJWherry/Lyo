using Lyo.Api;
using Lyo.Api.Middleware;
using Lyo.Cache;
using Lyo.Common.Json;
using Lyo.Drift.Api;
using Lyo.Drift.Postgres;
using Lyo.Drift.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Drift.Tests;

/// <summary>Postgres + TestServer host mapping <see cref="Lyo.Drift.Api.Extensions.BuildDriftGroup" />.</summary>
public sealed class DriftApiFixture : PostgresContainerFixtureBase
{
    public WebApplication App { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => App.Services;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddPostgresDriftManagement(new PostgresDriftOptions {
            ConnectionString = connectionString,
            EnableAutoMigrations = true,
            MaxSnapshotJsonBytes = 1024,
            SnapshotRetention = TimeSpan.FromDays(1)
        });
        builder.Services.AddDriftRetentionService();
        var app = builder.Build();
        app.UseMiddleware<LoggingMiddleware>();
        app.BuildDriftGroup();
        await app.StartAsync(cancellationToken);
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DriftDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        App = app;
        Client = app.GetTestClient();
    }

    protected override async ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        Client.Dispose();
        await App.DisposeAsync();
    }
}
