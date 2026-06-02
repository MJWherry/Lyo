namespace Lyo.EntityReference.Models.Tests;

public class EntityRelationBuilderTests
{
    [Fact]
    public void ForAndFrom_GuidKeys_BuildsEndpoints()
    {
        var subjectId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var actorId = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        var endpoints = EntityRelationBuilder.For<TestSubject>(subjectId).From<TestActor>(actorId);
        Assert.Equal(EntityRef.For<TestSubject>(subjectId), endpoints.Subject);
        Assert.Equal(EntityRef.For<TestActor>(actorId), endpoints.Actor);
    }

    [Fact]
    public void ForAndFrom_EntitySelectors_BuildsEndpoints()
    {
        var subject = new TestSubjectEntity { Id = Guid.NewGuid() };
        var actor = new TestActorEntity { Id = Guid.NewGuid() };
        var endpoints = EntityRelationBuilder.For(subject, s => [s.Id]).From(actor, a => [a.Id]);
        Assert.Equal(EntityRef.For(subject, s => s.Id), endpoints.Subject);
        Assert.Equal(EntityRef.For(actor, a => a.Id), endpoints.Actor);
    }

    [Fact]
    public void ForAndFrom_CompositeKeys_BuildsEndpoints()
    {
        var endpoints = EntityRelationBuilder.For<TestSubject>("ord-1", "line-2").From<TestActor>("user-1", "session-2");
        Assert.Equal(EntityRef.For<TestSubject>("ord-1", "line-2"), endpoints.Subject);
        Assert.Equal(EntityRef.For<TestActor>("user-1", "session-2"), endpoints.Actor);
    }

    [Fact]
    public void ForAndFrom_ExistingEntityRefs_BuildsEndpoints()
    {
        var subject = EntityRef.ForKey("Subject", "1");
        var actor = EntityRef.ForKey("Actor", "2");
        var endpoints = EntityRelationBuilder.For(subject).From(actor);
        Assert.Equal(subject, endpoints.Subject);
        Assert.Equal(actor, endpoints.Actor);
    }

    private sealed class TestSubject;

    private sealed class TestActor;

    private sealed class TestSubjectEntity
    {
        public Guid Id { get; set; }
    }

    private sealed class TestActorEntity
    {
        public Guid Id { get; set; }
    }
}