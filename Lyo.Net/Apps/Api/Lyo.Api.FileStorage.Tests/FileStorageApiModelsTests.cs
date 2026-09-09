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
}
