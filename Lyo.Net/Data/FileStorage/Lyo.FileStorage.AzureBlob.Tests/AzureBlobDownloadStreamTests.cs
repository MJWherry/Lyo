namespace Lyo.FileStorage.AzureBlob.Tests;

/// <summary>Confirms the response-disposing read stream forwards reads and disposes the owner once on both sync and async disposal.</summary>
public sealed class AzureBlobDownloadStreamTests
{
    [Fact]
    public void Dispose_DisposesBackingStreamAndOwner_Once()
    {
        var inner = new MemoryStream([1, 2, 3, 4], writable: false);
        var owner = new TrackingDisposable();
        var wrapper = new AzureBlobDownloadStream(inner, owner);
        var buffer = new byte[4];
        var read = wrapper.Read(buffer, 0, buffer.Length);
        Assert.Equal(4, read);
        Assert.Equal([1, 2, 3, 4], buffer);
        wrapper.Dispose();
        Assert.True(owner.Disposed);
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
        wrapper.Dispose();
        Assert.Equal(1, owner.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_DisposesBackingStreamAndOwner()
    {
        var inner = new MemoryStream([7, 8, 9], writable: false);
        var owner = new TrackingDisposable();
        var wrapper = new AzureBlobDownloadStream(inner, owner);
        var buffer = new byte[3];
        _ = await wrapper.ReadAsync(buffer, TestContext.Current.CancellationToken);
        Assert.Equal([7, 8, 9], buffer);
        await wrapper.DisposeAsync();
        Assert.True(owner.Disposed);
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
        await wrapper.DisposeAsync();
        Assert.Equal(1, owner.DisposeCount);
    }

    [Fact]
    public void Write_OnReadOnlyWrapper_Throws()
    {
        using var inner = new MemoryStream([0], writable: false);
        using var wrapper = new AzureBlobDownloadStream(inner, new TrackingDisposable());
        Assert.Throws<NotSupportedException>(() => wrapper.Write([1, 2], 0, 2));
        Assert.Throws<NotSupportedException>(() => wrapper.SetLength(10));
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public bool Disposed => DisposeCount > 0;

        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }
}
