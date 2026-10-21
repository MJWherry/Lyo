using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Lyo.Testing.Containers;

namespace Lyo.FileStorage.AzureBlob.Tests;

/// <summary>Starts Azurite, the Azure Storage emulator, for the Blob integration suites. Returns null when Docker is unavailable so those suites skip cleanly.</summary>
internal static class AzuriteTestHost
{
    private const int BlobPort = 10000;

    /// <summary>Well-known Azurite development credentials. These are published by Microsoft and are not a secret.</summary>
    private const string AccountName = "devstoreaccount1";

    private const string AccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    public static async Task<(IContainer Container, string ConnectionString)?> TryStartAsync(CancellationToken ct)
    {
        var container = await ContainerTestHost.TryStartAsync(
            "mcr.microsoft.com/azure-storage/azurite:latest",
            builder => builder.WithPortBinding(BlobPort, true)
                .WithCommand("azurite-blob", "--blobHost", "0.0.0.0", "--skipApiVersionCheck")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(BlobPort)), ct);

        if (container is null)
            return null;

        var endpoint = $"http://{container.Hostname}:{container.GetMappedPublicPort(BlobPort)}/{AccountName}";
        var connectionString = $"DefaultEndpointsProtocol=http;AccountName={AccountName};AccountKey={AccountKey};BlobEndpoint={endpoint};";
        return (container, connectionString);
    }
}