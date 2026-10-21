using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Law.Models.Enums;
using Lyo.Law.Models.Request;
using Lyo.Law.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Law.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class LawPostgresStoreTests(LawPostgresFixture fixture)
{
    [Fact]
    public async Task SaveJurisdiction_FederalKey_Throws()
    {
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveJurisdictionAsync(new() { Key = "US", Name = "United States" }, TestCt));
    }

    [Fact]
    public async Task SaveCode_TwoStates_RoundTripsIndependently()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        await store.SaveJurisdictionAsync(new() { Key = "US-PA", Name = "Pennsylvania" }, ct);
        await store.SaveJurisdictionAsync(new() { Key = "US-CA", Name = "California" }, ct);
        var paKey = Unique("consolidated-statutes");
        var caKey = Unique("penal");
        await store.SaveCodeAsync(
            "US-PA", new() {
                Key = paKey,
                Name = "Pennsylvania Consolidated Statutes",
                CitationPrefix = "Pa.C.S.",
                SourceHost = "www.palegis.us"
            }, ct);

        await store.SaveCodeAsync("US-CA", new() { Key = caKey, Name = "Penal Code", CitationPrefix = "Cal. Penal Code" }, ct);
        var pa = await store.GetCodeAsync("US-PA", paKey, ct);
        var ca = await store.GetCodeAsync("US-CA", caKey, ct);
        Assert.NotNull(pa);
        Assert.NotNull(ca);
        Assert.NotEqual(pa.Id, ca.Id);
        Assert.Equal("Pa.C.S.", pa.CitationPrefix);
        Assert.Equal("www.palegis.us", pa.SourceHost);
        Assert.Equal("Cal. Penal Code", ca.CitationPrefix);
        Assert.Equal("Pennsylvania", (await store.GetJurisdictionByKeyAsync("US-PA", ct))!.Name);
        Assert.Equal("California", (await store.GetJurisdictionByKeyAsync("US-CA", ct))!.Name);
    }

    [Fact]
    public async Task UpsertNode_PaSectionAndCaSection_KeepSeparateSourceKeys()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, caKey) = await SavePaAndCaAsync(store, ct);
        var title = await store.UpsertNodeBySourceKeyAsync(
            new() {
                JurisdictionKey = "US-PA",
                CodeKey = paKey,
                SourceKey = "ttl=67",
                Kind = LawNodeKind.Title,
                Label = "67",
                Heading = "PUBLIC WELFARE"
            }, ct);

        var section = await store.UpsertNodeBySourceKeyAsync(
            new() {
                JurisdictionKey = "US-PA",
                CodeKey = paKey,
                SourceKey = "ttl=67&div=0&chpt=11&sctn=5&subsctn=0",
                ParentSourceKey = "ttl=67",
                Kind = LawNodeKind.Section,
                Label = "1105",
                Heading = "Medical assistance",
                Citation = "67 Pa.C.S. § 1105",
                Body = "The department shall administer medical assistance.",
                SourceUrl = "https://www.palegis.us/statutes/consolidated/view-statute?txtType=HTM&ttl=67&div=0&chpt=11&sctn=5&subsctn=0",
                Annotations = [new() { Kind = LawAnnotationKind.History, Ordinal = 0, Body = "1972, Dec. 6, P.L. 1482, No. 334" }]
            }, ct);

        var california = await store.UpsertNodeBySourceKeyAsync(
            new() {
                JurisdictionKey = "US-CA",
                CodeKey = caKey,
                SourceKey = "section=187",
                Kind = LawNodeKind.Section,
                Label = "187",
                Heading = "Murder",
                Citation = "Cal. Penal Code § 187",
                Body = "Murder is the unlawful killing of a human being.",
                Annotations = [new() { Kind = LawAnnotationKind.History, Ordinal = 0, Body = "Enacted 1872" }]
            }, ct);

        var paLoaded = await store.GetNodeByCitationAsync("US-PA", paKey, "67 Pa.C.S. § 1105", ct);
        var caLoaded = await store.GetNodeByCitationAsync("US-CA", caKey, "Cal. Penal Code § 187", ct);
        Assert.NotNull(paLoaded);
        Assert.Equal(section.Id, paLoaded.Id);
        Assert.Equal(title.Id, paLoaded.ParentId);
        Assert.Equal("ttl=67&div=0&chpt=11&sctn=5&subsctn=0", paLoaded.SourceKey);
        Assert.Equal("1105", paLoaded.Label);
        Assert.Equal("1972, Dec. 6, P.L. 1482, No. 334", Assert.Single(paLoaded.Annotations).Body);
        Assert.NotNull(caLoaded);
        Assert.Equal(california.Id, caLoaded.Id);
        Assert.Null(caLoaded.ParentId);
        Assert.Equal("section=187", caLoaded.SourceKey);
        Assert.Equal("Enacted 1872", Assert.Single(caLoaded.Annotations).Body);
    }

    [Fact]
    public async Task UpsertNode_SameHash_DoesNotWriteRevision()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, _) = await SavePaAndCaAsync(store, ct);
        var request = Section(paKey, "same-hash", "Same", "Body");
        var first = await store.UpsertNodeBySourceKeyAsync(request, ct);
        var second = await store.UpsertNodeBySourceKeyAsync(request, ct);
        Assert.False(first.Unchanged);
        Assert.True(second.Unchanged);
        Assert.Equal(first.Id, second.Id);
        Assert.Empty(await store.ListRevisionsAsync(first.Id, ct));
    }

    [Fact]
    public async Task UpsertNode_ChangedBody_WritesRevisionAndReplacesAnnotations()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, _) = await SavePaAndCaAsync(store, ct);
        var sourceKey = Unique("changed");
        var first = await store.UpsertNodeBySourceKeyAsync(Section(paKey, sourceKey, "Old heading", "old body", "Act 1"), ct);
        var second = await store.UpsertNodeBySourceKeyAsync(Section(paKey, sourceKey, "New heading", "new body", "Act 2"), ct);
        Assert.Equal(first.Id, second.Id);
        Assert.False(second.Unchanged);
        var revision = Assert.Single(await store.ListRevisionsAsync(first.Id, ct));
        Assert.Equal("Old heading", revision.Heading);
        Assert.Equal("old body", revision.Body);
        Assert.Contains("Act 1", revision.AnnotationsJson, StringComparison.Ordinal);
        var loaded = await store.GetNodeByIdAsync(first.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal("new body", loaded.Body);
        Assert.Equal("Act 2", Assert.Single(loaded.Annotations).Body);
    }

    [Fact]
    public async Task UpsertNode_UnknownParentSourceKey_Throws()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, _) = await SavePaAndCaAsync(store, ct);
        var request = Section(paKey, Unique("orphan"), "Orphan", "body");
        request.ParentSourceKey = "missing-parent";
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpsertNodeBySourceKeyAsync(request, ct));
    }

    [Fact]
    public async Task GetChildren_ReturnsSiblingsInSortOrder()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, _) = await SavePaAndCaAsync(store, ct);
        var parentKey = Unique("parent");
        var parent = await store.UpsertNodeBySourceKeyAsync(
            new() {
                JurisdictionKey = "US-PA",
                CodeKey = paKey,
                SourceKey = parentKey,
                Kind = LawNodeKind.Chapter,
                Label = "11",
                Heading = "Chapter"
            }, ct);

        await store.UpsertNodeBySourceKeyAsync(Child(paKey, parentKey, Unique("later"), 2, "b", "Later"), ct);
        await store.UpsertNodeBySourceKeyAsync(Child(paKey, parentKey, Unique("earlier"), 0, "a", "Earlier"), ct);
        var children = await store.GetChildrenAsync(parent.Id, ct);
        Assert.Equal(["Earlier", "Later"], children.Select(c => c.Heading).ToArray());
        Assert.Equal([0, 2], children.Select(c => c.SortOrder).ToArray());
    }

    [Fact]
    public async Task GetByCitation_IsScopedToCode()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, caKey) = await SavePaAndCaAsync(store, ct);
        const string citation = "Shared § 1";
        var pa = await store.UpsertNodeBySourceKeyAsync(Section(paKey, Unique("pa-shared"), "Pennsylvania", "pa", citation: citation), ct);
        var california = Section(caKey, Unique("ca-shared"), "California", "ca", citation: citation);
        california.JurisdictionKey = "US-CA";
        var ca = await store.UpsertNodeBySourceKeyAsync(california, ct);
        var loaded = await store.GetNodeByCitationAsync("US-PA", paKey, citation, ct);
        Assert.NotNull(loaded);
        Assert.Equal(pa.Id, loaded.Id);
        Assert.NotEqual(ca.Id, loaded.Id);
    }

    [Fact]
    public async Task QueryProject_FilterByJurisdictionAndCitation_OmitsBody()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILawStore>();
        var (paKey, _) = await SavePaAndCaAsync(store, ct);
        var citation = Unique("query-citation");
        await store.UpsertNodeBySourceKeyAsync(Section(paKey, Unique("query-node"), "Projected heading", "secret body", citation: citation), ct);
        using var scope = fixture.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryService<LawDbContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Heading", "Citation"],
            WhereClause = WhereClauseBuilder.And(b => {
                b.Add(WhereClauseBuilder.Condition("Code.Jurisdiction.Key", ComparisonOperatorEnum.Equals, "US-PA"));
                b.Add(WhereClauseBuilder.Condition("Citation", ComparisonOperatorEnum.Equals, citation));
            })
        };

        var result = await query.QueryProjected<LawNodeEntity>(request, n => n.Heading, SortDirection.Asc, ct);
        Assert.True(result.IsSuccess);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(Assert.Single(result.Items!));
        Assert.False(row.ContainsKey("Body"));
        Assert.Equal("Projected heading", row["Heading"]?.ToString());
        Assert.Equal(citation, row["Citation"]?.ToString());
    }

    private static async Task<(string PaKey, string CaKey)> SavePaAndCaAsync(ILawStore store, CancellationToken ct)
    {
        await store.SaveJurisdictionAsync(new() { Key = "US-PA", Name = "Pennsylvania" }, ct);
        await store.SaveJurisdictionAsync(new() { Key = "US-CA", Name = "California" }, ct);
        var paKey = Unique("consolidated-statutes");
        var caKey = Unique("penal");
        await store.SaveCodeAsync(
            "US-PA", new() {
                Key = paKey,
                Name = "Pennsylvania Consolidated Statutes",
                CitationPrefix = "Pa.C.S.",
                SourceHost = "www.palegis.us"
            }, ct);

        await store.SaveCodeAsync("US-CA", new() { Key = caKey, Name = "Penal Code", CitationPrefix = "Cal. Penal Code" }, ct);
        return (paKey, caKey);
    }

    private static LawNodeUpsertReq Section(
        string codeKey,
        string sourceKey,
        string heading,
        string body,
        string? annotation = null,
        string jurisdictionKey = "US-PA",
        string? citation = null)
        => new() {
            JurisdictionKey = jurisdictionKey,
            CodeKey = codeKey,
            SourceKey = sourceKey,
            Kind = LawNodeKind.Section,
            Label = "1",
            Heading = heading,
            Body = body,
            Citation = citation,
            Annotations = annotation is null ? [] : [new() { Kind = LawAnnotationKind.History, Ordinal = 0, Body = annotation }]
        };

    private static LawNodeUpsertReq Child(string codeKey, string parentSourceKey, string sourceKey, int sortOrder, string label, string heading)
        => new() {
            JurisdictionKey = "US-PA",
            CodeKey = codeKey,
            SourceKey = sourceKey,
            ParentSourceKey = parentSourceKey,
            Kind = LawNodeKind.Section,
            SortOrder = sortOrder,
            Label = label,
            Heading = heading
        };

    private static string Unique(string name) => $"{name}-{LyoGuid.CreateV7():N}";
}