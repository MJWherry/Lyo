namespace Lyo.Ftp.Client.Tests;

public class FtpClientLeaseTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CancelWhileWaitingForLease_ThrowsAndDoesNotLeakPermit()
    {
        var host = await FtpTestHost.TryStartAsync(TestCt, 1);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new FtpClient(host.Value.Options);
        await client.HealthPingAsync(TestCt);
        await client.UploadAsync("held.txt", "x"u8.ToArray(), TestCt);
        var held = await client.OpenReadAsync("held.txt", TestCt);
        try {
            using var cts = new CancellationTokenSource();
            var blocked = client.ExistsAsync("held.txt", cts.Token);
            await Task.Delay(400, TestCt);
            Assert.False(blocked.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blocked);
        }
        finally {
            await held.DisposeAsync();
        }

        Assert.True(await client.ExistsAsync("held.txt", TestCt));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConcurrentAsyncOps_RespectMaxPooledClients()
    {
        var host = await FtpTestHost.TryStartAsync(TestCt, 2);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new FtpClient(host.Value.Options);
        await client.HealthPingAsync(TestCt);
        await client.UploadAsync("a.txt", "a"u8.ToArray(), TestCt);
        await client.UploadAsync("b.txt", "b"u8.ToArray(), TestCt);
        await using var s1 = await client.OpenReadAsync("a.txt", TestCt);
        await using var s2 = await client.OpenReadAsync("b.txt", TestCt);
        var third = client.ExistsAsync("a.txt", TestCt);
        await Task.Delay(500, TestCt);
        Assert.False(third.IsCompleted);
        await s1.DisposeAsync();
        Assert.True(await third);
        await s2.DisposeAsync();
    }
}