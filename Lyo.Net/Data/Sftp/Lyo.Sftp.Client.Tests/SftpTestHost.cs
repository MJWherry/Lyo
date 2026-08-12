using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Lyo.Sftp.Client.Tests;

internal static class SftpTestHost
{
    public static async Task<(IContainer Container, SftpClientOptions Options)?> TryStartAsync(
        CancellationToken ct,
        int maxPooledClients = 4)
    {
        var container = new ContainerBuilder("atmoz/sftp:alpine")
            .WithPortBinding(22, true)
            .WithCommand("foo:pass:1001:100:upload")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server listening"))
            .Build();

        try {
            await container.StartAsync(ct);
        }
        catch (Exception) {
            await container.DisposeAsync();
            return null;
        }

        await Task.Delay(2000, ct);
        var options = new SftpClientOptions {
            Host = container.Hostname,
            Port = container.GetMappedPublicPort(22),
            Username = "foo",
            Password = "pass",
            RootRemoteDirectory = "/upload",
            HostKeyPolicy = SftpHostKeyPolicy.AcceptAny,
            MaxPooledClients = maxPooledClients,
            EnableMetrics = false
        };
        return (container, options);
    }
}
