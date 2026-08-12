using Lyo.Common.Pathing;
using Lyo.IO.Temp.Storage;
using Lyo.Sftp.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Sftp.Tests;

public class SftpIOTempStorageProviderTests
{
    [Fact]
    public void PathStyle_IsPosix()
    {
        using var client = new SftpClient(
            new() {
                Host = "localhost",
                Username = "u",
                Password = "p",
                RootRemoteDirectory = "/upload",
                HostKeyPolicy = SftpHostKeyPolicy.AcceptAny
            });

        var provider = new SftpIOTempStorageProvider(client);
        Assert.Equal(PathStyle.Posix, provider.PathStyle);
        Assert.Equal("/upload", provider.RootPath);
    }

    [Fact]
    public void AddIOTempSftpStorageProvider_RegistersProvider()
    {
        var services = new ServiceCollection();
        services.AddIOTempSftpStorageProvider(o => {
            o.Host = "localhost";
            o.Username = "u";
            o.Password = "p";
            o.RootRemoteDirectory = "/upload";
            o.HostKeyPolicy = SftpHostKeyPolicy.AcceptAny;
            o.EnableMetrics = false;
        });

        using var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IIOTempStorageProvider>();
        Assert.IsType<SftpIOTempStorageProvider>(provider);
        Assert.Equal(PathStyle.Posix, provider.PathStyle);
    }
}