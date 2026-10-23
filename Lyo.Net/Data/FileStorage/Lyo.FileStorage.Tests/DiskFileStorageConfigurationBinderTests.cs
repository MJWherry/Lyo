using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.Tests;

public sealed class DiskFileStorageConfigurationBinderTests
{
    [Fact]
    public void Bind_LocalFileStorageServiceSection_DoesNotSetRoot()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["LocalFileStorageService:RootDirectoryPath"] = "/legacy/root" })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        using var provider = services.BuildServiceProvider();
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.DiskFileStorageConfigurationBinder.BindDiskFileStorage(provider, null));
        Assert.Equal("RootDirectoryPath", ex.ParamName);
    }
}