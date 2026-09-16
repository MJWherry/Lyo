using Azure.Storage.Blobs;
using DotNet.Testcontainers.Containers;
using Lyo.Common.Core.Identifiers;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Tests;

namespace Lyo.FileStorage.AzureBlob.Tests;

/// <summary>
/// Runs the shared <see cref="IFileSystem" /> contract against <see cref="AzureBlobFileSystem" /> on Azurite. One emulator is shared by the whole class and each test gets
/// its own container, so the cases stay isolated without paying for an emulator apiece.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AzureBlobFileSystemContractTests(AzuriteFixture fixture) : IFileSystemContractTests, IClassFixture<AzuriteFixture>
{
    protected override async ValueTask<IFileSystem?> CreateAsync()
    {
        if (fixture.ConnectionString is not { } connectionString)
            return null;

        var container = new BlobContainerClient(connectionString, $"c-{LyoGuid.CreateV7():N}");
        await container.CreateIfNotExistsAsync(cancellationToken: TestContext.Current.CancellationToken);
        return new AzureBlobFileSystem(container, "app-data");
    }
}

/// <summary>Starts one Azurite emulator for the contract suite.</summary>
public sealed class AzuriteFixture : IAsyncLifetime
{
    private IContainer? _container;

    /// <summary>Blob connection string for the running emulator, or null when Docker is unavailable.</summary>
    public string? ConnectionString { get; private set; }

    public async ValueTask InitializeAsync()
    {
        if (await AzuriteTestHost.TryStartAsync(TestContext.Current.CancellationToken) is not { } host)
            return;

        (_container, ConnectionString) = host;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
