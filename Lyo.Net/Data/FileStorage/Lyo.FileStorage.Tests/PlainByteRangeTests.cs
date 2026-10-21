using Lyo.FileStorage.Tests.Support;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

public sealed class PlainByteRangeTests
{
    [Fact]
    public async Task OpenPlainByteRangeAsync_PlainFile_ReturnsSliceAtOffset()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var payload = TestData.Create(32);
        var saved = await scope.Storage.SaveFileAsync(payload, "clip.mp4", ct: TestCt);
        await using var slice = await scope.Storage.OpenPlainByteRangeAsync(saved.Id, saved.PathPrefix, 4, 7, TestCt);
        using var buffered = new MemoryStream();
        await slice!.CopyToAsync(buffered, TestCt);
        Assert.Equal(payload[4..8], buffered.ToArray());
    }
}