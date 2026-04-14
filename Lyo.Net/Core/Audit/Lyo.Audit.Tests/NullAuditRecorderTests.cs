namespace Lyo.Audit.Tests;

public class NullAuditRecorderTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        var a = NullAuditRecorder.Instance;
        var b = NullAuditRecorder.Instance;
        Assert.Same(a, b);
    }

    [Fact]
    public void RecordChange_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var change = new AuditChange("Test.MyEntity, Test", new Dictionary<string, object?> { ["Name"] = "old" }, new Dictionary<string, object?> { ["Name"] = "new" });
        recorder.RecordChange(change);
    }

    [Fact]
    public void RecordEvent_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var evt = new AuditEvent("UserLogin", "User signed in", "user-1");
        recorder.RecordEvent(evt);
    }

    [Fact]
    public void RecordChanges_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var changes = new[] {
            new AuditChange("T1", new Dictionary<string, object?>(), new Dictionary<string, object?>()),
            new AuditChange("T2", new Dictionary<string, object?>(), new Dictionary<string, object?>())
        };

        recorder.RecordChanges(changes);
    }

    [Fact]
    public void RecordEvents_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var events = new[] { new AuditEvent("E1"), new AuditEvent("E2") };
        recorder.RecordEvents(events);
    }
}