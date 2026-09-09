using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresUserClaimStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresUserClaimStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_List_Delete_RoundTrip()
    {
        var user = await CreateUserAsync();
        var claim = new LyoUserClaim(Guid.NewGuid(), user.Id, "department", "engineering", DateTime.UtcNow, null);
        await _fixture.ClaimStore.CreateAsync(claim, null, TestContext.Current.CancellationToken);
        var listed = await _fixture.ClaimStore.ListForUserAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.Contains(listed, c => c.Id == claim.Id && c.Type == "department" && c.Value == "engineering");
        await _fixture.ClaimStore.DeleteAsync(claim.Id, null, TestContext.Current.CancellationToken);
        var after = await _fixture.ClaimStore.ListForUserAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(after, c => c.Id == claim.Id);
    }

    [Fact]
    public async Task Delete_Missing_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => _fixture.ClaimStore.DeleteAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken));

    [Fact]
    public async Task ListForUser_DoesNotLeakOtherUsers()
    {
        var owner = await CreateUserAsync();
        var other = await CreateUserAsync();
        await _fixture.ClaimStore.CreateAsync(new(Guid.NewGuid(), owner.Id, "role", "admin", DateTime.UtcNow, null), null, TestContext.Current.CancellationToken);
        await _fixture.ClaimStore.CreateAsync(new(Guid.NewGuid(), other.Id, "role", "guest", DateTime.UtcNow, null), null, TestContext.Current.CancellationToken);
        var listed = await _fixture.ClaimStore.ListForUserAsync(owner.Id, null, TestContext.Current.CancellationToken);
        Assert.All(listed, c => Assert.Equal(owner.Id, c.UserId));
    }

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(Guid.NewGuid(), "Claim Owner", $"claim-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
    }
}
