using Lyo.Api.Client;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileStorage;
using Lyo.FileStorage.S3;
using Lyo.FileStorage.Web.Components.Services;
using Lyo.Keystore;
using Lyo.Keystore.Aws;

namespace Lyo.Gateway.Services;

public static class FileStorageWorkbenchExtensions
{
    public static IServiceCollection AddFileStorageWorkbenchSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageWorkbenchOptions>(configuration.GetSection(FileStorageWorkbenchOptions.SectionName));
        services.AddScoped<FileStorageWorkbenchServiceResolver>();
        var options = new FileStorageWorkbenchOptions();
        configuration.GetSection(FileStorageWorkbenchOptions.SectionName).Bind(options);
        var keyStoreKey = string.IsNullOrWhiteSpace(options.KeyStoreServiceKey) ? "gateway-filestorage" : options.KeyStoreServiceKey;
        var fileStorageKey = string.IsNullOrWhiteSpace(options.FileStorageServiceKey) ? "gateway-filestorage" : options.FileStorageServiceKey;
        if (options.UseTestApiServices) {
            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(fileStorageKey) && s.ServiceType == typeof(IFileStorageService))) {
                services.AddKeyedScoped<IFileStorageService>(
                    fileStorageKey,
                    (provider, _) => new TestApiFileStorageService(
                        provider.GetRequiredService<IApiClient>(), options.ApiRoutePrefix, options.StreamUploadRelativePath));
            }

            if (!services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(keyStoreKey) && s.ServiceType == typeof(IKeyStore)))
                services.AddKeyedScoped<IKeyStore>(keyStoreKey, (provider, _) => new TestApiKeyStore(provider.GetRequiredService<IApiClient>(), options.ApiRoutePrefix));

            services.AddScoped<IFileStorageWorkbenchQueryService>(provider
                => new TestApiFileStorageWorkbenchQueryService(provider.GetRequiredService<IApiClient>(), options.ApiRoutePrefix));

            return services;
        }

        if (!options.AutoRegisterS3Services)
            return services;

        services.AddTwoKeyEncryptionFromConfiguration(configuration, keyStoreKey, options.AwsKeyStoreConfigSection);
        services.AddPostgresFileMetadataStoreKeyed(options.MetadataStoreKey).ConfigurePostgresFileStore(options.MetadataStoreConfigSection).Build();
        services.AddS3FileStorageServiceKeyed(fileStorageKey)
            .UseFileMetadataStore(options.MetadataStoreKey)
            .UseEncryptionService(keyStoreKey)
            .ConfigureS3FileStorage(options.S3FileStorageConfigSection)
            .Build(configuration);

        return services;
    }
}