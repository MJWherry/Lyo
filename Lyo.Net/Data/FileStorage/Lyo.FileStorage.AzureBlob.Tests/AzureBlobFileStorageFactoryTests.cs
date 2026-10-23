using System.Reflection;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Multipart;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.AzureBlob.Tests;

public sealed class AzureBlobFileStorageFactoryTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public void AddAzureBlob_TwoContainers_MultipartOnlyWhenRequested_AndSkipsUnkeyedEncryption()
    {
        using var provider = Provider(services => {
            services.AddSingleton(Metadata());
            services.AddSingleton<ITwoKeyEncryptionService>(_ => throw new InvalidOperationException("unkeyed"));
            services.AddFileStorageFactory(stores => {
                stores.AddAzureBlob(
                    "plain", options => {
                        options.ConnectionString = ConnectionString;
                        options.ContainerName = "plain";
                    });

                stores.AddAzureBlob(
                    "left", builder => builder.WithMultipart()
                        .Configure(options => {
                            options.ConnectionString = ConnectionString;
                            options.ContainerName = "left";
                        }));

                stores.AddAzureBlob(
                    "right", builder => builder.Configure(options => {
                            options.ConnectionString = ConnectionString;
                            options.ContainerName = "right";
                        })
                        .WithMultipart());
            });
        });

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();
        var plain = factory.Get("plain");
        var left = factory.Get("left");
        var right = factory.Get("right");
        Assert.Equal("plain", Options(plain).ContainerName);
        Assert.Equal("left", Options(left).ContainerName);
        Assert.Equal("right", Options(right).ContainerName);
        Assert.Null(Field(plain, "TwoKeyEncryptionService"));
        Assert.Null(provider.GetService<AzureBlobFileStorageOptions>());
        Assert.Null(scope.ServiceProvider.GetKeyedService<IMultipartUploadService>("plain"));
        var leftMultipart = scope.ServiceProvider.GetRequiredKeyedService<IMultipartUploadService>("left");
        var rightMultipart = scope.ServiceProvider.GetRequiredKeyedService<IMultipartUploadService>("right");
        Assert.NotSame(leftMultipart, rightMultipart);
        Assert.Same(left, Field(leftMultipart, "_storage"));
        Assert.Equal("left", ((AzureBlobFileStorageOptions)Field(leftMultipart, "_options")!).ContainerName);
        Assert.Equal("right", ((AzureBlobFileStorageOptions)Field(rightMultipart, "_options")!).ContainerName);
    }

    [Fact]
    public void AddAzureBlobFileStorageProvider_RegistersNoStore()
    {
        var services = new ServiceCollection();
        services.AddAzureBlobFileStorageProvider();
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFileStorageStoreProvider));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFileStorageService));
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static IFileMetadataStore Metadata() => DispatchProxy.Create<IFileMetadataStore, IdleProxy>();

    private static AzureBlobFileStorageOptions Options(IFileStorageService service) => (AzureBlobFileStorageOptions)Field(service, "Options")!;

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