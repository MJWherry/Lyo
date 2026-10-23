namespace Lyo.Authorization.Web.Components.Tests;

public class GrantFormTests
{
    [Fact]
    public void ToWrite_BlankResourceIdAndTenant_AreNull()
    {
        var write = Form("", "  ").ToWrite();
        Assert.Null(write.ResourceId);
        Assert.Null(write.TenantId);
    }

    [Fact]
    public void ToWrite_GuidResourceId_IsLowercaseD()
    {
        var write = Form("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE").ToWrite();
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", write.ResourceId);
    }

    [Fact]
    public void ToWrite_NonGuidResourceId_IsTrimmedAndNotLowercased()
    {
        var write = Form(" Note-1 ", subject: "User-A").ToWrite();
        Assert.Equal("Note-1", write.ResourceId);
        Assert.Equal("User-A", write.Subject);
    }

    [Fact]
    public void ToWrite_LocalExpiry_IsUtcWithoutShiftingClock()
    {
        var local = new DateTime(2026, 10, 14, 15, 30, 0, DateTimeKind.Local);
        var write = Form(expiresAt: local).ToWrite();
        Assert.Equal(DateTimeKind.Utc, write.ExpiresAt!.Value.Kind);
        Assert.Equal(2026, write.ExpiresAt.Value.Year);
        Assert.Equal(10, write.ExpiresAt.Value.Month);
        Assert.Equal(14, write.ExpiresAt.Value.Day);
        Assert.Equal(15, write.ExpiresAt.Value.Hour);
        Assert.Equal(30, write.ExpiresAt.Value.Minute);
    }

    [Fact]
    public void ToWrite_WhitespaceResourceId_Throws()
    {
        var form = Form("   ");
        Assert.Throws<ArgumentException>(() => form.ToWrite());
    }

    private static GrantForm Form(string? resourceId = null, string? tenantId = null, string subject = "user-a", DateTime? expiresAt = null)
        => new() {
            Subject = subject,
            Action = "read",
            ResourceType = "note",
            ResourceId = resourceId,
            TenantId = tenantId,
            ExpiresAt = expiresAt
        };
}