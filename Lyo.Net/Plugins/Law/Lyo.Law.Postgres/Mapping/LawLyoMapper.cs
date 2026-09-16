using Lyo.Api.Mapping;
using Lyo.Exceptions;
using Lyo.Law;
using Lyo.Law.Models.Request;
using Lyo.Law.Models.Response;
using Lyo.Law.Postgres.Database;

namespace Lyo.Law.Postgres.Mapping;

/// <summary>Hand-rolled <see cref="ILyoMapper"/> for jurisdiction and code CRUD. Node text is written through <see cref="ILawStore"/>.</summary>
public sealed class LawLyoMapper : ILyoMapper
{
    /// <inheritdoc />
    public TResult Map<TResult>(object source)
        => source switch {
            LawJurisdictionReq req when typeof(TResult) == typeof(LawJurisdictionEntity) => (TResult)(object)ReqToNew(req),
            LawCodeReq req when typeof(TResult) == typeof(LawCodeEntity) => (TResult)(object)ReqToNew(req),
            LawJurisdictionEntity entity when typeof(TResult) == typeof(LawJurisdictionRes) => (TResult)(object)ToRes(entity),
            LawCodeEntity entity when typeof(TResult) == typeof(LawCodeRes) => (TResult)(object)ToRes(entity),
            _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (LawJurisdictionReq req, LawJurisdictionEntity entity):
                Apply(req, entity);
                break;
            case (LawCodeReq req, LawCodeEntity entity):
                Apply(req, entity);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    private static LawJurisdictionEntity ReqToNew(LawJurisdictionReq request)
    {
        var entity = new LawJurisdictionEntity();
        Apply(request, entity);
        return entity;
    }

    private static LawCodeEntity ReqToNew(LawCodeReq request)
    {
        var entity = new LawCodeEntity();
        Apply(request, entity);
        return entity;
    }

    private static void Apply(LawJurisdictionReq request, LawJurisdictionEntity entity)
    {
        ArgumentHelpers.ThrowIfNull(request);
        LawJurisdictionKey.ThrowIfFederal(request.Key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Name);
        entity.Key = request.Key.Trim();
        entity.Name = request.Name.Trim();
    }

    private static void Apply(LawCodeReq request, LawCodeEntity entity)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Name);
        entity.JurisdictionId = request.JurisdictionId;
        entity.Key = request.Key.Trim();
        entity.Name = request.Name.Trim();
        entity.CitationPrefix = string.IsNullOrWhiteSpace(request.CitationPrefix) ? null : request.CitationPrefix.Trim();
        entity.SourceHost = string.IsNullOrWhiteSpace(request.SourceHost) ? null : request.SourceHost.Trim();
    }

    private static LawJurisdictionRes ToRes(LawJurisdictionEntity entity)
        => new() { Id = entity.Id, Key = entity.Key, Name = entity.Name };

    private static LawCodeRes ToRes(LawCodeEntity entity)
        => new() {
            Id = entity.Id,
            JurisdictionId = entity.JurisdictionId,
            Key = entity.Key,
            Name = entity.Name,
            CitationPrefix = entity.CitationPrefix,
            SourceHost = entity.SourceHost
        };

    private static InvalidOperationException Unmapped(Type source, Type dest)
        => new($"No mapping configured from {source.Name} to {dest.Name}.");
}
