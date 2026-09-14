using Lyo.Authentication.Audit;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Postgres.Stores;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Lyo.Authentication.Postgres;

/// <summary>Container helpers for <c>Lyo.Authentication.Postgres</c>: <see cref="UserDbContext" /> registration plus the three Postgres-backed stores.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the Postgres auth registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="IDbContextFactory{UserDbContext}" /> against the supplied <see cref="PostgresUserOptions" />.</summary>
        public IServiceCollection AddUserDbContextFactory(Action<PostgresUserOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresUserOptions();
            configure(options);
            return services.AddUserDbContextFactory(options);
        }

        /// <summary>Registers <see cref="IDbContextFactory{UserDbContext}" /> using configuration binding.</summary>
        public IServiceCollection AddUserDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresUserOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresUserOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddUserDbContextFactory(options);
        }

        /// <summary>Registers <see cref="IDbContextFactory{UserDbContext}" /> and the <see cref="PostgresMigrationHostedService{UserDbContext, PostgresUserOptions}" />.</summary>
        public IServiceCollection AddUserDbContextFactory(PostgresUserOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<UserDbContext, PostgresUserOptions>(options);
            services.AddEntityRefOptions();

            return services;
        }

        /// <summary>Registers <see cref="IApiTokenStore" /> backed by <see cref="UserDbContext" />.</summary>
        public IServiceCollection AddPostgresApiTokenStore(Action<PostgresUserOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddUserDbContextFactory(configure);
            services.AddSingleton<IApiTokenStore, PostgresApiTokenStore>();
            return services;
        }

        /// <summary>Registers <see cref="IApiTokenStore" /> backed by <see cref="UserDbContext" />, binding options from configuration.</summary>
        public IServiceCollection AddPostgresApiTokenStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresUserOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddUserDbContextFactoryFromConfiguration(configuration, configSectionName);
            services.AddSingleton<IApiTokenStore, PostgresApiTokenStore>();
            return services;
        }

        /// <summary>Registers <see cref="IUserStore" /> backed by <see cref="UserDbContext" />.</summary>
        public IServiceCollection AddPostgresUserStore(Action<PostgresUserOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddUserDbContextFactory(configure);
            services.AddSingleton<IUserStore, PostgresUserStore>();
            return services;
        }

        /// <summary>Registers <see cref="IUserStore" /> backed by <see cref="UserDbContext" />, binding options from configuration.</summary>
        public IServiceCollection AddPostgresUserStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresUserOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddUserDbContextFactoryFromConfiguration(configuration, configSectionName);
            services.AddSingleton<IUserStore, PostgresUserStore>();
            return services;
        }

        /// <summary>Registers <see cref="IExternalIdentityStore" /> backed by <see cref="UserDbContext" />.</summary>
        public IServiceCollection AddPostgresExternalIdentityStore(Action<PostgresUserOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddUserDbContextFactory(configure);
            services.AddSingleton<IExternalIdentityStore, PostgresExternalIdentityStore>();
            return services;
        }

        /// <summary>Registers <see cref="IExternalIdentityStore" /> backed by <see cref="UserDbContext" />, binding options from configuration.</summary>
        public IServiceCollection AddPostgresExternalIdentityStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresUserOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddUserDbContextFactoryFromConfiguration(configuration, configSectionName);
            services.AddSingleton<IExternalIdentityStore, PostgresExternalIdentityStore>();
            return services;
        }

        /// <summary>Registers the Postgres-backed authentication stores (user, token, identity, claim, scope) plus the Postgres <see cref="IAuthAuditRecorder" />.</summary>
        public IServiceCollection AddPostgresAuthenticationStores(Action<PostgresUserOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddUserDbContextFactory(configure);
            services.AddSingleton<IApiTokenStore, PostgresApiTokenStore>();
            services.AddSingleton<IUserStore, PostgresUserStore>();
            services.AddSingleton<IExternalIdentityStore, PostgresExternalIdentityStore>();
            services.AddSingleton<IUserClaimStore, PostgresUserClaimStore>();
            services.AddSingleton<IUserScopeStore, PostgresUserScopeStore>();
            services.Replace(ServiceDescriptor.Singleton<IAuthAuditRecorder, PostgresAuthAuditRecorder>());
            return services;
        }

        /// <summary>Registers the Postgres-backed authentication stores (user, token, identity, claim, scope) plus the Postgres <see cref="IAuthAuditRecorder" />, binding options from configuration.</summary>
        public IServiceCollection AddPostgresAuthenticationStoresFromConfiguration(IConfiguration configuration, string configSectionName = PostgresUserOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddUserDbContextFactoryFromConfiguration(configuration, configSectionName);
            services.AddSingleton<IApiTokenStore, PostgresApiTokenStore>();
            services.AddSingleton<IUserStore, PostgresUserStore>();
            services.AddSingleton<IExternalIdentityStore, PostgresExternalIdentityStore>();
            services.AddSingleton<IUserClaimStore, PostgresUserClaimStore>();
            services.AddSingleton<IUserScopeStore, PostgresUserScopeStore>();
            services.Replace(ServiceDescriptor.Singleton<IAuthAuditRecorder, PostgresAuthAuditRecorder>());
            return services;
        }
    }
}