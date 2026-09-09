using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Seed.Tests;

public sealed class EfSeedRunnerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private SeedHarnessDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        _connection = new("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SeedHarnessDbContext>().UseSqlite(_connection).Options;
        _db = new(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SeedAsync_EntityCount_PersistsRows()
    {
        var result = await RunAsync(new ParentContributor(3));
        Assert.True(result.Success);
        Assert.Equal(3, await _db.Parents.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, result.Counts["ParentRow"]);
    }

    [Fact]
    public async Task SeedAsync_ForEach_SetsParentId()
    {
        await RunAsync(new ParentChildContributor());
        var children = await _db.Children.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(4, children.Count);
        var parentIds = await _db.Parents.Select(p => p.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(children, c => Assert.Contains(c.ParentId, parentIds));
    }

    [Fact]
    public async Task SeedAsync_SkipIfNotEmpty_SkipsSecondRun()
    {
        var skip = new SeedOptions { Count = 3, Conflict = SeedConflictMode.SkipIfNotEmpty };
        await RunAsync(new ParentContributor(2), skip);
        var second = await RunAsync(new ParentContributor(9), skip);
        Assert.True(second.Skipped);
        Assert.Equal(2, await _db.Parents.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SeedAsync_Default_AppendsWhenRowsExist()
    {
        await RunAsync(new ParentContributor(2));
        var second = await RunAsync(new ParentContributor(3));
        Assert.False(second.Skipped);
        Assert.Equal(5, await _db.Parents.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SeedAsync_Append_AddsMoreRows()
    {
        await RunAsync(new ParentContributor(2));
        var second = await RunAsync(new ParentContributor(3), new SeedOptions { Conflict = SeedConflictMode.Append, Count = 3 });
        Assert.False(second.Skipped);
        Assert.Equal(5, await _db.Parents.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SeedAsync_After_SeesGeneratedParents()
    {
        var afterNames = new List<string>();
        await RunAsync(new AfterContributor(afterNames));
        Assert.Equal(["p-0", "p-1"], afterNames);
        Assert.Equal(2, await _db.Parents.CountAsync(TestContext.Current.CancellationToken));
    }

    private Task<SeedResult> RunAsync(SeedContributor contributor, SeedOptions? options = null)
    {
        ISeedRunner runner = new SeedRunner();
        var transport = new EfSeedTransport<SeedHarnessDbContext>(_db);
        return runner.SeedAsync(contributor, transport, options ?? new() { Count = 3 }, TestContext.Current.CancellationToken);
    }

    private sealed class ParentContributor(int count) : SeedContributor
    {
        public override string Name => "parents";

        public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

        protected override void Configure(SeedGraph graph, SeedOptions options)
            => graph.Entity(count, i => new ParentRow { Id = Guid.NewGuid(), Name = $"p-{i}" });
    }

    private sealed class ParentChildContributor : SeedContributor
    {
        public override string Name => "parent-child";

        public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

        protected override void Configure(SeedGraph graph, SeedOptions options)
            => graph.Entity(2, i => new ParentRow { Id = Guid.NewGuid(), Name = $"p-{i}" })
                .ForEach(_ => 2, (parent, i) => new ChildRow { Id = Guid.NewGuid(), ParentId = parent.Id, Label = $"{parent.Name}-{i}" });
    }

    private sealed class AfterContributor(List<string> names) : SeedContributor
    {
        public override string Name => "after";

        public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

        protected override void Configure(SeedGraph graph, SeedOptions options)
            => graph.Entity(2, i => new ParentRow { Id = Guid.NewGuid(), Name = $"p-{i}" })
                .After((session, _) => {
                    names.AddRange(session.Generated<ParentRow>().Select(p => p.Name));
                    return Task.CompletedTask;
                });
    }
}
