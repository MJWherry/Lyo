using Lyo.Common.Pathing;
using Lyo.Ftp.Client;
using Lyo.IO.Temp.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Ftp.Tests;

public class FtpIOTempStorageProviderTests
{
    [Fact]
    public void PathStyle_IsPosix()
    {
        using var client = new FtpClient(new FtpClientOptions {
            Host = "localhost",
            Username = "u",
            Password = "p",
            RootRemoteDirectory = "/upload",
            EncryptionMode = FtpEncryptionMode.None
        });
        var provider = new FtpIOTempStorageProvider(client);
        Assert.Equal(PathStyle.Posix, provider.PathStyle);
        Assert.Equal("/upload", provider.RootPath);
    }

    [Fact]
    public void AddIOTempFtpStorageProvider_RegistersProvider()
    {
        var services = new ServiceCollection();
        services.AddIOTempFtpStorageProvider(o => {
            o.Host = "localhost";
            o.Username = "u";
            o.Password = "p";
            o.RootRemoteDirectory = "/upload";
            o.EnableMetrics = false;
        });

        using var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IIOTempStorageProvider>();
        Assert.IsType<FtpIOTempStorageProvider>(provider);
        Assert.Equal(PathStyle.Posix, provider.PathStyle);
    }
}
