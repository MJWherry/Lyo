using System.Security.Cryptography;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.Rsa;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.Extensions;

/// <summary> Extension methods for registering encryption services in dependency injection containers. </summary>
public static class EncryptionServiceExtensions
{
    /// <summary>Determines the encryption algorithm used by the given encryption service.</summary>
    /// <param name="encryptionService">The encryption service to analyze</param>
    /// <returns>The encryption algorithm, or null if the service type is not recognized</returns>
    public static EncryptionAlgorithm? DetermineAlgorithm(IEncryptionService? encryptionService) => EncryptionAlgorithmDiscovery.FromEncryptionService(encryptionService);

    /// <summary>Determines the Data Encryption Key (DEK) algorithm from a two-key encryption service.</summary>
    /// <param name="twoKeyService">The two-key encryption service to analyze</param>
    /// <returns>The DEK encryption algorithm, or null if not recognized</returns>
    public static EncryptionAlgorithm? DetermineDekAlgorithm(ITwoKeyEncryptionService? twoKeyService) => twoKeyService?.DekAlgorithm;

    /// <summary>Determines the Key Encryption Key (KEK) algorithm from a two-key encryption service.</summary>
    /// <param name="twoKeyService">The two-key encryption service to analyze</param>
    /// <returns>The KEK encryption algorithm, or null if not recognized</returns>
    public static EncryptionAlgorithm? DetermineKekAlgorithm(ITwoKeyEncryptionService? twoKeyService) => twoKeyService?.KekAlgorithm;

    /// <summary>
    /// Instantiates one of the encryption service types shipped with the base <c>Lyo.Encryption</c> package (AES-GCM or ChaCha20-Poly1305). Throws a guidance exception for niche
    /// addon types so callers know which addon helper to call.
    /// </summary>
    private static TService CreateBuiltInService<TService>(IKeyStore keyStore, AesGcmKeySizeBits aesGcmKeySize)
        where TService : class, IEncryptionService
    {
        if (typeof(TService) == typeof(AesGcmEncryptionService))
            return (TService)(object)new AesGcmEncryptionService(keyStore, aesGcmKeySize);

        if (typeof(TService) == typeof(ChaCha20Poly1305EncryptionService))
            return (TService)(object)new ChaCha20Poly1305EncryptionService(keyStore);

        throw new InvalidOperationException(
            $"Generic AddEncryptionServiceKeyed does not support '{typeof(TService).Name}'. " +
            $"Install the matching Lyo.Encryption addon package (e.g. Lyo.Encryption.AesCcm / Lyo.Encryption.AesSiv / Lyo.Encryption.XChaCha20Poly1305) " +
            "and call its dedicated AddXxxEncryptionServiceKeyed extension, or register the service manually via services.AddKeyedSingleton.");
    }

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a keyed two-key encryption service to the service collection using an existing keyed key store.</summary>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="keyStoreName">The key name for the keyed key store service</param>
        /// <param name="aesGcmKeySize">AES-GCM key size for <see cref="AesGcmEncryptionService" /> when used as DEK/KEK.</param>
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
        /// <param name="aesGcmKeySize">AES-GCM key size when <typeparamref name="TEncryptionService" /> is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TEncryptionService>(string keyName, string keyStoreName, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TEncryptionService : class, IEncryptionService
            => services.AddEncryptionServiceKeyed<TEncryptionService, TEncryptionService>(keyName, keyStoreName, aesGcmKeySize);

        /// <summary>Adds a keyed two-key encryption service to the service collection using an existing keyed key store. Uses separate encryption service types for DEK and KEK operations.</summary>
        /// <typeparam name="TDekService">The Data Encryption Key (DEK) encryption service type</typeparam>
        /// <typeparam name="TKekService">The Key Encryption Key (KEK) encryption service type</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="keyStoreName">The key name for the keyed key store service</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when DEK or KEK service is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TDekService, TKekService>(
            string keyName,
            string keyStoreName,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TDekService : class, IEncryptionService where TKekService : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName);

            // Register DEK service as keyed if not already registered
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TDekService))) {
                services.AddKeyedSingleton<TDekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                        return CreateBuiltInService<TDekService>(keyStore, aesGcmKeySize);
                    });

                // Register interface for DEK service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyName,
                    (provider, _) => provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found."));
            }

            // Register KEK service as keyed if not already registered
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKekService))) {
                services.AddKeyedSingleton<TKekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                        return CreateBuiltInService<TKekService>(keyStore, aesGcmKeySize);
                    });
            }

            // Register TwoKeyEncryptionService as keyed
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, _) => {
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
        /// <param name="aesGcmKeySize">AES-GCM key size for <see cref="AesGcmEncryptionService" /> when used as DEK/KEK.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore>(
            string keyName,
            Func<IServiceProvider, TKeyStore> configKeyStore,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore
            => services.AddEncryptionServiceKeyed<TKeyStore, AesGcmEncryptionService, AesGcmEncryptionService>(keyName, configKeyStore, aesGcmKeySize);

        /// <summary>Adds a keyed two-key encryption service to the service collection with key store configuration. Uses the same encryption service type for both DEK and KEK operations.</summary>
        /// <typeparam name="TKeyStore">The key store type</typeparam>
        /// <typeparam name="TEncryptionService">The encryption service type for both DEK and KEK operations</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="configKeyStore">Function to configure the key store (will be registered with keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when <typeparamref name="TEncryptionService" /> is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore, TEncryptionService>(
            string keyName,
            Func<IServiceProvider, TKeyStore> configKeyStore,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore where TEncryptionService : class, IEncryptionService
            => services.AddEncryptionServiceKeyed<TKeyStore, TEncryptionService, TEncryptionService>(keyName, configKeyStore, aesGcmKeySize);

        /// <summary>Adds a keyed two-key encryption service to the service collection with key store configuration. Uses separate encryption service types for DEK and KEK operations.</summary>
        /// <typeparam name="TKeyStore">The key store type</typeparam>
        /// <typeparam name="TDekService">The Data Encryption Key (DEK) encryption service type</typeparam>
        /// <typeparam name="TKekService">The Key Encryption Key (KEK) encryption service type</typeparam>
        /// <param name="keyName">The key name for the keyed encryption service</param>
        /// <param name="configKeyStore">Function to configure the key store (will be registered with keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key size when DEK or KEK service is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore, TDekService, TKekService>(
            string keyName,
            Func<IServiceProvider, TKeyStore> configKeyStore,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore where TDekService : class, IEncryptionService where TKekService : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNull(configKeyStore);

            // Register key store with keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKeyStore))) {
                services.AddKeyedSingleton<TKeyStore>(keyName, (provider, _) => configKeyStore(provider));
                services.AddKeyedSingleton<IKeyStore>(
                    keyName,
                    (provider, _) => provider.GetKeyedService<TKeyStore>(keyName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyName}' of type '{typeof(TKeyStore).Name}' was not found."));
            }

            // Register DEK service as keyed
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TDekService))) {
                services.AddKeyedSingleton<TDekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyName}' was not found.");
                        return CreateBuiltInService<TDekService>(keyStore, aesGcmKeySize);
                    });

                // Register interface for DEK service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyName,
                    (provider, _) => provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found."));
            }

            // Register KEK service as keyed
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKekService))) {
                services.AddKeyedSingleton<TKekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyName}' was not found.");
                        return CreateBuiltInService<TKekService>(keyStore, aesGcmKeySize);
                    });
            }

            // Register TwoKeyEncryptionService as keyed
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                    OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStore}' was not found.");
                    var dekService = provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found.");

                    var kekService = provider.GetKeyedService<TKekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TKekService).Name}' was not found.");

                    return new TwoKeyEncryptionService<TKekService, TDekService>(dekService, kekService, keyStore);
                });
        }

        /// <summary>Adds an RSA encryptor (public key) to the service collection.</summary>
        /// <param name="publicPemPath">Path to the RSA public key PEM file</param>
        /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
        /// <param name="password">Password for the PFX certificate</param>
        /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
        /// <param name="maxChunkSize">Maximum chunk size for encryption. If null, automatically calculated.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddRsaEncryptor(
            string? publicPemPath = null,
            string? pfxPath = null,
            string? password = null,
            RSAEncryptionPadding? padding = null,
            int? maxChunkSize = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddScoped(_ => new RsaEncryptor(publicPemPath, pfxPath, password, padding, maxChunkSize));
        }

        /// <summary>Adds an RSA decryptor (private key) to the service collection.</summary>
        /// <param name="privatePemPath">Path to the RSA private key PEM file</param>
        /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
        /// <param name="password">Password for the PFX certificate</param>
        /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddRsaDecryptor(string? privatePemPath = null, string? pfxPath = null, string? password = null, RSAEncryptionPadding? padding = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddScoped(_ => new RsaDecryptor(privatePemPath, pfxPath, password, padding));
        }

        /// <summary>Adds both an RSA encryptor (public key) and decryptor (private key) to the service collection.</summary>
        /// <param name="publicPemPath">Path to the RSA public key PEM file</param>
        /// <param name="privatePemPath">Path to the RSA private key PEM file</param>
        /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM, used for both encryptor and decryptor)</param>
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
            ArgumentHelpers.ThrowIfNull(services);
            services.AddRsaEncryptor(publicPemPath, pfxPath, password, padding, maxChunkSize);
            services.AddRsaDecryptor(privatePemPath, pfxPath, password, padding);
            return services;
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
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddScoped(_ => new AesGcmRsaEncryptionService(publicPemPath, privatePemPath, pfxPath, password, padding));
        }

        /// <summary>Maps unkeyed <see cref="IEncryptionService" /> to an already-registered concrete singleton.</summary>
        /// <typeparam name="TConcrete">Concrete encryption service type registered earlier in the same service collection.</typeparam>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddDefaultEncryptionService<TConcrete>()
            where TConcrete : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IEncryptionService>(sp => sp.GetRequiredService<TConcrete>());
            return services;
        }

        /// <summary>Maps unkeyed <see cref="ITwoKeyEncryptionService" /> to an already-registered concrete singleton. Prefer keyed registration for multiple envelopes.</summary>
        /// <typeparam name="TConcrete">Concrete two-key encryption service type registered earlier in the same service collection.</typeparam>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddDefaultTwoKeyEncryptionService<TConcrete>()
            where TConcrete : class, ITwoKeyEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<ITwoKeyEncryptionService>(sp => sp.GetRequiredService<TConcrete>());
            return services;
        }
    }
}