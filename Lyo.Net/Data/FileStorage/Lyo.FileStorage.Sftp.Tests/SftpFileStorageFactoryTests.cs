using System.Reflection;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.Sftp.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.Sftp.Tests;

public sealed class SftpFileStorageFactoryTests
{
    [Fact]
    public void AddSftp_TwoHosts_BuildsDistinctStoresWithoutUnkeyedClients()
    {
        using var provider = Provider(services => {
            services.AddSingleton(Metadata());
            services.AddSingleton<ITwoKeyEncryptionService>(_ => throw new InvalidOperationException("unkeyed"));
            services.AddFileStorageFactory(stores => {
                stores.AddSftp("east", options => {
                    options.Sftp.Host = "east.example";
                    options.Sftp.Username = "u";
                    options.Sftp.Password = "p";
                    options.Sftp.RootRemoteDirectory = "/east";
                    options.Sftp.HostKeyPolicy = SftpHostKeyPolicy.AcceptAny;
                });
                stores.AddSftp("west", builder => builder.Configure(options => {
                    options.Sftp.Host = "west.example";
                    options.Sftp.Username = "u";
                    options.Sftp.Password = "p";
                    options.Sftp.RootRemoteDirectory = "/west";
                    options.Sftp.HostKeyPolicy = SftpHostKeyPolicy.AcceptAny;
                }));
            });
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();
        var east = factory.Get("east");
        var west = factory.Get("west");

        Assert.Equal("east.example", Options(east).Sftp.Host);
        Assert.Equal("west.example", Options(west).Sftp.Host);
        Assert.Null(Field(east, "TwoKeyEncryptionService"));
        Assert.Null(provider.GetService<SftpFileStorageOptions>());
        Assert.Null(provider.GetService<ISftpClient>());
        Assert.Null(provider.GetService<SftpClientOptions>());
    }

    [Fact]
    public void AddSftpFileStorageProvider_RegistersNoStore()
    {
        var services = new ServiceCollection();
        services.AddSftpFileStorageProvider();
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFileStorageStoreProvider));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISftpClient));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFileStorageService));
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static IFileMetadataStore Metadata() => DispatchProxy.Create<IFileMetadataStore, IdleProxy>();

    private static SftpFileStorageOptions Options(IFileStorageService service) => (SftpFileStorageOptions)Field(service, "Options")!;

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
