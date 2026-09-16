using Bogus;
using Lyo.Comment.Postgres;
using Lyo.Comment.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Comment.Seed;

/// <summary>EF seed of comments on different posts, each from a different person and with one like. No replies.</summary>
public sealed class CommentEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Tenant id written on every seeded row.</summary>
    public static readonly Guid TenantId = new("11111111-1111-7111-8111-111111111111");

    /// <summary>Subject type written on every seeded row.</summary>
    public const string SubjectType = "post";

    /// <inheritdoc />
    public override string Name => "Comment";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresCommentOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<CommentDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new CommentDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(options.Count, i => new CommentEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = TenantId,
            SubjectEntityType = SubjectType,
            SubjectEntityId = $"post-{i}",
            ActorEntityType = "person",
            ActorEntityId = $"person-{i}",
            Visibility = EntityRefVisibility.Public,
            CreatedAt = DateTime.UtcNow,
            Content = faker.Lorem.Sentence()
        }).ForEach(_ => 1, (comment, _) => new CommentReactionEntity {
            Id = LyoGuid.CreateV7(),
            ReactionType = (int)CommentReactionType.Like,
            TenantId = comment.TenantId,
            SubjectEntityType = comment.SubjectEntityType,
            SubjectEntityId = comment.SubjectEntityId,
            ActorEntityType = comment.ActorEntityType,
            ActorEntityId = comment.ActorEntityId,
            CreatedTimestamp = DateTime.UtcNow
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<CommentDbContext> ef)
            throw new SeedException("Comment OnClear requires EfSeedTransport<CommentDbContext>.");

        var db = ef.Context;
        await db.CommentReactions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Comments.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
