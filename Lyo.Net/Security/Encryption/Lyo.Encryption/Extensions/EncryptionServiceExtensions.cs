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

/// <summary>DI helpers that register encryption services.</summary>
public static class EncryptionServiceExtensions
{
    /// <summary>Detects the algorithm implemented by the given encryption service.</summary>
    /// <param name="encryptionService">Service to inspect</param>
    /// <returns>Algorithm, or null when the service type is unknown</returns>
    public static EncryptionAlgorithm? DetermineAlgorithm(IEncryptionService? encryptionService) => EncryptionAlgorithmDiscovery.FromEncryptionService(encryptionService);

    /// <summary>Detects the DEK algorithm on a two-key encryption service.</summary>
    /// <param name="twoKeyService">Two-key service to inspect</param>
    /// <returns>DEK algorithm, or null when unrecognized</returns>
    public static EncryptionAlgorithm? DetermineDekAlgorithm(ITwoKeyEncryptionService? twoKeyService) => twoKeyService?.DekAlgorithm;

    /// <summary>Detects the KEK algorithm on a two-key encryption service.</summary>
    /// <param name="twoKeyService">Two-key service to inspect</param>
    /// <returns>KEK algorithm, or null when unrecognized</returns>
    public static EncryptionAlgorithm? DetermineKekAlgorithm(ITwoKeyEncryptionService? twoKeyService) => twoKeyService?.KekAlgorithm;

    /// <summary>
    /// Creates one of the encryption services shipped in the base <c>Lyo.Encryption</c> package (AES-GCM or ChaCha20-Poly1305). Throws a guidance exception for niche
    /// addon types so the caller knows which addon helper to use.
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

    /// <param name="services">DI service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a keyed two-key service that uses an existing keyed key store.</summary>
        /// <param name="keyName">Keyed-registration name for the encryption service</param>
        /// <param name="keyStoreName">Keyed-registration name for the key store</param>
        /// <param name="aesGcmKeySize">AES-GCM key width for <see cref="AesGcmEncryptionService" /> as DEK/KEK.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed(string keyName, string keyStoreName, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            => services.AddEncryptionServiceKeyed<AesGcmEncryptionService, AesGcmEncryptionService>(keyName, keyStoreName, aesGcmKeySize);

        /// <summary>
        /// Registers a keyed two-key service that uses an existing keyed key store. The same encryption service type handles both DEK and KEK
        /// work.
        /// </summary>
        /// <typeparam name="TEncryptionService">Service type used for both DEK and KEK</typeparam>
        /// <param name="keyName">Keyed-registration name for the encryption service</param>
        /// <param name="keyStoreName">Keyed-registration name for the key store</param>
        /// <param name="aesGcmKeySize">AES-GCM key width when <typeparamref name="TEncryptionService" /> is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TEncryptionService>(string keyName, string keyStoreName, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TEncryptionService : class, IEncryptionService
            => services.AddEncryptionServiceKeyed<TEncryptionService, TEncryptionService>(keyName, keyStoreName, aesGcmKeySize);

        /// <summary>Registers a keyed two-key service over an existing keyed store, with separate DEK and KEK service types.</summary>
        /// <typeparam name="TDekService">Data Encryption Key (DEK) service type</typeparam>
        /// <typeparam name="TKekService">Key Encryption Key (KEK) service type</typeparam>
        /// <param name="keyName">Keyed-registration name for the encryption service</param>
        /// <param name="keyStoreName">Keyed-registration name for the key store</param>
        /// <param name="aesGcmKeySize">AES-GCM key width when DEK or KEK is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TDekService, TKekService>(
            string keyName,
            string keyStoreName,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TDekService : class, IEncryptionService where TKekService : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyStoreName);

            // register a keyed DEK service when missing
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TDekService))) {
                services.AddKeyedSingleton<TDekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                        return CreateBuiltInService<TDekService>(keyStore, aesGcmKeySize);
                    });

                // register IEncryptionService for the DEK
                services.AddKeyedSingleton<IEncryptionService>(
                    keyName,
                    (provider, _) => provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found."));
            }

            // register a keyed KEK service when missing
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKekService))) {
                services.AddKeyedSingleton<TKekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyStoreName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyStoreName}' was not found.");
                        return CreateBuiltInService<TKekService>(keyStore, aesGcmKeySize);
                    });
            }

            // register keyed TwoKeyEncryptionService
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

        /// <summary>Registers a keyed two-key service and configures its key store.</summary>
        /// <typeparam name="TKeyStore">Key store type</typeparam>
        /// <param name="keyName">Keyed-registration name for the encryption service</param>
        /// <param name="configKeyStore">Factory that configures the store (registered under keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key width for <see cref="AesGcmEncryptionService" /> as DEK/KEK.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore>(
            string keyName,
            Func<IServiceProvider, TKeyStore> configKeyStore,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore
            => services.AddEncryptionServiceKeyed<TKeyStore, AesGcmEncryptionService, AesGcmEncryptionService>(keyName, configKeyStore, aesGcmKeySize);

        /// <summary>Registers a keyed two-key service with store configuration; one service type covers DEK and KEK.</summary>
        /// <typeparam name="TKeyStore">Key store type</typeparam>
        /// <typeparam name="TEncryptionService">Service type used for both DEK and KEK</typeparam>
        /// <param name="keyName">Keyed-registration name for the encryption service</param>
        /// <param name="configKeyStore">Factory that configures the store (registered under keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key width when <typeparamref name="TEncryptionService" /> is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore, TEncryptionService>(
            string keyName,
            Func<IServiceProvider, TKeyStore> configKeyStore,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore where TEncryptionService : class, IEncryptionService
            => services.AddEncryptionServiceKeyed<TKeyStore, TEncryptionService, TEncryptionService>(keyName, configKeyStore, aesGcmKeySize);

        /// <summary>Registers a keyed two-key service with store configuration and separate DEK/KEK service types.</summary>
        /// <typeparam name="TKeyStore">Key store type</typeparam>
        /// <typeparam name="TDekService">Data Encryption Key (DEK) service type</typeparam>
        /// <typeparam name="TKekService">Key Encryption Key (KEK) service type</typeparam>
        /// <param name="keyName">Keyed-registration name for the encryption service</param>
        /// <param name="configKeyStore">Factory that configures the store (registered under keyName)</param>
        /// <param name="aesGcmKeySize">AES-GCM key width when DEK or KEK is <see cref="AesGcmEncryptionService" />.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddEncryptionServiceKeyed<TKeyStore, TDekService, TKekService>(
            string keyName,
            Func<IServiceProvider, TKeyStore> configKeyStore,
            AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
            where TKeyStore : class, IKeyStore where TDekService : class, IEncryptionService where TKekService : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNull(configKeyStore);

            // register the store under keyName
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKeyStore))) {
                services.AddKeyedSingleton<TKeyStore>(keyName, (provider, _) => configKeyStore(provider));
                services.AddKeyedSingleton<IKeyStore>(
                    keyName,
                    (provider, _) => provider.GetKeyedService<TKeyStore>(keyName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyName}' of type '{typeof(TKeyStore).Name}' was not found."));
            }

            // register keyed DEK service
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TDekService))) {
                services.AddKeyedSingleton<TDekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyName}' was not found.");
                        return CreateBuiltInService<TDekService>(keyStore, aesGcmKeySize);
                    });

                // register IEncryptionService for the DEK
                services.AddKeyedSingleton<IEncryptionService>(
                    keyName,
                    (provider, _) => provider.GetKeyedService<TDekService>(keyName) ??
                        throw new InvalidOperationException($"Keyed encryption service '{keyName}' of type '{typeof(TDekService).Name}' was not found."));
            }

            // register keyed KEK service
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyName) && s.ServiceType == typeof(TKekService))) {
                services.AddKeyedSingleton<TKekService>(
                    keyName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<IKeyStore>(keyName);
                        OperationHelpers.ThrowIfNull(keyStore, $"Keyed key store service '{keyName}' was not found.");
                        return CreateBuiltInService<TKekService>(keyStore, aesGcmKeySize);
                    });
            }

            // register keyed TwoKeyEncryptionService
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

        /// <summary>Registers an RSA encryptor (public key).</summary>
        /// <param name="publicPemPath">Path to the RSA public-key PEM</param>
        /// <param name="pfxPath">Path to a PFX certificate (alternative to PEM)</param>
        /// <param name="password">PFX password</param>
        /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
        /// <param name="maxChunkSize">Largest plaintext chunk. Null means compute automatically.</param>
        /// <returns>The same collection for fluent chaining</returns>
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

        /// <summary>Registers an RSA decryptor (private key).</summary>
        /// <param name="privatePemPath">Path to the RSA private-key PEM</param>
        /// <param name="pfxPath">Path to a PFX certificate (alternative to PEM)</param>
        /// <param name="password">PFX password</param>
        /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddRsaDecryptor(string? privatePemPath = null, string? pfxPath = null, string? password = null, RSAEncryptionPadding? padding = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            return services.AddScoped(_ => new RsaDecryptor(privatePemPath, pfxPath, password, padding));
        }

        /// <summary>Registers both an RSA encryptor (public key) and decryptor (private key).</summary>
        /// <param name="publicPemPath">Path to the RSA public-key PEM</param>
        /// <param name="privatePemPath">Path to the RSA private-key PEM</param>
        /// <param name="pfxPath">PFX path (alternative to PEM; used for both encryptor and decryptor)</param>
        /// <param name="password">PFX password</param>
        /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
        /// <param name="maxChunkSize">Largest plaintext chunk. Null means compute automatically.</param>
        /// <returns>The same collection for fluent chaining</returns>
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

        /// <summary>Registers the AES-GCM + RSA hybrid encryption service.</summary>
        /// <param name="publicPemPath">Path to the RSA public-key PEM</param>
        /// <param name="privatePemPath">Path to the RSA private-key PEM</param>
        /// <param name="pfxPath">Path to a PFX certificate (alternative to PEM)</param>
        /// <param name="password">PFX password</param>
        /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
        /// <returns>The same collection for fluent chaining</returns>
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

        /// <summary>Maps unkeyed <see cref="IEncryptionService" /> onto an already-registered concrete singleton.</summary>
        /// <typeparam name="TConcrete">Concrete encryption service type already registered on this collection.</typeparam>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddDefaultEncryptionService<TConcrete>()
            where TConcrete : class, IEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IEncryptionService>(sp => sp.GetRequiredService<TConcrete>());
            return services;
        }

        /// <summary>Maps unkeyed <see cref="ITwoKeyEncryptionService" /> onto an already-registered concrete singleton. Prefer keyed registration for multiple envelopes.</summary>
        /// <typeparam name="TConcrete">Concrete two-key service type already registered on this collection.</typeparam>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddDefaultTwoKeyEncryptionService<TConcrete>()
            where TConcrete : class, ITwoKeyEncryptionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<ITwoKeyEncryptionService>(sp => sp.GetRequiredService<TConcrete>());
            return services;
        }
    }
}