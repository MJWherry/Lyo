using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Keystore.Aws;

public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary> Adds AWS key store to the service collection. </summary>
        /// <param name="configure">Function that receives the service provider and returns the configured secret name prefix</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAwsKeyStore(Func<IServiceProvider, string> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
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
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
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

        /// <summary>Registers IAmazonSecretsManager from configuration. Binds AwsKeystoreOptions from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "AwsKeyStore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAmazonSecretsManagerFromConfiguration(IConfiguration configuration, string configSectionName = "AwsKeyStore")
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

            // Configure AwsKeystoreOptions from configuration
            services.AddSingleton<AwsKeystoreOptions>(_ => {
                var options = new AwsKeystoreOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists()) {
                    options.AccessKeyId = section["AccessKeyId"];
                    options.SecretAccessKey = section["SecretAccessKey"];
                    options.Region = section["Region"];
                    options.SecretNamePrefix = section["SecretNamePrefix"];
                }

                return options;
            });

            // Register IAmazonSecretsManager if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager))) {
                services.AddSingleton<IAmazonSecretsManager>(provider => {
                    var options = provider.GetRequiredService<AwsKeystoreOptions>();
                    var region = !string.IsNullOrEmpty(options.Region) ? RegionEndpoint.GetBySystemName(options.Region) : RegionEndpoint.USEast2; // Default region
                    var config = new AmazonSecretsManagerConfig { RegionEndpoint = region };
                    if (!string.IsNullOrEmpty(options.AccessKeyId) && !string.IsNullOrEmpty(options.SecretAccessKey))
                        return new AmazonSecretsManagerClient(new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey), config);

                    // If no credentials provided, use default credential chain
                    return new AmazonSecretsManagerClient(config);
                });
            }

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
        /// Adds two-key encryption service with AWS KeyStore using configuration from appsettings. Binds AwsKeystoreOptions from the specified configuration section. Usage:
        /// services.AddTwoKeyEncryptionFromConfiguration&lt;AwsKeyStore&gt;("two-key-aws", "AwsKeystore")
        /// </summary>
        /// <typeparam name="TKeyStore">Must be AwsKeyStore</typeparam>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="configSectionName">The configuration section name (e.g., "AwsKeystore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionFromConfiguration<TKeyStore>(IConfiguration configuration, string keyedServiceName, string configSectionName)
            where TKeyStore : AwsKeyStore
            => services.AddTwoKeyEncryptionFromConfiguration(configuration, keyedServiceName, configSectionName);

        /// <summary>Adds two-key encryption service with AWS KeyStore using configuration from appsettings. Binds AwsKeystoreOptions from the specified configuration section.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="configSectionName">The configuration section name (e.g., "AwsKeystore")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionFromConfiguration(IConfiguration configuration, string keyedServiceName, string configSectionName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName, nameof(keyedServiceName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));

            // Register IAmazonSecretsManager from configuration if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager)))
                services.AddAmazonSecretsManagerFromConfiguration(configuration, configSectionName);

            // Configure AwsKeystoreOptions from configuration (if not already registered)
            if (!services.Any(s => s.ServiceType == typeof(AwsKeystoreOptions))) {
                services.AddSingleton<AwsKeystoreOptions>(_ => {
                    var section = configuration.GetSection(configSectionName);
                    var options = new AwsKeystoreOptions();
                    if (section.Exists()) {
                        options.AccessKeyId = section["AccessKeyId"];
                        options.SecretAccessKey = section["SecretAccessKey"];
                        options.Region = section["Region"];
                        options.SecretNamePrefix = section["SecretNamePrefix"];
                    }

                    return options;
                });
            }

            // Register keyed AwsKeyStore - reads SecretNamePrefix from options at resolution time
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName) && s.ServiceType == typeof(AwsKeyStore))) {
                services.AddKeyedSingleton<AwsKeyStore>(
                    keyedServiceName, (provider, serviceKey) => {
                        var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                        var options = provider.GetRequiredService<AwsKeystoreOptions>();
                        var prefix = options.SecretNamePrefix ??
                            throw new InvalidOperationException($"SecretNamePrefix is required in configuration section '{configSectionName}'");

                        return new(secretsManager, prefix);
                    });

                services.AddKeyedSingleton<IKeyStore>(
                    keyedServiceName,
                    (provider, serviceKey) => provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{typeof(AwsKeyStore).Name}' was not found."));
            }

            // Register DEK and KEK services (keyed) - singleton since they're stateless
            if (!services.Any(s => s.ServiceType == typeof(AesGcmEncryptionService) && s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName))) {
                services.AddKeyedSingleton<AesGcmEncryptionService>(
                    keyedServiceName, (provider, serviceKey) => {
                        var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ?? throw new InvalidOperationException(
                            $"Keyed key store service '{keyedServiceName}' of type '{typeof(AwsKeyStore).Name}' was not found.");

                        return new(keyStore);
                    });

                // Register interface for encryption service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyedServiceName,
                    (provider, serviceKey) => provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{typeof(AesGcmEncryptionService).Name}' was not found."));
            }

            // Register TwoKeyEncryptionService (keyed) - singleton since it's stateless
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyedServiceName, (provider, serviceKey) => {
                    var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{typeof(AwsKeyStore).Name}' was not found.");

                    var dekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{typeof(AesGcmEncryptionService).Name}' was not found.");

                    var kekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{typeof(AesGcmEncryptionService).Name}' was not found.");

                    return new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(dekService, kekService, keyStore);
                });
        }

        /// <summary>Adds two-key encryption service with AWS KeyStore. This overload automatically configures the AwsKeyStore with the provided secret prefix and AWS config.</summary>
        /// <param name="keyedServiceName">The key name for the keyed service registration</param>
        /// <param name="secretNamePrefix">The AWS Secrets Manager secret name prefix (e.g., "dev/CourtCanary/FileStore")</param>
        /// <param name="awsConfig">Optional AWS configuration. If null, uses IAmazonSecretsManager from DI.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwoKeyEncryptionServiceKeyed(string keyedServiceName, string secretNamePrefix, AwsKeystoreOptions? awsConfig)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName, nameof(keyedServiceName));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(secretNamePrefix, nameof(secretNamePrefix));

            // Register IAmazonSecretsManager if awsConfig is provided and not already registered
            if (awsConfig != null && !services.Any(s => s.ServiceType == typeof(IAmazonSecretsManager))) {
                services.AddSingleton<IAmazonSecretsManager>(provider => {
                    var region = !string.IsNullOrEmpty(awsConfig.Region) ? RegionEndpoint.GetBySystemName(awsConfig.Region) : RegionEndpoint.USEast2; // Default region
                    var config = new AmazonSecretsManagerConfig { RegionEndpoint = region };
                    if (!string.IsNullOrEmpty(awsConfig.AccessKeyId) && !string.IsNullOrEmpty(awsConfig.SecretAccessKey))
                        return new AmazonSecretsManagerClient(new BasicAWSCredentials(awsConfig.AccessKeyId, awsConfig.SecretAccessKey), config);

                    // If no credentials provided, use default credential chain
                    return new AmazonSecretsManagerClient(config);
                });
            }

            // Register keyed AwsKeyStore
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName) && s.ServiceType == typeof(AwsKeyStore))) {
                services.AddKeyedSingleton<AwsKeyStore>(
                    keyedServiceName, (provider, serviceKey) => {
                        var secretsManager = provider.GetRequiredService<IAmazonSecretsManager>();
                        return new(secretsManager, secretNamePrefix);
                    });

                services.AddKeyedSingleton<IKeyStore>(
                    keyedServiceName,
                    (provider, serviceKey) => provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{typeof(AwsKeyStore).Name}' was not found."));
            }

            // Register DEK and KEK services (keyed) - singleton since they're stateless
            if (!services.Any(s => s.ServiceType == typeof(AesGcmEncryptionService) && s.ServiceKey != null && s.ServiceKey.Equals(keyedServiceName))) {
                services.AddKeyedSingleton<AesGcmEncryptionService>(
                    keyedServiceName, (provider, serviceKey) => {
                        var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ?? throw new InvalidOperationException(
                            $"Keyed key store service '{keyedServiceName}' of type '{typeof(AwsKeyStore).Name}' was not found.");

                        return new(keyStore);
                    });

                // Register interface for encryption service
                services.AddKeyedSingleton<IEncryptionService>(
                    keyedServiceName,
                    (provider, serviceKey) => provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{typeof(AesGcmEncryptionService).Name}' was not found."));
            }

            // Register TwoKeyEncryptionService (keyed) - singleton since it's stateless
            return services.AddKeyedSingleton<ITwoKeyEncryptionService>(
                keyedServiceName, (provider, serviceKey) => {
                    var keyStore = provider.GetKeyedService<AwsKeyStore>(keyedServiceName) ??
                        throw new InvalidOperationException($"Keyed key store service '{keyedServiceName}' of type '{typeof(AwsKeyStore).Name}' was not found.");

                    var dekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{typeof(AesGcmEncryptionService).Name}' was not found.");

                    var kekService = provider.GetKeyedService<AesGcmEncryptionService>(keyedServiceName) ?? throw new InvalidOperationException(
                        $"Keyed encryption service '{keyedServiceName}' of type '{typeof(AesGcmEncryptionService).Name}' was not found.");

                    return new TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService>(dekService, kekService, keyStore);
                });
        }
    }
}