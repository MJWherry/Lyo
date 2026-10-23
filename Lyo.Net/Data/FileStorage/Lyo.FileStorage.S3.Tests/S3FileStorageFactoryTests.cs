using System.Reflection;
using Amazon.S3;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Multipart;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.S3.Tests;

public sealed class S3FileStorageFactoryTests
{
    [Fact]
    public void AddS3_TwoBuckets_BuildsDistinctStoresAndMultipartWithoutUnkeyedClients()
    {
        using var provider = Provider(services => {
            services.AddSingleton(Metadata());
            services.AddSingleton<ITwoKeyEncryptionService>(_ => throw new InvalidOperationException("unkeyed"));
            services.AddFileStorageFactory(stores => {
                stores.AddS3(
                    "alpha", options => {
                        options.BucketName = "alpha";
                        options.AccessKeyId = "AKIATEST";
                        options.SecretAccessKey = "secret";
                        options.ServiceUrl = "http://127.0.0.1:9";
                    });

                stores.AddS3(
                    "beta", builder => builder.Configure(options => {
                        options.BucketName = "beta";
                        options.AccessKeyId = "AKIATEST";
                        options.SecretAccessKey = "secret";
                        options.ServiceUrl = "http://127.0.0.1:9";
                    }));
            });
        });

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();
        var alpha = factory.Get("alpha");
        var beta = factory.Get("beta");
        Assert.Equal("alpha", Options(alpha).BucketName);
        Assert.Equal("beta", Options(beta).BucketName);
        Assert.Null(Field(alpha, "TwoKeyEncryptionService"));
        Assert.Null(provider.GetService<S3FileStorageOptions>());
        Assert.Null(provider.GetService<IAmazonS3>());
        var alphaMultipart = scope.ServiceProvider.GetRequiredKeyedService<IMultipartUploadService>("alpha");
        var betaMultipart = scope.ServiceProvider.GetRequiredKeyedService<IMultipartUploadService>("beta");
        Assert.NotSame(alphaMultipart, betaMultipart);
        Assert.Same(alpha, Field(alphaMultipart, "_storage"));
        Assert.Same(Field(alpha, "_s3Client"), Field(alphaMultipart, "_s3"));
        Assert.NotSame(Field(alphaMultipart, "_s3"), Field(betaMultipart, "_s3"));
        Assert.Equal("alpha", ((S3FileStorageOptions)Field(alphaMultipart, "_options")!).BucketName);
    }

    [Fact]
    public void FromConfiguration_ProviderNameIgnoresCase_AndRegistersMultipart()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["FileStorage:Stores:archive:Provider"] = "s3",
                    ["FileStorage:Stores:archive:BucketName"] = "archive",
                    ["FileStorage:Stores:archive:AccessKeyId"] = "AKIATEST",
                    ["FileStorage:Stores:archive:SecretAccessKey"] = "secret",
                    ["FileStorage:Stores:archive:ServiceUrl"] = "http://127.0.0.1:9"
                })
            .Build();

        using var provider = Provider(services => {
            services.AddSingleton(Metadata());
            services.AddS3FileStorageProvider();
            services.AddFileStorageFactoryFromConfiguration(configuration);
        });

        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>().Get("archive");
        Assert.Equal("archive", Options(store).BucketName);
        Assert.NotNull(scope.ServiceProvider.GetRequiredKeyedService<IMultipartUploadService>("archive"));
        Assert.Null(provider.GetService<IFileStorageService>());
    }

    [Fact]
    public void AddS3FileStorageProvider_RegistersNoStore()
    {
        var services = new ServiceCollection();
        services.AddS3FileStorageProvider();
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFileStorageStoreProvider) && descriptor.ServiceKey is null);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFileStorageService));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFileStorageFactory));
    }

    [Fact]
    public void AddS3FileStorage_ThrowsWhenAnUnkeyedStoreExists()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileStorageService>(DispatchProxy.Create<IFileStorageService, IdleProxy>());
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddS3FileStorage(builder => builder.Configure(options => {
            options.BucketName = "bucket";
            options.AccessKeyId = "AKIATEST";
            options.SecretAccessKey = "secret";
            options.ServiceUrl = "http://127.0.0.1:9";
        })));

        Assert.Contains("unkeyed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static IFileMetadataStore Metadata() => DispatchProxy.Create<IFileMetadataStore, IdleProxy>();

    private static S3FileStorageOptions Options(IFileStorageService service) => (S3FileStorageOptions)Field(service, "Options")!;

    private static object? Field(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType) {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(target);
        }

        throw new MissingFieldException(target.GetType().FullName, name);
    }

    private class IdleProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }
}