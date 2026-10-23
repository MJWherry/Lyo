using Lyo.Authentication.Models.Records;
using Lyo.Common.Core.Identifiers;
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
        var scope = new LyoUserScope(LyoGuid.CreateV7(), user.Id, "people.read", DateTime.UtcNow, null);
        await _fixture.ScopeStore.CreateAsync(scope, null, TestCt);
        var listed = await _fixture.ScopeStore.ListForUserAsync(user.Id, null, TestCt);
        Assert.Contains(listed, s => s.Id == scope.Id && s.Name == "people.read");
        await _fixture.ScopeStore.DeleteAsync(scope.Id, null, TestCt);
        var after = await _fixture.ScopeStore.ListForUserAsync(user.Id, null, TestCt);
        Assert.DoesNotContain(after, s => s.Id == scope.Id);
    }

    [Fact]
    public async Task Create_DuplicateName_Throws()
    {
        var user = await CreateUserAsync();
        await _fixture.ScopeStore.CreateAsync(new(LyoGuid.CreateV7(), user.Id, "people.read", DateTime.UtcNow, null), null, TestCt);
        await Assert.ThrowsAsync<ConflictException>(() => _fixture.ScopeStore.CreateAsync(new(LyoGuid.CreateV7(), user.Id, "people.read", DateTime.UtcNow, null), null, TestCt));
    }

    [Fact]
    public async Task Delete_Missing_Throws() => await Assert.ThrowsAsync<NotFoundException>(() => _fixture.ScopeStore.DeleteAsync(LyoGuid.CreateV7(), null, TestCt));

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(
            LyoGuid.CreateV7(), "Scope Owner", $"scope-{LyoGuid.CreateV7():N}@example.com", true, null, null, [], null, null, DateTime.UtcNow, null, null, null, null);

        return await _fixture.UserStore.CreateAsync(user, null, TestCt);
    }
}