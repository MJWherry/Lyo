using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.KeyStore;

public static class Extensions
{
    /// <param name="services">Collection that receives the key-store registration.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a local key store to the container.</summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddLocalKeyStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<LocalKeyStore>(_ => new());
            services.AddSingleton<IKeyStore>(provider => provider.GetRequiredService<LocalKeyStore>());
            return services;
        }

        /// <summary>Adds a local key store and runs <paramref name="configure" /> on the instance.</summary>
        /// <param name="configure">Configures the key store instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddLocalKeyStore(Action<LocalKeyStore> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<LocalKeyStore>(_ => {
                var keyStore = new LocalKeyStore();
                configure(keyStore);
                return keyStore;
            });

            services.AddSingleton<IKeyStore>(provider => provider.GetRequiredService<LocalKeyStore>());
            return services;
        }

        /// <summary>Adds a local key store as a keyed service and runs <paramref name="configure" /> on the instance.</summary>
        /// <param name="key">Service key for the keyed registration.</param>
        /// <param name="configure">Configures the key store instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddKeyedLocalKeyStore(string key, Action<LocalKeyStore> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddKeyedSingleton<LocalKeyStore>(
                key, (_, _) => {
                    var keyStore = new LocalKeyStore();
                    configure(keyStore);
                    return keyStore;
                });

            // Register the interface pointing at the concrete implementation
            services.AddKeyedSingleton<IKeyStore>(
                key,
                (provider, _) => provider.GetKeyedService<LocalKeyStore>(key) ??
                    throw new InvalidOperationException($"Keyed keystore service '{key}' of type '{nameof(LocalKeyStore)}' was not found."));

            return services;
        }
    }
}