using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class MigrationTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public MigrationTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task NoPendingMigrations()
    {
        await using var context = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task AppliedMigrations_IncludeInitialCreate()
    {
        await using var context = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(applied, m => m.EndsWith("_InitialCreate"));
    }

    [Fact]
    public async Task MigrationsHistoryTable_LivesInUserSchema()
    {
        await using var context = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        try {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'user' AND table_name = '__EFMigrationsHistory'";
            var count = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(1L, count);
        }
        finally {
            await conn.CloseAsync();
        }
    }
}
