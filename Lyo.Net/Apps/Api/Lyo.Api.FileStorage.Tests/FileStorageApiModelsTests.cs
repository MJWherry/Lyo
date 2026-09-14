using System.Text.Json;
using Lyo.Api.FileStorage.Models;

namespace Lyo.Api.FileStorage.Tests;

public sealed class FileStorageApiModelsTests
{
    [Fact]
    public void CopyFileRequest_RoundTrip_KeepsPathPrefix()
    {
        var original = new CopyFileRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"), "archive");
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CopyFileRequest>(json);
        Assert.NotNull(restored);
        Assert.Equal(original.SourceFileId, restored.SourceFileId);
        Assert.Equal("archive", restored.PathPrefix);
    }

    [Fact]
    public void MultipartBeginResponse_DeserializesStringProviderKind()
    {
        var json =
            """{"sessionId":"11111111-1111-1111-1111-111111111111","targetFileId":"22222222-2222-2222-2222-222222222222","partSizeBytes":8388608,"expiresUtc":"2026-01-01T00:00:00Z","providerKind":"awsS3"}""";
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var restored = JsonSerializer.Deserialize<MultipartBeginResponse>(json, options);
        Assert.NotNull(restored);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), restored.SessionId);
        Assert.Equal("awsS3", restored.ProviderKind);
    }

    [Fact]
    public void FileStoragePresence_IntegersMatchLibraryContract()
    {
        Assert.Equal(0, (int)FileStoragePresence.None);
        Assert.Equal(1, (int)FileStoragePresence.Store);
        Assert.Equal(2, (int)FileStoragePresence.Physical);
        Assert.Equal(3, (int)FileStoragePresence.Both);
        var names = Enum.GetNames<FileStoragePresence>();
        Assert.DoesNotContain(names, n => n.Contains("Blob", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Cloud", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FolderListResponse_RoundTrip_KeepsPresence()
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var original = new FileStorageFolderListResponse(
            [new("keep.txt", false, "reports", fileId, FileStoragePresence.Both, "keep.txt", 4, "reports/" + fileId.ToString("N"))], false);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<FileStorageFolderListResponse>(json);
        Assert.NotNull(restored);
        Assert.False(restored.Truncated);
        var row = Assert.Single(restored.Entries);
        Assert.Equal(FileStoragePresence.Both, row.Presence);
        Assert.Equal(fileId, row.FileId);
        Assert.DoesNotContain("CloudOnly", json, StringComparison.Ordinal);
        Assert.DoesNotContain("MissingBlob", json, StringComparison.Ordinal);
    }
}
