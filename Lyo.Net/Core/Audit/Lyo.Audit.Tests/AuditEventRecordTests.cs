namespace Lyo.Audit.Tests;

public class AuditEventRecordTests
{
    [Fact]
    public void AuditEvent_WithDefaults_HasUtcNowApproximate()
    {
        var before = DateTime.UtcNow;
        var evt = new AuditEvent("Test");
        var after = DateTime.UtcNow;
        Assert.Equal("Test", evt.EventType);
        Assert.True(evt.Timestamp >= before.AddSeconds(-1) && evt.Timestamp <= after.AddSeconds(1));
        Assert.Null(evt.Message);
        Assert.Null(evt.Actor);
        Assert.Null(evt.Metadata);
    }

    [Fact]
    public void AuditEvent_WithExpression_CreatesCopyWithNewValues()
    {
        var evt = new AuditEvent("Login", "Signed in", "user-1");
        var updated = evt with { EventType = "Logout", Message = "Signed out" };
        Assert.Equal("Logout", updated.EventType);
        Assert.Equal("Signed out", updated.Message);
        Assert.Equal("user-1", updated.Actor);
    }

    [Fact]
    public void AuditEvent_ValueEquality_ComparesByValues()
    {
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var a = new AuditEvent("E", null, "a") { Id = id, Timestamp = timestamp };
        var b = new AuditEvent("E", null, "a") { Id = id, Timestamp = timestamp };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}