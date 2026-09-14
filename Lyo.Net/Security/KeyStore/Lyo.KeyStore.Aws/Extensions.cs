using Amazon;
using Amazon.SecretsManager;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.KeyStore.Aws;

public static class Extensions
{
    private static AwsKeyStoreOptions BindOptions(IConfiguration configuration, string configSectionName)
    {
        var options = new AwsKeyStoreOptions();
        configuration.GetSection(configSectionName).Bind(options);
        options.Validate();
        return options;
    }

    private static IAmazonSecretsManager CreateSecretsManagerClient(AwsKeyStoreOptions options)
    {
        var region = !string.IsNullOrEmpty(options.Region) ? RegionEndpoint.GetBySystemName(options.Region) : RegionEndpoint.USEast2;
        var config = new AmazonSecretsManagerConfig { RegionEndpoint = region };
        var credentials = AwsKeyStoreCredentialHelpers.Resolve(options.AccessKeyId, options.SecretAccessKey, options.Profile);
        return credentials is null ? new AmazonSecretsManagerClient(config) : new AmazonSecretsManagerClient(credentials, config);
    }

    /// <param name="services">DI service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the AWS key store on the collection.</summary>
        /// <param name="configure">Receives the provider and returns the secret-name prefix</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddAwsKeyStore(Func<IServiceProvider, string> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<AwsKeyStore>(provider => {
                var secretNamePrefix = configure(provider);
                var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                return new(secretsManager, secretNamePrefix);
            });

            return services;
        }

        /// <summary>Registers the AWS key store by binding options from configuration.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section name (default "AwsKeyStore")</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddAwsKeyStoreFromConfiguration(IConfiguration configuration, string configSectionName = "AwsKeyStore")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            // add IAmazonSecretsManager from configuration when it is not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddAmazonSecretsManagerFromConfiguration(configuration, configSectionName);

            services.AddSingleton<IKeyStore>(provider => {
                var secretNamePrefix = configuration.GetSection(configSectionName)["SecretNamePrefix"] ?? "lyo/kek";
                var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                return new AwsKeyStore(secretsManager, secretNamePrefix);
            });

            return services;
        }

        /// <summary>Registers IAmazonSecretsManager from configuration, binding AwsKeyStoreOptions from the named section.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section name (default "AwsKeyStore")</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddAmazonSecretsManagerFromConfiguration(IConfiguration configuration, string configSectionName = "AwsKeyStore")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            services.AddSingleton(BindOptions(configuration, configSectionName));

            // add IAmazonSecretsManager when it is not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddSingleton<IAmazonSecretsManager>(provider => CreateSecretsManagerClient(provider.GetRequiredService<AwsKeyStoreOptions>()));

            return services;
        }

        /// <summary>
        /// Registers two-key encryption with AWS KeyStore and applies the given secret prefix to AwsKeyStore. Example:
        /// services.AddTwoKeyEncryption&lt;AwsKeyStore&gt;("two-key-aws", "dev/FileStore")
        /// </summary>
        /// <typeparam name="TKeyStore">Must be AwsKeyStore</typeparam>
        /// <param name="keyedServiceName">Keyed-registration name</param>
        /// <param name="secretNamePrefix">Secrets Manager prefix (for example "dev/FileStore")</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed<TKeyStore>(string keyedServiceName, string secretNamePrefix)
            where TKeyStore : AwsKeyStore
            => services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix, null);

        /// <summary>Registers two-key encryption with AWS KeyStore and applies the given secret prefix to AwsKeyStore.</summary>
        /// <param name="keyedServiceName">Keyed-registration name</param>
        /// <param name="secretNamePrefix">Secrets Manager prefix (for example "dev/FileStore")</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed(string keyedServiceName, string secretNamePrefix)
            => services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix, null);

        /// <summary>
        /// Registers two-key encryption with AWS KeyStore, binding AwsKeyStoreOptions from appsettings. Example:
        /// services.AddTwoKeyEncryptionFromConfiguration&lt;AwsKeyStore&gt;("two-key-aws", "AwsKeyStore")
        /// </summary>
        /// <typeparam name="TKeyStore">Must be AwsKeyStore</typeparam>
        /// <param name="configuration">Host configuration (for example builder.Configuration).</param>
        /// <param name="keyedServiceName">Keyed-registration name</param>
        /// <param name="configSectionName">Section name (for example "AwsKeyStore")</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionFromConfiguration<TKeyStore>(IConfiguration configuration, string keyedServiceName, string configSectionName)
            where TKeyStore : AwsKeyStore
            => services.AddTwoKeyEncryptionFromConfiguration(configuration, keyedServiceName, configSectionName);

        /// <summary>Registers two-key encryption with AWS KeyStore, binding AwsKeyStoreOptions from the named appsettings section.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration).</param>
        /// <param name="keyedServiceName">Keyed-registration name</param>
        /// <param name="configSectionName">Section name (for example "AwsKeyStore")</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionFromConfiguration(IConfiguration configuration, string keyedServiceName, string configSectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // add IAmazonSecretsManager from configuration when it is not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddAmazonSecretsManagerFromConfiguration(configuration, configSectionName);

            if (!services.Any(s => s.ServiceType == typeof(AwsKeyStoreOptions)))
                services.AddSingleton(BindOptions(configuration, configSectionName));

            // keyed AwsKeyStore; SecretNamePrefix is read from options when the store is resolved
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName) && s.ServiceType == typeof(AwsKeyStore))) {
                services.AddKeyedSingleton<AwsKeyStore>(
                    keyedServiceName, (provider, _) => {
                        var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                        var options = provider.GetRequiredService<AwsKeyStoreOptions>();
                        OperationHelpers.ThrowIfNullOrWhiteSpace(options.SecretNamePrefix, $"SecretNamePrefix is required in configuration section '{configSectionName}'");
                        var prefix = options.SecretNamePrefix;
                        return new(secretsManager, prefix);
                    });

                services.AddKeyedSingleton<IKeyStore>(
                    keyedServiceName,
                    (provider, _) => provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found."));
            }

            // keyed DEK and KEK services — singleton because they hold no per-call state
            if (!services.Any(s => s.ServiceType == typeof(AesGcmEncryptionService) && s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName))) {
                services.AddKeyedSingleton<AesGcmEncryptionService>(
                    keyedServiceName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ?? throw new InvalidOperationException(
                            $"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found.");

                        return new(keyStore);
                    });

                // keyed IEncryptionService pointing at the same AES-GCM instance
                services.AddKeyedSingleton<IEncryptionService>(
                    keyedServiceName,
                    (provider, _) => provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found."));
            }

            // keyed TwoKeyEncryptionService — singleton because it holds no per-call state
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyedServiceName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found.");

                    var dekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found.");

                    var kekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found.");

                    return new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(dekService, kekService, keyStore);
                });
        }

        /// <summary>Registers two-key encryption with AWS KeyStore, applying the secret prefix and optional AWS client config.</summary>
        /// <param name="keyedServiceName">Keyed-registration name</param>
        /// <param name="secretNamePrefix">Secrets Manager prefix (for example "dev/FileStore")</param>
        /// <param name="awsConfig">Optional AWS settings. Null uses IAmazonSecretsManager from DI.</param>
        /// <returns>The same collection for fluent chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed(string keyedServiceName, string secretNamePrefix, AwsKeyStoreOptions? awsConfig)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(secretNamePrefix);

            // add IAmazonSecretsManager from awsConfig when supplied and not already registered
            if (awsConfig != null && !services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddSingleton<IAmazonSecretsManager>(_ => CreateSecretsManagerClient(awsConfig));

            // keyed AwsKeyStore
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName) && s.ServiceType == typeof(AwsKeyStore))) {
                services.AddKeyedSingleton<AwsKeyStore>(
                    keyedServiceName, (provider, _) => {
                        var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                        return new(secretsManager, secretNamePrefix);
                    });

                services.AddKeyedSingleton<IKeyStore>(
                    keyedServiceName,
                    (provider, _) => provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found."));
            }

            // keyed DEK and KEK services — singleton because they hold no per-call state
            if (!services.Any(s => s.ServiceType == typeof(AesGcmEncryptionService) && s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName))) {
                services.AddKeyedSingleton<AesGcmEncryptionService>(
                    keyedServiceName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ?? throw new InvalidOperationException(
                            $"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found.");

                        return new(keyStore);
                    });

                // keyed IEncryptionService pointing at the same AES-GCM instance
                services.AddKeyedSingleton<IEncryptionService>(
                    keyedServiceName,
                    (provider, _) => provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found."));
            }

            // keyed TwoKeyEncryptionService — singleton because it holds no per-call state
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyedServiceName, (provider, _) => {
                    var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found.");

                    var dekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found.");

                    var kekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found.");

                    return new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(dekService, kekService, keyStore);
                });
        }
    }
}