using Lyo.Authentication.Audit;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class AuthenticationPostgresFixture : PostgresServiceFixtureBase<UserDbContext>
{
    public IApiTokenStore TokenStore => ServiceProvider.GetRequiredService<IApiTokenStore>();

    public IUserStore UserStore => ServiceProvider.GetRequiredService<IUserStore>();

    public IExternalIdentityStore IdentityStore => ServiceProvider.GetRequiredService<IExternalIdentityStore>();

    public IAuthAuditRecorder AuthAuditRecorder => ServiceProvider.GetRequiredService<IAuthAuditRecorder>();

    public IUserClaimStore ClaimStore => ServiceProvider.GetRequiredService<IUserClaimStore>();

    public IUserScopeStore ScopeStore => ServiceProvider.GetRequiredService<IUserScopeStore>();

    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddPostgresAuthenticationStores(o => {
            o.ConnectionString = connectionString;
            o.EnableAutoMigrations = true;
        });
}
