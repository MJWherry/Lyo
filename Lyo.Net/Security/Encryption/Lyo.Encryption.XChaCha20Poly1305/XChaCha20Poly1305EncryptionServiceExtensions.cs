using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.XChaCha20Poly1305;

/// <summary>Dependency-injection extensions for the XChaCha20-Poly1305 addon. Mirrors the keyed-registration shape from <see cref="EncryptionServiceExtensions" /> but specialized to <see cref="XChaCha20Poly1305EncryptionService" />.</summary>
public static class XChaCha20Poly1305EncryptionServiceExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="XChaCha20Poly1305EncryptionService" /> as a singleton resolved from the provided <see cref="IKeyStore" />. Use <see cref="EncryptionServiceExtensions.AddDefaultEncryptionService{TConcrete}" /> for unkeyed <see cref="IEncryptionService" />.</summary>
        public IServiceCollection AddXChaCha20Poly1305Encryption()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<XChaCha20Poly1305EncryptionService>(provider =>
                new(provider.GetRequiredService<IKeyStore>()));
            return services;
        }

        /// <summary>Registers <see cref="XChaCha20Poly1305EncryptionService" /> as a keyed singleton (DEK + KEK = XChaCha20-Poly1305) plus the matching <see cref="ITwoKeyEncryptionService" /> envelope.</summary>
        /// <param name="keyName">Service key shared by the DEK service, KEK service, and two-key wrapper.</param>
        /// <param name="keyStoreName">Service key used to look up the backing <see cref="IKeyStore" />.</param>
        public IServiceCollection AddXChaCha20Poly1305EncryptionServiceKeyed(string keyName, string keyStoreName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName);

            services.AddKeyedSingleton<XChaCha20Poly1305EncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    return new XChaCha20Poly1305EncryptionService(keyStore);
                });

            services.AddKeyedSingleton<IEncryptionService>(
                keyName,
                (provider, _) => provider.GetKeyedService<XChaCha20Poly1305EncryptionService>(keyName) ??
                    throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{nameof(XChaCha20Poly1305EncryptionService)}' was not found."));

            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    var dek = provider.GetKeyedService<XChaCha20Poly1305EncryptionService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{nameof(XChaCha20Poly1305EncryptionService)}' was not found.");

                    return new TwoKeyEncryptionService<XChaCha20Poly1305EncryptionService, XChaCha20Poly1305EncryptionService>(dek, dek, keyStore);
                });
        }
    }
}
