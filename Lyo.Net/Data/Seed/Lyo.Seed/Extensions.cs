using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Seed;

/// <summary>DI helpers that register <see cref="ISeedRunner"/> plus optional EF and API transports.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ISeedRunner"/>. Hosts still build or register transports and contributors themselves.</summary>
        public IServiceCollection AddLyoSeed()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<ISeedRunner, SeedRunner>();
            return services;
        }

        /// <summary>Adds <typeparamref name="TContributor"/> as a singleton <see cref="SeedContributor"/> (multi-implementation).</summary>
        public IServiceCollection AddSeedContributor<TContributor>()
            where TContributor : SeedContributor
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<SeedContributor, TContributor>());
            return services;
        }

        /// <summary>Adds a scoped <see cref="EfSeedTransport{TContext}"/> built from the <typeparamref name="TContext"/> already in DI.</summary>
        public IServiceCollection AddEfSeedTransport<TContext>()
            where TContext : DbContext
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddScoped(sp => new EfSeedTransport<TContext>(sp.GetRequiredService<TContext>()));
            return services;
        }

        /// <summary>Adds a singleton <see cref="SeedApiCatalog"/> and a scoped <see cref="ApiSeedTransport"/> that uses <see cref="IApiClient"/>.</summary>
        public IServiceCollection AddApiSeedTransport(Action<SeedApiCatalog> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var catalog = new SeedApiCatalog();
            configure(catalog);
            services.TryAddSingleton(catalog);
            services.TryAddScoped(sp => new ApiSeedTransport(sp.GetRequiredService<IApiClient>(), sp.GetRequiredService<SeedApiCatalog>()));
            return services;
        }
    }
}
