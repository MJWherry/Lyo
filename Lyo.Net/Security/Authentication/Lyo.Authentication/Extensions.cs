using Lyo.Authentication.Audit;
using Lyo.Authentication.Options;
using Lyo.Authentication.Scopes;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Refresh;
using Lyo.Authentication.Services.Users;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Authentication;

/// <summary>
/// Top-level DI surface for <c>Lyo.Authentication</c>. Compose with <see cref="ScopeRegistrationExtensions.AddScope" />, <c>AddInMemoryAuthenticationStores</c>, and (in
/// ASP.NET) <c>AddLyoApiTokenAuthentication</c>.
/// </summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the base authentication services: <see cref="AuthenticationOptions" />, <see cref="LyoJwtOptions" />, <see cref="ScopeRegistry" /> (as both
        /// <see cref="IScopeRegistry" /> and concrete), opaque token issuer + validator, JWT issuer + validator, refresh token issuer + exchange, and the JWKS builder. Does NOT register
        /// stores — call <see cref="AddInMemoryAuthenticationStores" /> or use <c>Lyo.Authentication.Postgres</c>.
        /// </summary>
        public IServiceCollection AddLyoAuthentication()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddOptions<AuthenticationOptions>();
            services.AddOptions<LyoJwtOptions>();
            services.AddSingleton(new ScopeRegistry());
            services.AddSingleton<IScopeRegistry>(sp => sp.GetRequiredService<ScopeRegistry>());
            services.AddSingleton<IApiTokenIssuer, DefaultApiTokenIssuer>();
            services.AddSingleton<IApiTokenValidator, DefaultApiTokenValidator>();
            services.AddSingleton<ILyoRefreshTokenIssuer, DefaultLyoRefreshTokenIssuer>();
            services.AddSingleton<ILyoRefreshTokenExchange, DefaultLyoRefreshTokenExchange>();
            services.AddSingleton<ILyoJwtIssuer, Ed25519LyoJwtIssuer>();
            services.AddSingleton<ILyoJwtValidator, Ed25519LyoJwtValidator>();
            services.AddSingleton<JwkSetBuilder>();
            services.AddHostedService<Ed25519KeyBootstrapper>();
            services.TryAddSingleton<IAuthAuditRecorder>(NullAuthAuditRecorder.Instance);
            services.TryAddSingleton<IAuthAuditContextAccessor>(NullAuthAuditContextAccessor.Instance);
            return services;
        }

        /// <summary>Same as <see cref="AddLyoAuthentication()" /> but also binds options from configuration.</summary>
        public IServiceCollection AddLyoAuthentication(
            IConfiguration configuration,
            string authSection = AuthenticationOptions.SectionName,
            string jwtSection = LyoJwtOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddLyoAuthentication();
            services.Configure<AuthenticationOptions>(o => configuration.GetSection(authSection).Bind(o));
            services.Configure<LyoJwtOptions>(o => configuration.GetSection(jwtSection).Bind(o));
            return services;
        }

        /// <summary>Same as <see cref="AddLyoAuthentication()" /> but also binds <see cref="AuthenticationOptions" /> from a single <see cref="IConfigurationSection" /> (advanced).</summary>
        public IServiceCollection AddLyoAuthentication(IConfigurationSection authSection)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(authSection);
            services.AddLyoAuthentication();
            services.Configure<AuthenticationOptions>(o => authSection.Bind(o));
            return services;
        }

        /// <summary>
        /// Registers in-memory <see cref="IApiTokenStore" />, <see cref="IUserStore" />, and <see cref="IExternalIdentityStore" />. Suitable for development and tests; swap for
        /// <c>Lyo.Authentication.Postgres</c> in production.
        /// </summary>
        public IServiceCollection AddInMemoryAuthenticationStores()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IApiTokenStore, InMemoryApiTokenStore>();
            services.AddSingleton<IUserStore, InMemoryUserStore>();
            services.AddSingleton<IExternalIdentityStore, InMemoryExternalIdentityStore>();
            return services;
        }
    }
}