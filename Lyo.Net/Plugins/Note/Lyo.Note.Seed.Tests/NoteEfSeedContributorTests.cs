using Lyo.Note.Postgres.Database;
using Lyo.Note.Seed;
using Lyo.Seed;

namespace Lyo.Note.Seed.Tests;

public sealed class NoteEfSeedContributorTests
{
    [Fact]
    public async Task NoteEfSeedContributor_Replace_ClearsBeforeInsert()
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(
            new NoteEfSeedContributor(),
            transport,
            new() { Count = 2, Conflict = SeedConflictMode.Replace, RandomSeed = 1 },
            TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(["clear", "persist"], transport.Log);
        var notes = transport.Items.OfType<NoteEntity>().ToList();
        Assert.Equal(2, notes.Count);
        Assert.Equal(2, notes.Select(note => note.SubjectEntityId).Distinct().Count());
        Assert.Equal(2, notes.Select(note => note.ActorEntityId).Distinct().Count());
        Assert.All(notes, note => Assert.False(string.IsNullOrWhiteSpace(note.Content)));
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<string> Log { get; } = [];

        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            if (items.Count == 0) {
                Log.Add("clear");
                return Task.CompletedTask;
            }

            Log.Add("persist");
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
