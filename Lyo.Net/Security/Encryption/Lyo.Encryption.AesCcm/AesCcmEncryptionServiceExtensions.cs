using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.AesCcm;

/// <summary>Dependency-injection extensions for the AES-CCM addon. Mirrors the keyed-registration shape from <see cref="EncryptionServiceExtensions" /> but specialized to <see cref="AesCcmEncryptionService" />.</summary>
public static class AesCcmEncryptionServiceExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="AesCcmEncryptionService" /> as a singleton resolved from the provided <see cref="IKeyStore" />. Use <see cref="EncryptionServiceExtensions.AddDefaultEncryptionService{TConcrete}" /> for unkeyed <see cref="IEncryptionService" />.</summary>
        /// <param name="aesKeySize">Underlying AES key size for AES-CCM (128/192/256 bit).</param>
        public IServiceCollection AddAesCcmEncryption(AesGcmKeySizeBits aesKeySize = AesGcmKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<AesCcmEncryptionService>(provider => new(
                provider.GetRequiredService<IKeyStore>(), aesKeySize));
            return services;
        }

        /// <summary>Registers <see cref="AesCcmEncryptionService" /> as a keyed singleton (DEK + KEK = AES-CCM) plus the matching <see cref="ITwoKeyEncryptionService" /> envelope.</summary>
        /// <param name="keyName">Service key shared by the DEK service, KEK service, and two-key wrapper.</param>
        /// <param name="keyStoreName">Service key used to look up the backing <see cref="IKeyStore" />.</param>
        /// <param name="aesKeySize">Underlying AES key size for AES-CCM (128/192/256 bit).</param>
        public IServiceCollection AddAesCcmEncryptionServiceKeyed(string keyName, string keyStoreName, AesGcmKeySizeBits aesKeySize = AesGcmKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName);

            services.AddKeyedSingleton<AesCcmEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    return new AesCcmEncryptionService(keyStore, aesKeySize);
                });

            services.AddKeyedSingleton<IEncryptionService>(
                keyName,
                (provider, _) => provider.GetKeyedService<AesCcmEncryptionService>(keyName) ??
                    throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{nameof(AesCcmEncryptionService)}' was not found."));

            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    var dek = provider.GetKeyedService<AesCcmEncryptionService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{nameof(AesCcmEncryptionService)}' was not found.");

                    return new TwoKeyEncryptionService<AesCcmEncryptionService, AesCcmEncryptionService>(dek, dek, keyStore);
                });
        }
    }
}
