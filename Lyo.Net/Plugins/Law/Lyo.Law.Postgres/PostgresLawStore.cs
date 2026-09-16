using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Law.Models.Request;
using Lyo.Law.Models.Response;
using Lyo.Law.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Law.Postgres;

/// <summary>Postgres-backed <see cref="ILawStore"/>.</summary>
public sealed class PostgresLawStore(IDbContextFactory<LawDbContext> contextFactory) : ILawStore, IHealth
{
    /// <inheritdoc />
    public string HealthCheckName => "law-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(contextFactory, PostgresLawOptions.Schema, ct);

    /// <inheritdoc />
    public async Task<LawJurisdictionRes> SaveJurisdictionAsync(LawJurisdictionReq jurisdiction, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(jurisdiction);
        LawJurisdictionKey.ThrowIfFederal(jurisdiction.Key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jurisdiction.Name);
        var key = jurisdiction.Key.Trim();
        var name = jurisdiction.Name.Trim();
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Jurisdictions.FirstOrDefaultAsync(j => j.Key == key, ct).ConfigureAwait(false);
        if (existing is null) {
            existing = new LawJurisdictionEntity { Id = LyoGuid.CreateV7(), Key = key, Name = name };
            context.Jurisdictions.Add(existing);
        } else {
            existing.Name = name;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(existing);
    }

    /// <inheritdoc />
    public async Task<LawJurisdictionRes?> GetJurisdictionByKeyAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Jurisdictions.AsNoTracking().FirstOrDefaultAsync(j => j.Key == key.Trim(), ct).ConfigureAwait(false);
        return row is null ? null : ToRes(row);
    }

    /// <inheritdoc />
    public async Task<LawCodeRes> SaveCodeAsync(string jurisdictionKey, LawCodeReq code, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(code);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jurisdictionKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code.Key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code.Name);
        var key = code.Key.Trim();
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var jurisdiction = await context.Jurisdictions.FirstOrDefaultAsync(j => j.Key == jurisdictionKey.Trim(), ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(jurisdiction, $"Unknown jurisdiction '{jurisdictionKey.Trim()}'.");
        var existing = await context.Codes.FirstOrDefaultAsync(c => c.JurisdictionId == jurisdiction.Id && c.Key == key, ct).ConfigureAwait(false);
        if (existing is null) {
            existing = new LawCodeEntity { Id = LyoGuid.CreateV7(), JurisdictionId = jurisdiction.Id, Key = key };
            context.Codes.Add(existing);
        }

        existing.Name = code.Name.Trim();
        existing.CitationPrefix = NullIfWhiteSpace(code.CitationPrefix);
        existing.SourceHost = NullIfWhiteSpace(code.SourceHost);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToRes(existing);
    }

    /// <inheritdoc />
    public async Task<LawCodeRes?> GetCodeAsync(string jurisdictionKey, string codeKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jurisdictionKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codeKey);
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Codes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Jurisdiction.Key == jurisdictionKey.Trim() && c.Key == codeKey.Trim(), ct)
            .ConfigureAwait(false);
        return row is null ? null : ToRes(row);
    }

    /// <inheritdoc />
    public async Task<LawNodeUpsertRes> UpsertNodeBySourceKeyAsync(LawNodeUpsertReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(request.Annotations);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.SourceKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Label);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Heading);
        var sourceKey = request.SourceKey.Trim();
        var hash = Hash(request);
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var code = await RequireCodeAsync(context, request.JurisdictionKey, request.CodeKey, ct).ConfigureAwait(false);
        var parentId = await ResolveParentIdAsync(context, code.Id, sourceKey, request.ParentSourceKey, ct).ConfigureAwait(false);
        var existing = await context.Nodes.Include(n => n.Annotations)
            .FirstOrDefaultAsync(n => n.CodeId == code.Id && n.SourceKey == sourceKey, ct)
            .ConfigureAwait(false);
        if (existing is not null && existing.ContentSha256 == hash)
            return new LawNodeUpsertRes { Id = existing.Id, Unchanged = true };

        if (existing is null) {
            existing = new LawNodeEntity { Id = LyoGuid.CreateV7(), CodeId = code.Id, SourceKey = sourceKey };
            context.Nodes.Add(existing);
        } else {
            var previous = existing.Annotations.ToArray();
            var next = await context.Revisions.Where(r => r.NodeId == existing.Id).MaxAsync(r => (int?)r.Revision, ct).ConfigureAwait(false) ?? 0;
            context.Revisions.Add(new LawRevisionEntity {
                Id = LyoGuid.CreateV7(),
                NodeId = existing.Id,
                Revision = next + 1,
                Heading = existing.Heading,
                Body = existing.Body,
                ContentSha256 = existing.ContentSha256,
                AnnotationsJson = Snapshot(previous)
            });
            context.Annotations.RemoveRange(previous);
        }

        existing.ParentId = parentId;
        existing.Kind = request.Kind;
        existing.SortOrder = request.SortOrder;
        existing.Label = request.Label.Trim();
        existing.Heading = request.Heading.Trim();
        existing.Body = NullIfWhiteSpace(request.Body);
        existing.Citation = NullIfWhiteSpace(request.Citation);
        existing.IsRepealed = request.IsRepealed;
        existing.ContentSha256 = hash;
        existing.SourceUrl = NullIfWhiteSpace(request.SourceUrl);
        foreach (var note in request.Annotations)
            context.Annotations.Add(ToEntity(note, existing.Id));

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return new LawNodeUpsertRes { Id = existing.Id, Unchanged = false };
    }

    /// <inheritdoc />
    public async Task<LawNodeDetailRes?> GetNodeByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var node = await DetailQuery(context).FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
        return node is null ? null : await ToDetailAsync(context, node, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LawNodeDetailRes?> GetNodeByCitationAsync(string jurisdictionKey, string codeKey, string citation, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jurisdictionKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codeKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(citation);
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var trimmed = citation.Trim();
        var node = await DetailQuery(context).FirstOrDefaultAsync(
            n => n.Code.Jurisdiction.Key == jurisdictionKey.Trim() && n.Code.Key == codeKey.Trim() && n.Citation == trimmed, ct).ConfigureAwait(false);
        return node is null ? null : await ToDetailAsync(context, node, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LawNodeChildRes>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Nodes.AsNoTracking()
            .Where(n => n.ParentId == parentId)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Label)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(ToChild).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LawNodeChildRes>> GetAncestorsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var current = await context.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct).ConfigureAwait(false);
        if (current is null)
            return [];

        var ancestors = new List<LawNodeChildRes>();
        var parentId = current.ParentId;
        var guard = 0;
        while (parentId is Guid id) {
            OperationHelpers.ThrowIf(++guard > 32, "Node parent chain is cyclic.");
            var parent = await context.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
            if (parent is null)
                break;

            ancestors.Add(ToChild(parent));
            parentId = parent.ParentId;
        }

        return ancestors;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LawRevisionRes>> ListRevisionsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Revisions.AsNoTracking()
            .Where(r => r.NodeId == nodeId)
            .OrderBy(r => r.Revision)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(ToRes).ToArray();
    }

    private static async Task<LawCodeEntity> RequireCodeAsync(LawDbContext context, string jurisdictionKey, string codeKey, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jurisdictionKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codeKey);
        var code = await context.Codes.FirstOrDefaultAsync(c => c.Jurisdiction.Key == jurisdictionKey.Trim() && c.Key == codeKey.Trim(), ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(code, $"Unknown code '{codeKey.Trim()}' in jurisdiction '{jurisdictionKey.Trim()}'.");
        return code;
    }

    private static async Task<Guid?> ResolveParentIdAsync(LawDbContext context, Guid codeId, string sourceKey, string? parentSourceKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parentSourceKey))
            return null;

        var parentKey = parentSourceKey.Trim();
        OperationHelpers.ThrowIf(parentKey == sourceKey, "A node cannot be its own parent.");
        var parent = await context.Nodes.FirstOrDefaultAsync(n => n.CodeId == codeId && n.SourceKey == parentKey, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(parent, $"Unknown parent source key '{parentKey}'.");
        return parent.Id;
    }

    private static IQueryable<LawNodeEntity> DetailQuery(LawDbContext context)
        => context.Nodes.AsNoTracking().Include(n => n.Annotations).Include(n => n.Code).ThenInclude(c => c.Jurisdiction);

    private static async Task<LawNodeDetailRes> ToDetailAsync(LawDbContext context, LawNodeEntity node, CancellationToken ct)
    {
        string? parentSourceKey = null;
        if (node.ParentId is Guid parentId)
            parentSourceKey = await context.Nodes.AsNoTracking().Where(n => n.Id == parentId).Select(n => n.SourceKey).FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return new LawNodeDetailRes {
            Id = node.Id,
            CodeId = node.CodeId,
            JurisdictionKey = node.Code.Jurisdiction.Key,
            CodeKey = node.Code.Key,
            ParentId = node.ParentId,
            ParentSourceKey = parentSourceKey,
            SourceKey = node.SourceKey,
            SourceUrl = node.SourceUrl,
            Kind = node.Kind,
            SortOrder = node.SortOrder,
            Label = node.Label,
            Heading = node.Heading,
            Body = node.Body,
            Citation = node.Citation,
            IsRepealed = node.IsRepealed,
            ContentSha256 = node.ContentSha256,
            Annotations = node.Annotations.OrderBy(a => a.Ordinal).Select(ToRes).ToList()
        };
    }

    private static LawAnnotationEntity ToEntity(LawAnnotationReq note, Guid nodeId)
    {
        ArgumentHelpers.ThrowIfNull(note);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(note.Body);
        return new LawAnnotationEntity {
            Id = LyoGuid.CreateV7(),
            NodeId = nodeId,
            Kind = note.Kind,
            Ordinal = note.Ordinal,
            Body = note.Body.Trim(),
            RelatedCitation = NullIfWhiteSpace(note.RelatedCitation),
            RelatedNodeId = note.RelatedNodeId
        };
    }

    private static string Hash(LawNodeUpsertReq request)
    {
        var builder = new StringBuilder();
        builder.Append(request.Kind).Append('\n');
        builder.Append(request.Label.Trim()).Append('\n');
        builder.Append(request.Heading.Trim()).Append('\n');
        builder.Append(request.Body ?? string.Empty).Append('\n');
        builder.Append(request.Citation?.Trim() ?? string.Empty).Append('\n');
        builder.Append(request.IsRepealed).Append('\n');
        foreach (var note in request.Annotations.OrderBy(a => a.Ordinal).ThenBy(a => a.Body, StringComparer.Ordinal)) {
            builder.Append(note.Kind).Append('|').Append(note.Ordinal).Append('|').Append(note.Body).Append('|');
            builder.Append(note.RelatedCitation ?? string.Empty).Append('|');
            builder.Append(note.RelatedNodeId?.ToString("D") ?? string.Empty).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static string Snapshot(IEnumerable<LawAnnotationEntity> notes)
        => JsonSerializer.Serialize(notes.OrderBy(a => a.Ordinal).Select(a => new AnnotationSnapshot(a.Kind.ToString(), a.Ordinal, a.Body, a.RelatedCitation, a.RelatedNodeId)));

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LawJurisdictionRes ToRes(LawJurisdictionEntity row) => new() { Id = row.Id, Key = row.Key, Name = row.Name };

    private static LawCodeRes ToRes(LawCodeEntity row)
        => new() {
            Id = row.Id,
            JurisdictionId = row.JurisdictionId,
            Key = row.Key,
            Name = row.Name,
            CitationPrefix = row.CitationPrefix,
            SourceHost = row.SourceHost
        };

    private static LawAnnotationRes ToRes(LawAnnotationEntity row)
        => new() {
            Id = row.Id,
            NodeId = row.NodeId,
            Kind = row.Kind,
            Ordinal = row.Ordinal,
            Body = row.Body,
            RelatedCitation = row.RelatedCitation,
            RelatedNodeId = row.RelatedNodeId
        };

    private static LawNodeChildRes ToChild(LawNodeEntity row)
        => new() {
            Id = row.Id,
            Kind = row.Kind,
            SortOrder = row.SortOrder,
            Label = row.Label,
            Heading = row.Heading,
            Citation = row.Citation,
            SourceKey = row.SourceKey
        };

    private static LawRevisionRes ToRes(LawRevisionEntity row)
        => new() {
            Id = row.Id,
            NodeId = row.NodeId,
            Revision = row.Revision,
            Heading = row.Heading,
            Body = row.Body,
            ContentSha256 = row.ContentSha256,
            AnnotationsJson = row.AnnotationsJson,
            CreatedTimestamp = row.CreatedTimestamp
        };

    private sealed record AnnotationSnapshot(string Kind, int Ordinal, string Body, string? RelatedCitation, Guid? RelatedNodeId);
}
