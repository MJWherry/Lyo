using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresUserScopeStoreTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresUserScopeStoreTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_List_Delete_RoundTrip()
    {
        var user = await CreateUserAsync();
        var scope = new LyoUserScope(Guid.NewGuid(), user.Id, "people.read", DateTime.UtcNow, null);
        await _fixture.ScopeStore.CreateAsync(scope, null, TestContext.Current.CancellationToken);
        var listed = await _fixture.ScopeStore.ListForUserAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.Contains(listed, s => s.Id == scope.Id && s.Name == "people.read");
        await _fixture.ScopeStore.DeleteAsync(scope.Id, null, TestContext.Current.CancellationToken);
        var after = await _fixture.ScopeStore.ListForUserAsync(user.Id, null, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(after, s => s.Id == scope.Id);
    }

    [Fact]
    public async Task Create_DuplicateName_Throws()
    {
        var user = await CreateUserAsync();
        await _fixture.ScopeStore.CreateAsync(new(Guid.NewGuid(), user.Id, "people.read", DateTime.UtcNow, null), null, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ConflictException>(() =>
            _fixture.ScopeStore.CreateAsync(new(Guid.NewGuid(), user.Id, "people.read", DateTime.UtcNow, null), null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_Missing_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => _fixture.ScopeStore.DeleteAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken));

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(Guid.NewGuid(), "Scope Owner", $"scope-{Guid.NewGuid():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);
        return await _fixture.UserStore.CreateAsync(user, null, TestContext.Current.CancellationToken);
    }
}
