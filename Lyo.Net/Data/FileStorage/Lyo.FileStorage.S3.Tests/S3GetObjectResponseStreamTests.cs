using Amazon.S3.Model;
using Lyo.FileStorage.S3;

namespace Lyo.FileStorage.S3.Tests;

/// <summary>Confirms the response-disposing read stream forwards reads and disposes the underlying <see cref="GetObjectResponse" /> exactly once on both sync and async disposal.</summary>
public sealed class S3GetObjectResponseStreamTests
{
    [Fact]
    public void Dispose_DisposesBackingStreamAndResponse_Once()
    {
        var (inner, response) = CreatePayload([1, 2, 3, 4]);
        var wrapper = new S3GetObjectResponseStream(response);

        var buffer = new byte[4];
        var read = wrapper.Read(buffer, 0, buffer.Length);

        Assert.Equal(4, read);
        Assert.Equal([1, 2, 3, 4], buffer);

        wrapper.Dispose();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());

        // Second dispose must be a no-op (no double-dispose throw from base stream).
        wrapper.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_DisposesBackingStreamAndResponse()
    {
        var (inner, response) = CreatePayload([7, 8, 9]);
        var wrapper = new S3GetObjectResponseStream(response);

        var buffer = new byte[3];
        await wrapper.ReadAsync(buffer);

        Assert.Equal([7, 8, 9], buffer);

        await wrapper.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());

        await wrapper.DisposeAsync();
    }

    [Fact]
    public void Constructor_NullResponseStream_Throws()
    {
        using var response = new GetObjectResponse();
        Assert.Throws<InvalidOperationException>(() => new S3GetObjectResponseStream(response));
    }

    [Fact]
    public void Write_OnReadOnlyWrapper_Throws()
    {
        var (_, response) = CreatePayload([0]);
        using var wrapper = new S3GetObjectResponseStream(response);
        Assert.Throws<NotSupportedException>(() => wrapper.Write([1, 2], 0, 2));
        Assert.Throws<NotSupportedException>(() => wrapper.SetLength(10));
    }

    private static (MemoryStream Inner, GetObjectResponse Response) CreatePayload(byte[] payload)
    {
        var inner = new MemoryStream(payload, writable: false);
        var response = new GetObjectResponse { ResponseStream = inner };
        return (inner, response);
    }
}
