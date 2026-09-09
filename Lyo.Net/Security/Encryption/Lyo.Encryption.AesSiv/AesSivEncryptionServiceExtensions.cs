using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.AesSiv;

/// <summary>
/// Container helpers for the AES-SIV addon. Same keyed-registration shape as <see cref="EncryptionServiceExtensions" />, specialized for
/// <see cref="AesSivEncryptionService" />.
/// </summary>
public static class AesSivEncryptionServiceExtensions
{
    /// <param name="services">Collection that receives the AES-SIV registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="AesSivEncryptionService" /> as a singleton resolved from the given <see cref="IKeyStore" />. Use
        /// <see cref="EncryptionServiceExtensions.AddDefaultEncryptionService{TConcrete}" /> for unkeyed <see cref="IEncryptionService" />.
        /// </summary>
        /// <param name="keySize">AES-SIV key size (256/384/512-bit).</param>
        public IServiceCollection AddAesSivEncryption(AesSivKeySizeBits keySize = AesSivKeySizeBits.Bits256)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<AesSivEncryptionService>(provider => new(provider.GetRequiredService<IKeyStore>(), keySize));
            return services;
        }

        /// <summary>Keyed singleton <see cref="AesSivEncryptionService" /> (DEK + KEK = AES-SIV) plus matching <see cref="ITwoKeyEncryptionService" /> envelope.</summary>
        /// <param name="keyName">Service key shared by the DEK service, KEK service, and two-key wrapper.</param>
        /// <param name="keyStoreName">Service key used to resolve the backing <see cref="IKeyStore" />.</param>
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