using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authorization.Postgres.Tests;

public sealed class PostgresGrantStoreTests(AuthorizationPostgresFixture fixture) : IClassFixture<AuthorizationPostgresFixture>
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Wildcard_Expiry_Supersede_Regrant_Concurrent_Guid_AndHealth()
    {
        var store = fixture.Store;
        var subject = "user-" + Guid.NewGuid().ToString("N");
        await store.SaveAsync(Write(subject, resourceId: null), TestCt);
        Assert.True(await store.IsAllowedAsync(Check(subject, "n1"), TestCt));
        Assert.False(await store.IsAllowedAsync(new GrantCheck { Subject = subject, Action = "write", ResourceType = "note", ResourceId = null }, TestCt));

        var expiring = Clock();
        var expiredSubject = "exp-" + Guid.NewGuid().ToString("N");
        var old = await expiring.SaveAsync(Write(expiredSubject, expiresAt: Now), TestCt);
        Assert.False(await expiring.IsAllowedAsync(Check(expiredSubject, "n1"), TestCt));
        var replaced = await expiring.SaveAsync(Write(expiredSubject), TestCt);
        Assert.NotEqual(old.Id, replaced.Id);
        Assert.True(await expiring.IsAllowedAsync(Check(expiredSubject, "n1"), TestCt));

        await expiring.RevokeAsync(replaced.Id, TestCt);
        var again = await expiring.SaveAsync(Write(expiredSubject), TestCt);
        Assert.True(await expiring.IsAllowedAsync(Check(expiredSubject, "n1"), TestCt));
        Assert.NotEqual(replaced.Id, again.Id);

        var raceSubject = "race-" + Guid.NewGuid().ToString("N");
        var races = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => CatchAsync(() => store.SaveAsync(Write(raceSubject), TestCt))));
        Assert.Single(races, g => g is not null);
        Assert.Contains(races, g => g is null);

        var guid = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
        var guidSubject = "guid-" + Guid.NewGuid().ToString("N");
        var saved = await store.SaveAsync(Write(guidSubject, resourceId: guid.ToString("D").ToUpperInvariant()), TestCt);
        Assert.Equal(guid.ToString("D"), saved.ResourceId);
        Assert.True(await store.IsAllowedAsync(Check(guidSubject, guid.ToString("D")), TestCt));

        var health = await store.CheckHealthAsync(TestCt);
        Assert.True(health.IsHealthy);
        Assert.Equal("authorization.grants", store.HealthCheckName);
    }

    private PostgresGrantStore Clock()
        => new(fixture.ContextFactory, () => Now);

    private static GrantWrite Write(string subject, string? resourceId = "n1", DateTime? expiresAt = null)
        => new() { Subject = subject, Action = "read", ResourceType = "note", ResourceId = resourceId, ExpiresAt = expiresAt };

    private static GrantCheck Check(string subject, string? resourceId)
        => new() { Subject = subject, Action = "read", ResourceType = "note", ResourceId = resourceId };

    private static async Task<Grant?> CatchAsync(Func<Task<Grant>> call)
    {
        try {
            return await call();
        }
        catch (ConflictException) {
            return null;
        }
    }
}
