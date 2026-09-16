using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresUserClaimStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresUserClaimStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_List_Delete_RoundTrip()
    {
        var user = await CreateUserAsync();
        var claim = new LyoUserClaim(LyoGuid.CreateV7(), user.Id, "department", "engineering", DateTime.UtcNow, null);
        await _fixture.ClaimStore.CreateAsync(claim, null, TestCt);
        var listed = await _fixture.ClaimStore.ListForUserAsync(user.Id, null, TestCt);
        Assert.Contains(listed, c => c.Id == claim.Id && c.Type == "department" && c.Value == "engineering");
        await _fixture.ClaimStore.DeleteAsync(claim.Id, null, TestCt);
        var after = await _fixture.ClaimStore.ListForUserAsync(user.Id, null, TestCt);
        Assert.DoesNotContain(after, c => c.Id == claim.Id);
    }

    [Fact]
    public async Task Delete_Missing_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => _fixture.ClaimStore.DeleteAsync(LyoGuid.CreateV7(), null, TestCt));

    [Fact]
    public async Task ListForUser_DoesNotLeakOtherUsers()
    {
        var owner = await CreateUserAsync();
        var other = await CreateUserAsync();
        await _fixture.ClaimStore.CreateAsync(new(LyoGuid.CreateV7(), owner.Id, "role", "admin", DateTime.UtcNow, null), null, TestCt);
        await _fixture.ClaimStore.CreateAsync(new(LyoGuid.CreateV7(), other.Id, "role", "guest", DateTime.UtcNow, null), null, TestCt);
        var listed = await _fixture.ClaimStore.ListForUserAsync(owner.Id, null, TestCt);
        Assert.All(listed, c => Assert.Equal(owner.Id, c.UserId));
    }

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(LyoGuid.CreateV7(), "Claim Owner", $"claim-{LyoGuid.CreateV7():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await _fixture.UserStore.CreateAsync(user, null, TestCt);
    }
}
