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
using Microsoft.Extensions.Options;

namespace Lyo.Authentication;

/// <summary>
/// Top-level container helpers for <c>Lyo.Authentication</c>. Compose with <see cref="ScopeRegistrationExtensions.AddScope" />, <c>AddInMemoryAuthenticationStores</c>, and (in
/// ASP.NET) <c>AddLyoApiTokenAuthentication</c>.
/// </summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the authentication registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the base authentication services: <see cref="AuthenticationOptions" />, <see cref="LyoJwtOptions" />, <see cref="ScopeRegistry" /> (as both
        /// <see cref="IScopeRegistry" /> and concrete), opaque issuer + validator, JWT issuer + validator, refresh issuer + exchange, and the JWKS builder. Does not register
        /// stores — call <see cref="AddInMemoryAuthenticationStores" /> or use <c>Lyo.Authentication.Postgres</c>.
        /// </summary>
        public IServiceCollection AddLyoAuthentication()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddOptions<AuthenticationOptions>();
            services.AddOptions<LyoJwtOptions>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<AuthenticationOptions>>().Value);
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<LyoJwtOptions>>().Value);
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

        /// <summary>Like <see cref="AddLyoAuthentication()" /> but binds <see cref="AuthenticationOptions" /> from one <see cref="IConfigurationSection" /> (advanced).</summary>
        public IServiceCollection AddLyoAuthentication(IConfigurationSection authSection)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(authSection);
            services.AddLyoAuthentication();
            services.Configure<AuthenticationOptions>(o => authSection.Bind(o));
            return services;
        }

        /// <summary>
        /// Registers in-memory <see cref="IApiTokenStore" />, <see cref="IUserStore" />, and <see cref="IExternalIdentityStore" />. Fine for development and tests; swap for
        /// <c>Lyo.Authentication.Postgres</c> in production.
        /// </summary>
        public IServiceCollection AddInMemoryAuthenticationStores()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IApiTokenStore, InMemoryApiTokenStore>();
            services.AddSingleton<IUserStore, InMemoryUserStore>();
            services.AddSingleton<IUserClaimStore, InMemoryUserClaimStore>();
            services.AddSingleton<IUserScopeStore, InMemoryUserScopeStore>();
            services.AddSingleton<IExternalIdentityStore, InMemoryExternalIdentityStore>();
            return services;
        }
    }
}