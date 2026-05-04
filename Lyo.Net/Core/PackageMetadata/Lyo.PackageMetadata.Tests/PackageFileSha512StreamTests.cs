namespace Lyo.PackageMetadata.Tests;

public sealed class PackageFileSha512StreamTests
{
    [Fact]
    public void ComputeHex_stream_leave_open_leaves_position_at_end_matches_bytes_path()
    {
        var payload = "nupkg-bytes-parity"u8.ToArray();
        var bytesHex = PackageFileSha512.ComputeHex(payload);
        using var ms = new MemoryStream(payload);
        var streamHex = PackageFileSha512.ComputeHex(ms, leaveOpen: true);
        Assert.Equal(bytesHex, streamHex);
        Assert.Equal(ms.Length, ms.Position);
    }
}
