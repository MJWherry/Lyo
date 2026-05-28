using Lyo.EntityReference.Models;

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
        var change = new AuditChange(
            EntityRef.ForKey("Test.MyEntity", "1"), new Dictionary<string, object?> { ["Name"] = "old" }, new Dictionary<string, object?> { ["Name"] = "new" });

        recorder.RecordChange(change);
    }

    [Fact]
    public void RecordEvent_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var evt = new AuditEvent(EntityRef.ForKey("User", "u1"), "UserLogin", "User signed in", EntityRef.ForKey("User", "u1"));
        recorder.RecordEvent(evt);
    }

    [Fact]
    public void RecordChanges_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var changes = new[] {
            new AuditChange(EntityRef.ForKey("T1", "1"), new Dictionary<string, object?>(), new Dictionary<string, object?>()),
            new AuditChange(EntityRef.ForKey("T2", "2"), new Dictionary<string, object?>(), new Dictionary<string, object?>())
        };

        recorder.RecordChanges(changes);
    }

    [Fact]
    public void RecordEvents_DoesNotThrow()
    {
        var recorder = NullAuditRecorder.Instance;
        var events = new[] { new AuditEvent(EntityRef.ForKey("E", "1"), "E1"), new AuditEvent(EntityRef.ForKey("E", "2"), "E2") };
        recorder.RecordEvents(events);
    }
}