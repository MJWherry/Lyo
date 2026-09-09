using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

public class FileAuditHandlerTests
{
    [Fact]
    public async Task SaveFileAsync_WithAuditHandler_AppendsSaveEvent()
    {
        var sink = new CaptureAuditHandler();
        using var scope = LocalFileStorageTestScope.Create(o => o, new[] { sink });
        var data = "audit-me"u8.ToArray();
        await scope.Storage.SaveFileAsync(data, "a.txt", ct: TestContext.Current.CancellationToken);
        Assert.Contains(sink.Events, e => e.EventType == FileAuditEventType.Save && e.Outcome == FileAuditOutcome.Success);
    }
}
