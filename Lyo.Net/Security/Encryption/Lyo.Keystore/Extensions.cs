using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Keystore;

public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary> Adds local key store to the service collection. </summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLocalKeyStore()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<LocalKeyStore>(_ => new());
            services.AddSingleton<IKeyStore>(provider => provider.GetRequiredService<LocalKeyStore>());
            return services;
        }

        /// <summary> Adds local key store to the service collection with configuration. </summary>
        /// <param name="configure">Action to configure the key store instance</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLocalKeyStore(Action<LocalKeyStore> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<LocalKeyStore>(_ => {
                var keyStore = new LocalKeyStore();
                configure(keyStore);
                return keyStore;
            });

            services.AddSingleton<IKeyStore>(provider => provider.GetRequiredService<LocalKeyStore>());
            return services;
        }

        /// <summary> Adds local key store as a keyed service to the service collection with configuration. </summary>
        /// <param name="key">The key for the keyed service</param>
        /// <param name="configure">Action to configure the key store instance</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddKeyedLocalKeyStore(string key, Action<LocalKeyStore> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddKeyedSingleton<LocalKeyStore>(
                key, (_, _) => {
                    var keyStore = new LocalKeyStore();
                    configure(keyStore);
                    return keyStore;
                });

            // Register interface pointing to concrete implementation
            services.AddKeyedSingleton<IKeyStore>(
                key,
                (provider, _) => provider.GetKeyedService<LocalKeyStore>(key) ??
                    throw new InvalidOperationException($"Keyed keystore service '{key}' of type '{nameof(LocalKeyStore)}' was not found."));

            return services;
        }
    }
}