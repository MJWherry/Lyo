using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Seed.Tests;

public sealed class SeedRunnerFailureTests
{
    [Fact]
    public async Task SeedAsync_DbUpdateException_IncludesInnerConstraintMessage()
    {
        var inner = new InvalidOperationException("duplicate key value violates unique constraint \"ix_favorite_actor\"");
        var transport = new ThrowingTransport(new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            inner));
        var result = await new SeedRunner().SeedAsync(new OneRowContributor(), transport, new() { Count = 1 }, TestCt);
        Assert.False(result.Success);
        var text = string.Join(" ", result.Errors);
        Assert.Contains("ix_favorite_actor", text, StringComparison.Ordinal);
        Assert.Contains("An error occurred while saving the entity changes", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAsync_PostgresUpdateException_IncludesConstraintTableAndEntity()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestCt);
        var options = new DbContextOptionsBuilder<SeedHarnessDbContext>().UseSqlite(connection).Options;
        await using var db = new SeedHarnessDbContext(options);
        var row = new ParentRow { Id = Guid.NewGuid(), Name = "x" };
        db.Add(row);
        var postgres = new PostgresException(
            "duplicate key value violates unique constraint \"ix_favorite_actor\"",
            "ERROR",
            "ERROR",
            "23505",
            detail: "Key (actor_entity_id)=(seed) already exists.",
            tableName: "favorite",
            columnName: "actor_entity_id",
            constraintName: "ix_favorite_actor");
        var update = new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            postgres,
            [db.Entry(row)]);
        var result = await new SeedRunner().SeedAsync(new OneRowContributor(), new ThrowingTransport(update), new() { Count = 1 }, TestCt);
        Assert.False(result.Success);
        var text = string.Join('\n', result.Errors);
        Assert.Contains("23505", text, StringComparison.Ordinal);
        Assert.Contains("favorite", text, StringComparison.Ordinal);
        Assert.Contains("actor_entity_id", text, StringComparison.Ordinal);
        Assert.Contains("ix_favorite_actor", text, StringComparison.Ordinal);
        Assert.Contains("ParentRow", text, StringComparison.Ordinal);
        Assert.Contains("Key (actor_entity_id)=(seed) already exists.", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAsync_OperationCanceled_Propagates()
    {
        var transport = new ThrowingTransport(new OperationCanceledException());
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new SeedRunner().SeedAsync(new OneRowContributor(), transport, new() { Count = 1 }, TestCt));
    }

    private sealed class Row
    {
        public int Id { get; set; }
    }

    private sealed class OneRowContributor : SeedContributor
    {
        public override string Name => "rows";

        public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

        protected override void Configure(SeedGraph graph, SeedOptions options) => graph.Entity(1, i => new Row { Id = i });
    }

    private sealed class ThrowingTransport(Exception exception) : ISeedTransport
    {
        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(true);

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
            => throw exception;
    }
}
