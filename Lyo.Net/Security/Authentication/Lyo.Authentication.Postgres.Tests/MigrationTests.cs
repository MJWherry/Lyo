using Microsoft.EntityFrameworkCore;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class MigrationTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public MigrationTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task NoPendingMigrations()
    {
        await using var context = await _fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var pending = await context.Database.GetPendingMigrationsAsync(TestCt);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task AppliedMigrations_IncludeInitialCreate()
    {
        await using var context = await _fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var applied = await context.Database.GetAppliedMigrationsAsync(TestCt);
        Assert.Contains(applied, m => m.EndsWith("_InitialCreate"));
    }

    [Fact]
    public async Task MigrationsHistoryTable_LivesInUserSchema()
    {
        await using var context = await _fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync(TestCt);
        try {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'user' AND table_name = '__EFMigrationsHistory'";
            var scalar = await cmd.ExecuteScalarAsync(TestCt);
            Assert.NotNull(scalar);
            Assert.Equal(1L, (long)scalar);
        }
        finally {
            await conn.CloseAsync();
        }
    }
}