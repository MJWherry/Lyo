using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Ftp.Client.Tests;

public class FtpClientDiTests
{
    [Fact]
    public void AddFtpClient_RegistersSingleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetrics>(NullMetrics.Instance);
        services.AddFtpClient(o => {
            o.Host = "localhost";
            o.Username = "foo";
            o.Password = "pass";
            o.RootRemoteDirectory = "/upload";
            o.EnableMetrics = false;
        });

        using var sp = services.BuildServiceProvider();
        var a = sp.GetRequiredService<IFtpClient>();
        var b = sp.GetRequiredService<IFtpClient>();
        Assert.Same(a, b);
        Assert.Equal("/upload", a.RootRemoteDirectory);
    }
}
