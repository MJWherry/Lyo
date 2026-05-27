using Lyo.EntityReference.Models;

namespace Lyo.Audit.Tests;

public class AuditEventRecordTests
{
    [Fact]
    public void AuditEvent_WithDefaults_HasUtcNowApproximate()
    {
        var before = DateTime.UtcNow;
        var subject = EntityRef.ForKey("User", "u1");
        var evt = new AuditEvent(subject, "Test");
        var after = DateTime.UtcNow;
        Assert.Equal("Test", evt.EventType);
        Assert.Equal(subject, evt.Subject);
        Assert.True(evt.Timestamp >= before.AddSeconds(-1) && evt.Timestamp <= after.AddSeconds(1));
        Assert.Null(evt.Message);
        Assert.Null(evt.Actor);
        Assert.Null(evt.Metadata);
    }

    [Fact]
    public void AuditEvent_WithExpression_CreatesCopyWithNewValues()
    {
        var subject = EntityRef.ForKey("User", "u1");
        var actor = EntityRef.ForKey("User", "u1");
        var evt = new AuditEvent(subject, "Login", "Signed in", actor);
        var updated = evt with { EventType = "Logout", Message = "Signed out" };
        Assert.Equal("Logout", updated.EventType);
        Assert.Equal("Signed out", updated.Message);
        Assert.Equal(actor, updated.Actor);
    }

    [Fact]
    public void AuditEvent_ValueEquality_ComparesByValues()
    {
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var subject = EntityRef.ForKey("E", "1");
        var actor = EntityRef.ForKey("User", "a");
        var a = new AuditEvent(subject, "E", null, actor) { Id = id, Timestamp = timestamp };
        var b = new AuditEvent(subject, "E", null, actor) { Id = id, Timestamp = timestamp };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
