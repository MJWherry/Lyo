using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Users;

namespace Lyo.Authentication.Tests;

public sealed class UserScopeNamesTests
{
    [Fact]
    public async Task ResolveAsync_EmptyStore_DoesNotReturnUserScopes()
    {
        var user = User(["people.read"]);
        var names = await UserScopeNames.ResolveAsync(new EmptyScopeStore(), user, TestCt);
        Assert.Empty(names);
    }

    [Fact]
    public async Task ResolveAsync_NullStore_ReturnsEmpty()
    {
        var names = await UserScopeNames.ResolveAsync(null, User(["people.read"]), TestCt);
        Assert.Empty(names);
    }

    private static LyoUser User(IReadOnlyList<string> scopes)
        => new(Guid.NewGuid(), "Ada", "ada@example.com", true, null, null, scopes, null, null, DateTime.UtcNow, null, null, null, null);

    private sealed class EmptyScopeStore : IUserScopeStore
    {
        public Task<IReadOnlyList<LyoUserScope>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LyoUserScope>>([]);

        public Task<LyoUserScope> CreateAsync(LyoUserScope scope, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
