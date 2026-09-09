using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Sftp.Client.Tests;

public class SftpClientDiTests
{
    [Fact]
    public void AddSftpClient_RegistersSingleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetrics>(NullMetrics.Instance);
        services.AddSftpClient(o => {
            o.Host = "localhost";
            o.Username = "foo";
            o.Password = "pass";
            o.HostKeyPolicy = SftpHostKeyPolicy.AcceptAny;
            o.RootRemoteDirectory = "/upload";
            o.EnableMetrics = false;
        });

        using var sp = services.BuildServiceProvider();
        var a = sp.GetRequiredService<ISftpClient>();
        var b = sp.GetRequiredService<ISftpClient>();
        Assert.Same(a, b);
        Assert.Equal("/upload", a.RootRemoteDirectory);
    }
}