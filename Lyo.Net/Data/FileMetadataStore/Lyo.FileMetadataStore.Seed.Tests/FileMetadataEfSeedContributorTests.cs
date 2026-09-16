using System.Text;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileMetadataStore.Seed;
using Lyo.Seed;

namespace Lyo.FileMetadataStore.Seed.Tests;

public sealed class FileMetadataEfSeedContributorTests
{
    [Fact]
    public async Task FileMetadataEfSeedContributor_Count_PersistsFileGraph()
    {
        var transport = await SeedAsync(new FileMetadataEfSeedContributor(), 2);
        var files = transport.Items.OfType<FileMetadataEntity>().ToList();
        Assert.Equal(2, files.Count);
        Assert.All(files, file => {
            var id = Guid.Parse(file.Id);
            var blob = Assert.Single(transport.Items.OfType<FileDataEntity>(), row => row.FileId == id);
            Assert.Equal(FileMetadataEfSeedContributor.BlobText, Encoding.UTF8.GetString(blob.Data));
            Assert.Contains(transport.Items.OfType<FileRevisionEntity>(), row => row.ParentId == id);
            Assert.Contains(transport.Items.OfType<FileAuditEventEntity>(), row => row.FileId == id);
            Assert.Contains(transport.Items.OfType<MultipartUploadSessionEntity>(), row => row.TargetFileId == id);
            var link = Assert.Single(transport.Items.OfType<FileDownloadAccessLinkEntity>(), row => row.FileId == id);
            Assert.NotEmpty(link.TokenHash);
        });
        var hashes = transport.Items.OfType<FileDownloadAccessLinkEntity>().Select(row => Convert.ToHexString(row.TokenHash)).ToList();
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
