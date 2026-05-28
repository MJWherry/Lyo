using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.AesSiv;

/// <summary>
/// Dependency-injection extensions for the AES-SIV addon. Mirrors the keyed-registration shape from <see cref="EncryptionServiceExtensions" /> but specialized to
/// <see cref="AesSivEncryptionService" />.
/// </summary>
public static class AesSivEncryptionServiceExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="AesSivEncryptionService" /> as a singleton resolved from the provided <see cref="IKeyStore" />. Use
        /// <see cref="EncryptionServiceExtensions.AddDefaultEncryptionService{TConcrete}" /> for unkeyed <see cref="IEncryptionService" />.
        /// </summary>
        /// <param name="keySize">AES-SIV key size (256/384/512-bit).</param>
        public IServiceCollection AddAesSivEncryption(AesSivKeySizeBits keySize = AesSivKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<AesSivEncryptionService>(provider => new(provider.GetRequiredService<IKeyStore>(), keySize));
            return services;
        }

        /// <summary>Registers <see cref="AesSivEncryptionService" /> as a keyed singleton (DEK + KEK = AES-SIV) plus the matching <see cref="ITwoKeyEncryptionService" /> envelope.</summary>
        /// <param name="keyName">Service key shared by the DEK service, KEK service, and two-key wrapper.</param>
        /// <param name="keyStoreName">Service key used to look up the backing <see cref="IKeyStore" />.</param>
        /// <param name="keySize">AES-SIV key size (256/384/512-bit).</param>
        public IServiceCollection AddAesSivEncryptionServiceKeyed(string keyName, string keyStoreName, AesSivKeySizeBits keySize = AesSivKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName);
            services.AddKeyedSingleton<AesSivEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    return new(keyStore, keySize);
                });

            services.AddKeyedSingleton<IEncryptionService>(
                keyName,
                (provider, _) => provider.GetKeyedService<AesSivEncryptionService>(keyName) ??
                    throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{nameof(AesSivEncryptionService)}' was not found."));

            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    var dek = provider.GetKeyedService<AesSivEncryptionService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{nameof(AesSivEncryptionService)}' was not found.");

                    return new TwoKeyEncryptionService<AesSivEncryptionService, AesSivEncryptionService>(dek, dek, keyStore);
                });
        }
    }
}