using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Lyo.Ftp.Client.Tests;

internal static class FtpTestHost
{
    private static int _passiveBase = 21000;

    public static async Task<(IContainer Container, FtpClientOptions Options)?> TryStartAsync(CancellationToken ct, int maxPooledClients = 4)
    {
        // Unique host-bound PASV range per container so parallel integration tests do not collide.
        var passiveMin = Interlocked.Add(ref _passiveBase, 20) - 20;
        var passiveMax = passiveMin + 10;
        var builder = new ContainerBuilder("delfer/alpine-ftp-server").WithPortBinding(21, true)
            .WithEnvironment("USERS", "foo|pass|/ftp/foo")
            .WithEnvironment("MIN_PORT", passiveMin.ToString())
            .WithEnvironment("MAX_PORT", passiveMax.ToString())
            .WithEnvironment("ADDRESS", "127.0.0.1")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(21));

        for (var p = passiveMin; p <= passiveMax; p++)
            builder = builder.WithPortBinding(p, p);

        var container = builder.Build();
        try {
            await container.StartAsync(ct);
        }
        catch (Exception) {
            await container.DisposeAsync();
            return null;
        }

        await Task.Delay(2000, ct);
        var options = new FtpClientOptions {
            Host = "127.0.0.1",
            Port = container.GetMappedPublicPort(21),
            Username = "foo",
            Password = "pass",
            RootRemoteDirectory = "/ftp/foo",
            EncryptionMode = FtpEncryptionMode.None,
            TlsPolicy = FtpTlsPolicy.AcceptAny,
            MaxPooledClients = maxPooledClients,
            PassivePortRange = (passiveMin, passiveMax),
            EnableMetrics = false
        };

        return (container, options);
    }
}