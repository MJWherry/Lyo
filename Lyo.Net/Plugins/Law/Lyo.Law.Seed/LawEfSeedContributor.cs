using System.Security.Cryptography;
using Bogus;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Law.Models.Enums;
using Lyo.Law.Postgres;
using Lyo.Law.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Law.Seed;

/// <summary>EF seed of jurisdictions, one code each, and a title/chapter/section tree with an annotation and a revision. Schema <c>law</c>.</summary>
public sealed class LawEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Law";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresLawOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<LawDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new LawDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var codes = graph.Entity(options.Count, i => new LawJurisdictionEntity {
            Id = LyoGuid.CreateV7(),
            Key = $"US-S{i}",
            Name = faker.Address.State(),
            CreatedTimestamp = now
        }).ForEach(_ => 1, (jurisdiction, _) => new LawCodeEntity {
            Id = LyoGuid.CreateV7(),
            JurisdictionId = jurisdiction.Id,
            Key = "statutes",
            Name = "Revised Statutes",
            CitationPrefix = jurisdiction.Key,
            CreatedTimestamp = now
        });
        var titles = codes.ForEach(_ => 1, (code, _) => Node(code.Id, null, LawNodeKind.Title, 0, "1", "General Provisions", faker.Lorem.Paragraph(), now));
        var chapters = titles.ForEach(_ => 1, (title, _) => Node(title.CodeId, title.Id, LawNodeKind.Chapter, 0, "1", "Administration", faker.Lorem.Paragraph(), now));
        var sections = chapters.ForEach(_ => 1, (chapter, _) => Node(chapter.CodeId, chapter.Id, LawNodeKind.Section, 0, "101", "Definitions", faker.Lorem.Paragraph(), now));
        sections.ForEach(_ => 1, (section, _) => new LawAnnotationEntity {
            Id = LyoGuid.CreateV7(),
            NodeId = section.Id,
            Kind = LawAnnotationKind.History,
            Ordinal = 1,
            Body = faker.Lorem.Sentence(),
            CreatedTimestamp = now
        });
        sections.ForEach(_ => 1, (section, _) => new LawRevisionEntity {
            Id = LyoGuid.CreateV7(),
            NodeId = section.Id,
            Revision = 1,
            Heading = section.Heading,
            Body = section.Body,
            ContentSha256 = section.ContentSha256,
            AnnotationsJson = "[]",
            CreatedTimestamp = now
        });
    }

    private static LawNodeEntity Node(Guid codeId, Guid? parentId, LawNodeKind kind, int sort, string label, string heading, string body, DateTime now)
        => new() {
            Id = LyoGuid.CreateV7(),
            CodeId = codeId,
            ParentId = parentId,
            Kind = kind,
            SortOrder = sort,
            Label = label,
            Heading = heading,
            Body = body,
            Citation = parentId is null ? null : $"{label}",
            ContentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant(),
            SourceKey = $"{kind}-{label}-{parentId?.ToString() ?? "root"}",
            CreatedTimestamp = now
        };

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<LawDbContext> ef)
            throw new SeedException("Law OnClear requires EfSeedTransport<LawDbContext>.");

        var db = ef.Context;
        await db.Annotations.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Revisions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Nodes.ExecuteUpdateAsync(s => s.SetProperty(n => n.ParentId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Nodes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Codes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Jurisdictions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
