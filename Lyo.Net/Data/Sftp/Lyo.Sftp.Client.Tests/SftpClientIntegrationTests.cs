namespace Lyo.Sftp.Client.Tests;

public class SftpClientIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task UploadDownloadAsync_RoundTrip()
    {
        var host = await SftpTestHost.TryStartAsync(TestCt);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new SftpClient(host.Value.Options);
        await client.HealthPingAsync(TestCt);
        var remote = "roundtrip-async.txt";
        var payload = "hello-sftp-async"u8.ToArray();
        await client.UploadAsync(remote, payload, TestCt);
        Assert.True(await client.FileExistsAsync(remote, TestCt));
        Assert.Equal(payload, await client.DownloadBytesAsync(remote, TestCt));
        await client.DeleteFileAsync(remote, TestCt);
        Assert.False(await client.FileExistsAsync(remote, TestCt));
    }
}