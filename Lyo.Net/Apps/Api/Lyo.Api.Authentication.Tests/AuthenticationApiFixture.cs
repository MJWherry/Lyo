using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Api.Middleware;
using Lyo.Authentication.Postgres;
using Lyo.Authentication.Postgres.Database;
using Lyo.Cache;
using Lyo.Common.Json;
using Lyo.Testing.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.Authentication.Tests;

/// <summary>Postgres + TestServer host mapping <see cref="Extensions.BuildAuthenticationApi" /> as anonymous so HTTP checks can run without a login.</summary>
public sealed class AuthenticationApiFixture : PostgresContainerFixtureBase
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
        builder.Services.AddPostgresAuthenticationStores(o => {
            o.ConnectionString = connectionString;
            o.EnableAutoMigrations = true;
        });
        builder.Services.AddLyoApiAuthentication();
        builder.Services.AddScoped<ILyoMapper>(sp => sp.GetRequiredService<AuthenticationLyoMapper>());
        var app = builder.Build();
        app.UseMiddleware<LoggingMiddleware>();
        app.BuildAuthenticationApi(AuthenticationApiOptions.WithAuth(EndpointAuth.Anonymous()));
        await app.StartAsync(cancellationToken);
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>();
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
