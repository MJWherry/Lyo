using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.AesCcm;

/// <summary>
/// Container helpers for the AES-CCM addon. Same keyed-registration shape as <see cref="EncryptionServiceExtensions" />, specialized for
/// <see cref="AesCcmEncryptionService" />.
/// </summary>
public static class AesCcmEncryptionServiceExtensions
{
    /// <param name="services">Collection that receives the AES-CCM registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="AesCcmEncryptionService" /> as a singleton resolved from the given <see cref="IKeyStore" />. Use
        /// <see cref="EncryptionServiceExtensions.AddDefaultEncryptionService{TConcrete}" /> for unkeyed <see cref="IEncryptionService" />.
        /// </summary>
        /// <param name="aesKeySize">AES key size under AES-CCM (128/192/256 bit).</param>
        public IServiceCollection AddAesCcmEncryption(AesGcmKeySizeBits aesKeySize = AesGcmKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<AesCcmEncryptionService>(provider => new(provider.GetRequiredService<IKeyStore>(), aesKeySize));
            return services;
        }

        /// <summary>Keyed singleton <see cref="AesCcmEncryptionService" /> (DEK + KEK = AES-CCM) plus matching <see cref="ITwoKeyEncryptionService" /> envelope.</summary>
        /// <param name="keyName">Service key shared by the DEK service, KEK service, and two-key wrapper.</param>
        /// <param name="keyStoreName">Service key used to resolve the backing <see cref="IKeyStore" />.</param>
        /// <param name="aesKeySize">AES key size under AES-CCM (128/192/256 bit).</param>
        public IServiceCollection AddAesCcmEncryptionServiceKeyed(string keyName, string keyStoreName, AesGcmKeySizeBits aesKeySize = AesGcmKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName);
            services.AddKeyedSingleton<AesCcmEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    return new(keyStore, aesKeySize);
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