using Lyo.Authentication.Audit;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class AuthenticationPostgresFixture : PostgresContainerFixtureBase
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public IDbContextFactory<UserDbContext> ContextFactory => ServiceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>();

    public IApiTokenStore TokenStore => ServiceProvider.GetRequiredService<IApiTokenStore>();

    public IUserStore UserStore => ServiceProvider.GetRequiredService<IUserStore>();

    public IExternalIdentityStore IdentityStore => ServiceProvider.GetRequiredService<IExternalIdentityStore>();

    public IAuthAuditRecorder AuthAuditRecorder => ServiceProvider.GetRequiredService<IAuthAuditRecorder>();

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddPostgresAuthenticationStores(o => {
            o.ConnectionString = connectionString;
            o.EnableAutoMigrations = true;
        });

        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}