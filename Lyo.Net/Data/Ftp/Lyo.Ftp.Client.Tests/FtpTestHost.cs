using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Lyo.Testing;
using Lyo.Testing.Containers;

namespace Lyo.Ftp.Client.Tests;

internal static class FtpTestHost
{
    private static int _passiveBase = 21000;

    public static async Task<(IContainer Container, FtpClientOptions Options)?> TryStartAsync(CancellationToken ct, int maxPooledClients = 4)
    {
        // Unique host-bound PASV range per container so parallel integration tests do not overlap.
        var passiveMin = Interlocked.Add(ref _passiveBase, 20) - 20;
        var passiveMax = passiveMin + 10;
        var container = await ContainerTestHost.TryStartAsync(
            "delfer/alpine-ftp-server", builder => {
                builder = builder.WithPortBinding(21, true)
                    .WithEnvironment("USERS", "foo|pass|/ftp/foo")
                    .WithEnvironment("MIN_PORT", passiveMin.ToString())
                    .WithEnvironment("MAX_PORT", passiveMax.ToString())
                    .WithEnvironment("ADDRESS", "127.0.0.1")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(21));

                for (var p = passiveMin; p <= passiveMax; p++)
                    builder = builder.WithPortBinding(p, p);

                return builder;
            }, ct);

        if (container is null)
            return null;

        var port = container.GetMappedPublicPort(21);
        // Internal port-21 wait can fire before Docker publishes the host mapping, so poll the mapped port.
        try {
            await PollAssert.NoExceptionAsync(
                async () => {
                    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port), ct);
                }, TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(200));
        }
        catch {
            await container.DisposeAsync();
            throw;
        }

        var options = new FtpClientOptions {
            Host = "127.0.0.1",
            Port = port,
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