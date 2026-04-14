using System.Security.Cryptography;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Rsa;
using Lyo.Encryption.Symmetric.Aes.AesCcm;
using Lyo.Encryption.Symmetric.Aes.AesSiv;
using Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.Extensions;

/// <summary> Extension methods for registering encryption services in dependency injection containers. </summary>
public static class EncryptionServiceExtensions
{
    /// <summary>Determines the encryption algorithm used by the given encryption service.</summary>
    /// <param name="encryptionService">The encryption service to analyze</param>
    /// <returns>The encryption algorithm, or null if the service type is not recognized</returns>
    public static EncryptionAlgorithm? DetermineAlgorithm(IEncryptionService? encryptionService)
        => EncryptionAlgorithmDiscovery.FromEncryptionService(encryptionService);

    /// <summary>Determines the Data Encryption Key (DEK) algorithm from a two-key encryption service.</summary>
    /// <param name="twoKeyService">The two-key encryption service to analyze</param>
    /// <returns>The DEK encryption algorithm, or null if not recognized</returns>
    public static EncryptionAlgorithm? DetermineDekAlgorithm(ITwoKeyEncryptionService? twoKeyService) => twoKeyService?.DekAlgorithm;

    /// <summary>Determines the Key Encryption Key (KEK) algorithm from a two-key encryption service.</summary>
    /// <param name="twoKeyService">The two-key encryption service to analyze</param>
    /// <returns>The KEK encryption algorithm, or null if not recognized</returns>
    public static EncryptionAlgorithm? DetermineKekAlgorithm(ITwoKeyEncryptionService? twoKeyService) => twoKeyService?.KekAlgorithm;

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a keyed two-key encryption service to the service collection using an existing keyed key store.</summary>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="keyStoreName">The key name for the keyed key store service</param>
        /// <param name="aesGcmKeySize">AES-GCM key size for <see cref="AesGcmEncryptionService"/> when used as DEK/KEK.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed(string keyName, string keyStoreName, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            => services.AddEncryptionServiceKeyed<AesGcmEncryptionService, AesGcmEncryptionService>(keyName, keyStoreName, aesGcmKeySize);

        /// <summary>
        /// Adds a keyed two-key encryption service to the service collection using an existing keyed key store. Uses the same encryption service type for both DEK and KEK
        /// operations.
        /// </summary>
        /// <typeparam name="TEncryptionService">The encryption service type for both DEK and KEK operations</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="keyStoreName">The key name for the keyed key store service</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when <typeparamref name="TEncryptionService"/> is <see cref="AesGcmEncryptionService"/>.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TEncryptionService>(string keyName, string keyStoreName, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TEncryptionService : class, IEncryptionService
            => services.AddEncryptionServiceKeyed<TEncryptionService, TEncryptionService>(keyName, keyStoreName, aesGcmKeySize);

        /// <summary>Adds a keyed two-key encryption service to the service collection using an existing keyed key store. Uses separate encryption service types for DEK and KEK operations.</summary>
        /// <typeparam name="TDekService">The Data Encryption Key (DEK) encryption service type</typeparam>
        /// <typeparam name="TKekService">The Key Encryption Key (KEK) encryption service type</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="keyStoreName">The key name for the keyed key store service</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when DEK or KEK service is <see cref="AesGcmEncryptionService"/>.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TDekService, TKekService>(string keyName, string keyStoreName, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TDekService : class, IEncryptionService where TKekService : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName, nameof(keyStoreName));

            // Register DEK service as keyed if not already registered
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TDekService))) {
                services.AddKeyedSingleton<TDekService>(
                    keyName, (provider, serviceKey) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                        if (typeof(TDekService) == typeof(AesGcmEncryptionService))
                            return (TDekService)(object)new AesGcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TDekService) == typeof(ChaCha20Poly1305EncryptionService))
                            return (TDekService)(object)new ChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TDekService) == typeof(AesCcmEncryptionService))
                            return (TDekService)(object)new AesCcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TDekService) == typeof(XChaCha20Poly1305EncryptionService))
                            return (TDekService)(object)new XChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TDekService) == typeof(AesSivEncryptionService))
                            return (TDekService)(object)new AesSivEncryptionService(keyStore);

                        throw new InvalidOperationException(
                            $"Generic AddEncryptionServiceKeyed does not support {typeof(TDekService).Name}. Register it manually.");
                    });

                // Register interface for DEK service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyName,
                    (provider, serviceKey) => provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found."));
            }

            // Register KEK service as keyed if not already registered
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKekService))) {
                services.AddKeyedSingleton<TKekService>(
                    keyName, (provider, serviceKey) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                        if (typeof(TKekService) == typeof(AesGcmEncryptionService))
                            return (TKekService)(object)new AesGcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TKekService) == typeof(ChaCha20Poly1305EncryptionService))
                            return (TKekService)(object)new ChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TKekService) == typeof(AesCcmEncryptionService))
                            return (TKekService)(object)new AesCcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TKekService) == typeof(XChaCha20Poly1305EncryptionService))
                            return (TKekService)(object)new XChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TKekService) == typeof(AesSivEncryptionService))
                            return (TKekService)(object)new AesSivEncryptionService(keyStore);

                        throw new InvalidOperationException(
                            $"Generic AddEncryptionServiceKeyed does not support {typeof(TKekService).Name}. Register it manually.");
                    });
            }

            // Register TwoKeyEncryptionService as keyed
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, serviceKey) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                    var dekService = provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found.");

                    var kekService = provider.GetKeyedService<TKekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TKekService).Name}' was not found.");

                    return new TwoKeyEncryptionService<TKekService, TDekService>(dekService, kekService, keyStore);
                });
        }

        /// <summary>Adds a keyed two-key encryption service to the service collection with key store configuration.</summary>
        /// <typeparam name="TKeyStore">The key store type</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="configKeyStore">Function to configure the key store (will be registered with keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key size for <see cref="AesGcmEncryptionService"/> when used as DEK/KEK.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore>(string keyName, Func<IServiceProvider, TKeyStore> configKeyStore, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore
            => services.AddEncryptionServiceKeyed<TKeyStore, AesGcmEncryptionService, AesGcmEncryptionService>(keyName, configKeyStore, aesGcmKeySize);

        /// <summary>Adds a keyed two-key encryption service to the service collection with key store configuration. Uses the same encryption service type for both DEK and KEK operations.</summary>
        /// <typeparam name="TKeyStore">The key store type</typeparam>
        /// <typeparam name="TEncryptionService">The encryption service type for both DEK and KEK operations</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="configKeyStore">Function to configure the key store (will be registered with keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when <typeparamref name="TEncryptionService"/> is <see cref="AesGcmEncryptionService"/>.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore, TEncryptionService>(string keyName, Func<IServiceProvider, TKeyStore> configKeyStore, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore where TEncryptionService : class, IEncryptionService
            => services.AddEncryptionServiceKeyed<TKeyStore, TEncryptionService, TEncryptionService>(keyName, configKeyStore, aesGcmKeySize);

        /// <summary>Adds a keyed two-key encryption service to the service collection with key store configuration. Uses separate encryption service types for DEK and KEK operations.</summary>
        /// <typeparam name="TKeyStore">The key store type</typeparam>
        /// <typeparam name="TDekService">The Data Encryption Key (DEK) encryption service type</typeparam>
        /// <typeparam name="TKekService">The Key Encryption Key (KEK) encryption service type</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="configKeyStore">Function to configure the key store (will be registered with keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when DEK or KEK service is <see cref="AesGcmEncryptionService"/>.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore, TDekService, TKekService>(string keyName, Func<IServiceProvider, TKeyStore> configKeyStore, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore where TDekService : class, IEncryptionService where TKekService : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
            ArgumentHelpers.ThrowIfNull(configKeyStore, nameof(configKeyStore));

            // Register key store with keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKeyStore))) {
                services.AddKeyedSingleton<TKeyStore>(keyName, (provider, serviceKey) => configKeyStore(provider));
                services.AddKeyedSingleton<IKeyStore>(
                    keyName,
                    (provider, serviceKey) => provider.GetKeyedService<TKeyStore>(keyName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyName}' of type '{typeof(TKeyStore).Name}' was not found."));
            }

            // Register DEK service as keyed
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TDekService))) {
                services.AddKeyedSingleton<TDekService>(
                    keyName, (provider, serviceKey) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyName}' was not found.");
                        if (typeof(TDekService) == typeof(AesGcmEncryptionService))
                            return (TDekService)(object)new AesGcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TDekService) == typeof(ChaCha20Poly1305EncryptionService))
                            return (TDekService)(object)new ChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TDekService) == typeof(AesCcmEncryptionService))
                            return (TDekService)(object)new AesCcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TDekService) == typeof(XChaCha20Poly1305EncryptionService))
                            return (TDekService)(object)new XChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TDekService) == typeof(AesSivEncryptionService))
                            return (TDekService)(object)new AesSivEncryptionService(keyStore);

                        throw new InvalidOperationException(
                            $"Generic AddEncryptionServiceKeyed does not support {typeof(TDekService).Name}. Register it manually.");
                    });

                // Register interface for DEK service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyName,
                    (provider, serviceKey) => provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found."));
            }

            // Register KEK service as keyed
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKekService))) {
                services.AddKeyedSingleton<TKekService>(
                    keyName, (provider, serviceKey) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStore}' was not found.");
                        if (typeof(TKekService) == typeof(AesGcmEncryptionService))
                            return (TKekService)(object)new AesGcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TKekService) == typeof(ChaCha20Poly1305EncryptionService))
                            return (TKekService)(object)new ChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TKekService) == typeof(AesCcmEncryptionService))
                            return (TKekService)(object)new AesCcmEncryptionService(keyStore, aesGcmKeySize);

                        if (typeof(TKekService) == typeof(XChaCha20Poly1305EncryptionService))
                            return (TKekService)(object)new XChaCha20Poly1305EncryptionService(keyStore);

                        if (typeof(TKekService) == typeof(AesSivEncryptionService))
                            return (TKekService)(object)new AesSivEncryptionService(keyStore);

                        throw new InvalidOperationException(
                            $"Generic AddEncryptionServiceKeyed does not support {typeof(TKekService).Name}. Register it manually.");
                    });
            }

            // Register TwoKeyEncryptionService as keyed
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, serviceKey) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStore}' was not found.");
                    var dekService = provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found.");

                    var kekService = provider.GetKeyedService<TKekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TKekService).Name}' was not found.");

                    return new TwoKeyEncryptionService<TKekService, TDekService>(dekService, kekService, keyStore);
                });
        }

        /// <summary>Adds RSA encryption service to the service collection.</summary>
        /// <param name="publicPemPath">Path to the RSA public key PEM file</param>
        /// <param name="privatePemPath">Path to the RSA private key PEM file</param>
        /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
        /// <param name="password">Password for the PFX certificate</param>
        /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
        /// <param name="maxChunkSize">Maximum chunk size for encryption. If null, automatically calculated.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddRsaEncryption(
            string? publicPemPath = null,
            string? privatePemPath = null,
            string? pfxPath = null,
            string? password = null,
            RSAEncryptionPadding? padding = null,
            int? maxChunkSize = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            return services.AddScoped(_ => new RsaEncryptionService(publicPemPath, privatePemPath, pfxPath, password, padding, maxChunkSize));
        }

        /// <summary>Adds AES-GCM + RSA hybrid encryption service to the service collection.</summary>
        /// <param name="publicPemPath">Path to the RSA public key PEM file</param>
        /// <param name="privatePemPath">Path to the RSA private key PEM file</param>
        /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
        /// <param name="password">Password for the PFX certificate</param>
        /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAesGcmRsaEncryption(
            string? publicPemPath = null,
            string? privatePemPath = null,
            string? pfxPath = null,
            string? password = null,
            RSAEncryptionPadding? padding = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            return services.AddScoped(_ => new AesGcmRsaEncryptionService(publicPemPath, privatePemPath, pfxPath, password, padding));
        }
    }
}