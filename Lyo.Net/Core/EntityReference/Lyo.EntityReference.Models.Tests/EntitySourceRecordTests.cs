namespace Lyo.EntityReference.Models.Tests;

public class EntitySourceRecordTests
{
    private static readonly DateTime ImportedAt = new(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void From_Guid_MatchesManualComposition()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var expected = EntitySourceRecord.From(EntityRef.For<TestSource>(guid), ImportedAt);
        var actual = EntitySourceRecord.From<TestSource>(guid, ImportedAt);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void From_String_MatchesManualComposition()
    {
        var expected = EntitySourceRecord.From(EntityRef.For<TestSource>("ext-42"), ImportedAt);
        var actual = EntitySourceRecord.From<TestSource>("ext-42", ImportedAt);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void From_Keys_MatchesManualComposition()
    {
        var expected = EntitySourceRecord.From(EntityRef.For<TestSource>("a", "b"), ImportedAt);
        var actual = EntitySourceRecord.From<TestSource>(ImportedAt, "a", "b");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void From_EntityAndSelector_MatchesManualComposition()
    {
        var dto = new TestSourceDto { ExternalId = "ext-99" };
        var expected = EntitySourceRecord.From(EntityRef.For(dto, d => [d.ExternalId]), ImportedAt);
        var actual = EntitySourceRecord.From(dto, d => [d.ExternalId], ImportedAt);
        Assert.Equal(expected, actual);
    }

    private sealed class TestSource;

    private sealed class TestSourceDto
    {
        public string ExternalId { get; set; } = "";
    }
}