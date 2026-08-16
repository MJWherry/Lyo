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
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return options;
    }

    private static IAmazonSecretsManager CreateSecretsManagerClient(AwsKeyStoreOptions options)
    {
        var region = !string.IsNullOrEmpty(options.Region) ? RegionEndpoint.GetBySystemName(options.Region) : RegionEndpoint.USEast2;
        var config = new AmazonSecretsManagerConfig { RegionEndpoint = region };
        var credentials = AwsKeyStoreCredentialHelpers.Resolve(options.AccessKeyId, options.SecretAccessKey, options.Profile);
        return credentials is null ? new AmazonSecretsManagerClient(config) : new AmazonSecretsManagerClient(credentials, config);
    }

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary> Adds AWS key store to the service collection. </summary>
        /// <param name="configure">Function that receives the service provider and returns the configured secret name prefix</param>
        /// <returns>The service collection for chaining</returns>
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

        /// <summary> Adds AWS key store to the service collection using configuration binding. </summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "AwsKeyStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAwsKeyStoreFromConfiguration(IConfiguration configuration, string configSectionName = "AwsKeyStore")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            // Register IAmazonSecretsManager from configuration if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddAmazonSecretsManagerFromConfiguration(configuration, configSectionName);

            services.AddSingleton<IKeyStore>(provider => {
                var secretNamePrefix = configuration.GetSection(configSectionName)["SecretNamePrefix"] ?? "lyo/kek";
                var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                return new AwsKeyStore(secretsManager, secretNamePrefix);
            });

            return services;
        }

        /// <summary>Registers IAmazonSecretsManager from configuration. Binds AwsKeyStoreOptions from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "AwsKeyStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAmazonSecretsManagerFromConfiguration(IConfiguration configuration, string configSectionName = "AwsKeyStore")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            services.AddSingleton<AwsKeyStoreOptions>(_ => BindOptions(configuration, configSectionName));

            // Register IAmazonSecretsManager if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddSingleton<IAmazonSecretsManager>(provider => CreateSecretsManagerClient(provider.GetRequiredService<AwsKeyStoreOptions>()));

            return services;
        }

        /// <summary>
        /// Adds two-key encryption service with AWS KeyStore. This overload automatically configures the AwsKeyStore with the provided secret prefix. Usage:
        /// services.AddTwoKeyEncryption&lt;AwsKeyStore&gt;("two-key-aws", "dev/CourtCanary/FileStore")
        /// </summary>
        /// <typeparam name="TKeyStore">Must be AwsKeyStore</typeparam>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="secretNamePrefix">The AWS Secrets Manager secret name prefix (e.g., "dev/CourtCanary/FileStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed<TKeyStore>(string keyedServiceName, string secretNamePrefix)
            where TKeyStore : AwsKeyStore
            => services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix, null);

        /// <summary>Adds two-key encryption service with AWS KeyStore. This overload automatically configures the AwsKeyStore with the provided secret prefix.</summary>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="secretNamePrefix">The AWS Secrets Manager secret name prefix (e.g., "dev/CourtCanary/FileStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed(string keyedServiceName, string secretNamePrefix)
            => services.AddTwoKeyEncryptionServiceKeyed(keyedServiceName, secretNamePrefix, null);

        /// <summary>
        /// Adds two-key encryption service with AWS KeyStore using configuration from appsettings. Binds AwsKeyStoreOptions from the specified configuration section. Usage:
        /// services.AddTwoKeyEncryptionFromConfiguration&lt;AwsKeyStore&gt;("two-key-aws", "AwsKeyStore")
        /// </summary>
        /// <typeparam name="TKeyStore">Must be AwsKeyStore</typeparam>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="configSectionName">The configuration section name (e.g., "AwsKeyStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionFromConfiguration<TKeyStore>(IConfiguration configuration, string keyedServiceName, string configSectionName)
            where TKeyStore : AwsKeyStore
            => services.AddTwoKeyEncryptionFromConfiguration(configuration, keyedServiceName, configSectionName);

        /// <summary>Adds two-key encryption service with AWS KeyStore using configuration from appsettings. Binds AwsKeyStoreOptions from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="configSectionName">The configuration section name (e.g., "AwsKeyStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionFromConfiguration(IConfiguration configuration, string keyedServiceName, string configSectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);

            // Register IAmazonSecretsManager from configuration if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddAmazonSecretsManagerFromConfiguration(configuration, configSectionName);

            if (!services.Any(s => s.ServiceType == typeof(AwsKeyStoreOptions)))
                services.AddSingleton<AwsKeyStoreOptions>(_ => BindOptions(configuration, configSectionName));

            // Register keyed AwsKeyStore - reads SecretNamePrefix from options at resolution time
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

            // Register DEK and KEK services (keyed) - singleton since they're stateless
            if (!services.Any(s => s.ServiceType == typeof(AesGcmEncryptionService) && s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName))) {
                services.AddKeyedSingleton<AesGcmEncryptionService>(
                    keyedServiceName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ?? throw new InvalidOperationException(
                            $"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found.");

                        return new(keyStore);
                    });

                // Register interface for encryption service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyedServiceName,
                    (provider, _) => provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found."));
            }

            // Register TwoKeyEncryptionService (keyed) - singleton since it's stateless
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

        /// <summary>Adds two-key encryption service with AWS KeyStore. This overload automatically configures the AwsKeyStore with the provided secret prefix and AWS config.</summary>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="secretNamePrefix">The AWS Secrets Manager secret name prefix (e.g., "dev/CourtCanary/FileStore")</param>
        /// <param name="awsConfig">Optional AWS configuration. If null, uses IAmazonSecretsManager from DI.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed(string keyedServiceName, string secretNamePrefix, AwsKeyStoreOptions? awsConfig)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(secretNamePrefix);

            // Register IAmazonSecretsManager if awsConfig is provided and not already registered
            if (awsConfig != null && !services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddSingleton<IAmazonSecretsManager>(_ => CreateSecretsManagerClient(awsConfig));

            // Register keyed AwsKeyStore
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

            // Register DEK and KEK services (keyed) - singleton since they're stateless
            if (!services.Any(s => s.ServiceType == typeof(AesGcmEncryptionService) && s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName))) {
                services.AddKeyedSingleton<AesGcmEncryptionService>(
                    keyedServiceName, (provider, _) => {
                        var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ?? throw new InvalidOperationException(
                            $"Keyed key store service '{keyedServiceName}' of type '{nameof(AwsKeyStore)}' was not found.");

                        return new(keyStore);
                    });

                // Register interface for encryption service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyedServiceName,
                    (provider, _) => provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{nameof(AesGcmEncryptionService)}' was not found."));
            }

            // Register TwoKeyEncryptionService (keyed) - singleton since it's stateless
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