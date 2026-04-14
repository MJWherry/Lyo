using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.S3;

/// <summary>Helpers for common S3-compatible object storage providers (MinIO, Wasabi, Cloudflare R2, etc.).</summary>
public static class S3FileStorageS3CompatibleExtensions
{
    /// <summary>Configuration section for MinIO-style <see cref="S3FileStorageOptions" /> (bucket, <see cref="S3FileStorageOptions.ServiceUrl" />, keys, optional region).</summary>
    public const string MinioFileStorageConfigurationSectionName = "MinioFileStorage";

    /// <summary>Configuration section for Wasabi (set <see cref="S3FileStorageOptions.Region" /> to the Wasabi region code).</summary>
    public const string WasabiFileStorageConfigurationSectionName = "WasabiFileStorage";

    /// <summary>Configuration section for DigitalOcean Spaces (set <see cref="S3FileStorageOptions.Region" /> to the region slug, e.g. <c>nyc3</c>).</summary>
    public const string DigitalOceanSpacesFileStorageConfigurationSectionName = "DigitalOceanSpacesFileStorage";

    /// <summary>Configuration section for Cloudflare R2 (set <see cref="S3FileStorageOptions.ProviderAccountId" /> to the account id).</summary>
    public const string CloudflareR2FileStorageConfigurationSectionName = "CloudflareR2FileStorage";

    /// <summary>Configuration section for Scaleway Object Storage (set <see cref="S3FileStorageOptions.Region" />, e.g. <c>fr-par</c>).</summary>
    public const string ScalewayFileStorageConfigurationSectionName = "ScalewayFileStorage";

    /// <summary>Configuration section for Linode Object Storage (set <see cref="S3FileStorageOptions.Region" />, e.g. <c>us-east-1</c>).</summary>
    public const string LinodeObjectStorageConfigurationSectionName = "LinodeObjectStorage";

    // --- Endpoint URL builders (documented provider patterns) ---

    /// <summary>Returns a base URL for MinIO when only a host (and optional port) is given without a scheme; otherwise trims and removes a trailing slash.</summary>
    public static string GetMinioServiceUrl(string hostOrUri)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(hostOrUri, nameof(hostOrUri));
        var t = hostOrUri.Trim().TrimEnd('/');
        if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return t;

        return $"http://{t.Trim()}";
    }

    /// <summary>Wasabi S3 endpoint for the given region (e.g. <c>us-east-1</c>).</summary>
    public static string GetWasabiServiceUrl(string wasabiRegion)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(wasabiRegion, nameof(wasabiRegion));
        return $"https://s3.{wasabiRegion.Trim()}.wasabisys.com";
    }

    /// <summary>DigitalOcean Spaces endpoint for the region slug (e.g. <c>nyc3</c>, <c>ams3</c>).</summary>
    public static string GetDigitalOceanSpacesServiceUrl(string regionSlug)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(regionSlug, nameof(regionSlug));
        return $"https://{regionSlug.Trim()}.digitaloceanspaces.com";
    }

    /// <summary>Cloudflare R2 S3 API endpoint for the account id.</summary>
    public static string GetCloudflareR2ServiceUrl(string accountId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(accountId, nameof(accountId));
        return $"https://{accountId.Trim()}.r2.cloudflarestorage.com";
    }

    /// <summary>Scaleway Object Storage endpoint for the region (e.g. <c>fr-par</c>, <c>nl-ams</c>).</summary>
    public static string GetScalewayObjectStorageServiceUrl(string region)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(region, nameof(region));
        return $"https://s3.{region.Trim()}.scw.cloud";
    }

    /// <summary>Linode Object Storage endpoint for the cluster region (e.g. <c>us-east-1</c>, <c>eu-central-1</c>).</summary>
    public static string GetLinodeObjectStorageServiceUrl(string region)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(region, nameof(region));
        return $"https://{region.Trim()}.linodeobjects.com";
    }

    // --- Apply defaults onto options (same pattern as Backblaze B2) ---

    /// <summary>
    /// Normalizes <see cref="S3FileStorageOptions.ServiceUrl" /> and sets a default <see cref="S3FileStorageOptions.Region" /> for the AWS SDK when using a custom MinIO endpoint.
    /// Does not set <see cref="S3FileStorageOptions.ServiceUrl" /> if it is empty — configure the MinIO server URL explicitly.
    /// </summary>
    public static void ApplyMinioDefaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
            return;

        options.ServiceUrl = GetMinioServiceUrl(options.ServiceUrl);
        options.Region ??= "us-east-1";
    }

    /// <summary>Sets <see cref="S3FileStorageOptions.ServiceUrl" /> from <see cref="S3FileStorageOptions.Region" /> when the URL is not already set.</summary>
    public static void ApplyWasabiDefaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) || string.IsNullOrWhiteSpace(options.Region))
            return;

        options.ServiceUrl = GetWasabiServiceUrl(options.Region);
    }

    /// <summary>Sets <see cref="S3FileStorageOptions.ServiceUrl" /> from <see cref="S3FileStorageOptions.Region" /> (Spaces region slug) when the URL is not already set.</summary>
    public static void ApplyDigitalOceanSpacesDefaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) || string.IsNullOrWhiteSpace(options.Region))
            return;

        options.ServiceUrl = GetDigitalOceanSpacesServiceUrl(options.Region);
    }

    /// <summary>
    /// Sets <see cref="S3FileStorageOptions.ServiceUrl" /> from <see cref="S3FileStorageOptions.ProviderAccountId" /> when the URL is not already set.
    /// Optionally set <see cref="S3FileStorageOptions.Region" /> to <c>auto</c> for R2 if your tooling expects a region string.
    /// </summary>
    public static void ApplyCloudflareR2Defaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) || string.IsNullOrWhiteSpace(options.ProviderAccountId))
            return;

        options.ServiceUrl = GetCloudflareR2ServiceUrl(options.ProviderAccountId);
        options.Region ??= "auto";
    }

    /// <summary>Sets <see cref="S3FileStorageOptions.ServiceUrl" /> from <see cref="S3FileStorageOptions.Region" /> when the URL is not already set.</summary>
    public static void ApplyScalewayDefaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) || string.IsNullOrWhiteSpace(options.Region))
            return;

        options.ServiceUrl = GetScalewayObjectStorageServiceUrl(options.Region);
    }

    /// <summary>Sets <see cref="S3FileStorageOptions.ServiceUrl" /> from <see cref="S3FileStorageOptions.Region" /> when the URL is not already set.</summary>
    public static void ApplyLinodeObjectStorageDefaults(this S3FileStorageOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl) || string.IsNullOrWhiteSpace(options.Region))
            return;

        options.ServiceUrl = GetLinodeObjectStorageServiceUrl(options.Region);
    }

    // --- DI registration (mirrors <see cref="S3FileStorageBackblazeExtensions" />) ---

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForMinio(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = MinioFileStorageConfigurationSectionName)
        => AddKeyedForApply(services, keyName, configuration, configSectionName, static o => o.ApplyMinioDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForMinio(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
        => AddKeyedForConfigure(services, keyName, configure, static o => o.ApplyMinioDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForWasabi(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = WasabiFileStorageConfigurationSectionName)
        => AddKeyedForApply(services, keyName, configuration, configSectionName, static o => o.ApplyWasabiDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForWasabi(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
        => AddKeyedForConfigure(services, keyName, configure, static o => o.ApplyWasabiDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForDigitalOceanSpaces(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = DigitalOceanSpacesFileStorageConfigurationSectionName)
        => AddKeyedForApply(services, keyName, configuration, configSectionName, static o => o.ApplyDigitalOceanSpacesDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForDigitalOceanSpaces(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
        => AddKeyedForConfigure(services, keyName, configure, static o => o.ApplyDigitalOceanSpacesDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForCloudflareR2(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = CloudflareR2FileStorageConfigurationSectionName)
        => AddKeyedForApply(services, keyName, configuration, configSectionName, static o => o.ApplyCloudflareR2Defaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForCloudflareR2(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
        => AddKeyedForConfigure(services, keyName, configure, static o => o.ApplyCloudflareR2Defaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForScaleway(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = ScalewayFileStorageConfigurationSectionName)
        => AddKeyedForApply(services, keyName, configuration, configSectionName, static o => o.ApplyScalewayDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForScaleway(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
        => AddKeyedForConfigure(services, keyName, configure, static o => o.ApplyScalewayDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForLinodeObjectStorage(
        this IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName = LinodeObjectStorageConfigurationSectionName)
        => AddKeyedForApply(services, keyName, configuration, configSectionName, static o => o.ApplyLinodeObjectStorageDefaults());

    public static S3FileStorageServiceBuilder AddS3FileStorageServiceKeyedForLinodeObjectStorage(this IServiceCollection services, string keyName, Action<S3FileStorageOptions> configure)
        => AddKeyedForConfigure(services, keyName, configure, static o => o.ApplyLinodeObjectStorageDefaults());

    private static S3FileStorageServiceBuilder AddKeyedForApply(
        IServiceCollection services,
        string keyName,
        IConfiguration configuration,
        string configSectionName,
        Action<S3FileStorageOptions> applyDefaults)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        ArgumentHelpers.ThrowIfNull(applyDefaults, nameof(applyDefaults));
        RegisterOptionsFromConfiguration(services, configuration, configSectionName, applyDefaults);
        return services.AddS3FileStorageServiceKeyed(keyName);
    }

    private static S3FileStorageServiceBuilder AddKeyedForConfigure(
        IServiceCollection services,
        string keyName,
        Action<S3FileStorageOptions> configure,
        Action<S3FileStorageOptions> applyDefaults)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName, nameof(keyName));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        ArgumentHelpers.ThrowIfNull(applyDefaults, nameof(applyDefaults));
        if (!services.Any(s => s.ServiceType == typeof(S3FileStorageOptions))) {
            services.AddSingleton<S3FileStorageOptions>(_ => {
                var options = new S3FileStorageOptions();
                configure(options);
                applyDefaults(options);
                return options;
            });
        }

        return services.AddS3FileStorageServiceKeyed(keyName);
    }

    private static void RegisterOptionsFromConfiguration(
        IServiceCollection services,
        IConfiguration configuration,
        string configSectionName,
        Action<S3FileStorageOptions> applyDefaults)
    {
        if (services.Any(s => s.ServiceType == typeof(S3FileStorageOptions)))
            return;

        services.AddSingleton<S3FileStorageOptions>(_ => {
            var section = configuration.GetSection(configSectionName);
            var options = new S3FileStorageOptions();
            if (section.Exists())
                section.Bind(options);

            applyDefaults(options);
            return options;
        });
    }
}
