using System.Reflection;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.Ftp.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.Ftp.Tests;

public sealed class FtpFileStorageFactoryTests
{
    [Fact]
    public void AddFtp_TwoHosts_BuildsDistinctStoresWithoutUnkeyedClients()
    {
        using var provider = Provider(services => {
            services.AddSingleton(Metadata());
            services.AddSingleton<ITwoKeyEncryptionService>(_ => throw new InvalidOperationException("unkeyed"));
            services.AddFileStorageFactory(stores => {
                stores.AddFtp("east", options => {
                    options.Ftp.Host = "east.example";
                    options.Ftp.Username = "u";
                    options.Ftp.Password = "p";
                    options.Ftp.RootRemoteDirectory = "/east";
                });
                stores.AddFtp("west", builder => builder.Configure(options => {
                    options.Ftp.Host = "west.example";
                    options.Ftp.Username = "u";
                    options.Ftp.Password = "p";
                    options.Ftp.RootRemoteDirectory = "/west";
                }));
            });
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();
        var east = factory.Get("east");
        var west = factory.Get("west");

        Assert.Equal("east.example", Options(east).Ftp.Host);
        Assert.Equal("west.example", Options(west).Ftp.Host);
        Assert.Null(Field(east, "TwoKeyEncryptionService"));
        Assert.Null(provider.GetService<FtpFileStorageOptions>());
        Assert.Null(provider.GetService<IFtpClient>());
        Assert.Null(provider.GetService<FtpClientOptions>());
    }

    [Fact]
    public void AddFtpFileStorageProvider_RegistersNoStore()
    {
        var services = new ServiceCollection();
        services.AddFtpFileStorageProvider();
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFileStorageStoreProvider));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFtpClient));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFileStorageService));
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static IFileMetadataStore Metadata() => DispatchProxy.Create<IFileMetadataStore, IdleProxy>();

    private static FtpFileStorageOptions Options(IFileStorageService service) => (FtpFileStorageOptions)Field(service, "Options")!;

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
