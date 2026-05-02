using Lyo.Api.Mapping;
using Lyo.Comic.Api.Models.Request;
using Lyo.Comic.Api.Models.Response;
using Lyo.Comic.Enums;
using Lyo.Comic.Postgres.Database;

namespace Lyo.Comic.Api.Mapping;

/// <summary>
/// Hand-rolled ILyoMapper for the Comic API. All mappings use explicit property assignments —
/// no reflection or third-party mapping library.
/// </summary>
public sealed class ComicLyoMapper : ILyoMapper
{
    public TResult Map<TResult>(object source)
        => source switch {
            // Req → new Entity  (Create / CreateBulk)
            ComicSeriesReq    req when typeof(TResult) == typeof(SeriesEntity)    => (TResult)(object)ReqToNew(req),
            ComicVolumeReq    req when typeof(TResult) == typeof(VolumeEntity)    => (TResult)(object)ReqToNew(req),
            ComicChapterReq   req when typeof(TResult) == typeof(ChapterEntity)   => (TResult)(object)ReqToNew(req),
            ComicPageReq      req when typeof(TResult) == typeof(PageEntity)      => (TResult)(object)ReqToNew(req),
            ComicCharacterReq req when typeof(TResult) == typeof(CharacterEntity) => (TResult)(object)ReqToNew(req),

            // Entity → Response  (Get / Query)
            SeriesEntity    e when typeof(TResult) == typeof(ComicSeriesRes)    => (TResult)(object)ToRes(e),
            VolumeEntity    e when typeof(TResult) == typeof(ComicVolumeRes)    => (TResult)(object)ToRes(e),
            ChapterEntity   e when typeof(TResult) == typeof(ComicChapterRes)   => (TResult)(object)ToRes(e),
            PageEntity      e when typeof(TResult) == typeof(ComicPageRes)      => (TResult)(object)ToRes(e),
            CharacterEntity e when typeof(TResult) == typeof(ComicCharacterRes) => (TResult)(object)ToRes(e),

            _ => throw new InvalidOperationException(
                $"No mapping configured from {source.GetType().Name} to {typeof(TResult).Name}.")
        };

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (ComicSeriesReq    req, SeriesEntity    e): Apply(req, e); break;
            case (ComicVolumeReq    req, VolumeEntity    e): Apply(req, e); break;
            case (ComicChapterReq   req, ChapterEntity   e): Apply(req, e); break;
            case (ComicPageReq      req, PageEntity      e): Apply(req, e); break;
            case (ComicCharacterReq req, CharacterEntity e): Apply(req, e); break;
            default:
                throw new InvalidOperationException(
                    $"No mapping configured from {typeof(TSource).Name} to {typeof(TDest).Name}.");
        }
    }

    private static SeriesEntity ReqToNew(ComicSeriesReq r)
    {
        var id = Guid.NewGuid();
        return new SeriesEntity {
            Id             = id,
            Title          = r.Title,
            Slug           = r.Slug,
            ComicType      = r.ComicType,
            Status         = r.Status,
            Description    = r.Description,
            Language       = r.Language,
            PublishedYear  = r.PublishedYear,
            Author         = r.Author,
            Artist         = r.Artist,
            Publisher      = r.Publisher,
            Source         = r.Source,
            CoverImageRef  = r.CoverImageRef,
            Demographic    = r.Demographic,
            AlternateTitles = r.AlternateTitles
                .Select(a => new AlternateTitleEntity { Id = Guid.NewGuid(), SeriesId = id, Title = a.Title, Language = a.Language })
                .ToList()
        };
    }

    private static VolumeEntity ReqToNew(ComicVolumeReq r) => new() {
        Id            = Guid.NewGuid(),
        SeriesId      = r.SeriesId,
        VolumeNumber  = r.VolumeNumber,
        Title         = r.Title,
        CoverImageRef = r.CoverImageRef,
        PublishedDate = r.PublishedDate.HasValue ? DateOnly.FromDateTime(r.PublishedDate.Value) : null
    };

    private static ChapterEntity ReqToNew(ComicChapterReq r) => new() {
        Id            = Guid.NewGuid(),
        SeriesId      = r.SeriesId,
        VolumeId      = r.VolumeId,
        ChapterNumber = r.ChapterNumber,
        Title         = r.Title,
        Language      = r.Language,
        PageCount     = r.PageCount,
        PublishedDate = r.PublishedDate.HasValue ? DateOnly.FromDateTime(r.PublishedDate.Value) : null,
        Source        = r.Source
    };

    private static PageEntity ReqToNew(ComicPageReq r) => new() {
        Id          = Guid.NewGuid(),
        ChapterId   = r.ChapterId,
        PageNumber  = r.PageNumber,
        ImageRef    = r.ImageRef,
        Width       = r.Width,
        Height      = r.Height
    };

    private static CharacterEntity ReqToNew(ComicCharacterReq r) => new() {
        Id          = Guid.NewGuid(),
        SeriesId    = r.SeriesId,
        Name        = r.Name,
        Description = r.Description,
        ImageRef    = r.ImageRef,
        Role        = r.Role
    };
    
    internal static ComicSeriesRes ToRes(SeriesEntity e) => new() {
        Id               = e.Id,
        Title            = e.Title,
        Slug             = e.Slug,
        ComicType        = e.ComicType,
        Status           = e.Status,
        Description      = e.Description,
        Language         = e.Language,
        PublishedYear    = e.PublishedYear,
        Author           = e.Author,
        Artist           = e.Artist,
        Publisher        = e.Publisher,
        Source           = e.Source,
        CoverImageRef    = e.CoverImageRef,
        Demographic      = e.Demographic,
        CreatedTimestamp = e.CreatedTimestamp,
        UpdatedTimestamp = e.UpdatedTimestamp,
        AlternateTitles  = e.AlternateTitles
            .Select(a => new ComicAlternateTitleRes { Id = a.Id, Title = a.Title, Language = a.Language })
            .ToList()
    };

    internal static ComicVolumeRes ToRes(VolumeEntity e) => new() {
        Id               = e.Id,
        SeriesId         = e.SeriesId,
        VolumeNumber     = e.VolumeNumber,
        Title            = e.Title,
        CoverImageRef    = e.CoverImageRef,
        PublishedDate    = e.PublishedDate?.ToDateTime(TimeOnly.MinValue),
        CreatedTimestamp = e.CreatedTimestamp,
        UpdatedTimestamp = e.UpdatedTimestamp
    };

    internal static ComicChapterRes ToRes(ChapterEntity e) => new() {
        Id               = e.Id,
        SeriesId         = e.SeriesId,
        VolumeId         = e.VolumeId,
        ChapterNumber    = e.ChapterNumber,
        Title            = e.Title,
        Language         = e.Language,
        PageCount        = e.PageCount,
        PublishedDate    = e.PublishedDate?.ToDateTime(TimeOnly.MinValue),
        Source           = e.Source,
        CreatedTimestamp = e.CreatedTimestamp,
        UpdatedTimestamp = e.UpdatedTimestamp
    };

    internal static ComicPageRes ToRes(PageEntity e) => new() {
        Id               = e.Id,
        ChapterId        = e.ChapterId,
        PageNumber       = e.PageNumber,
        ImageRef         = e.ImageRef,
        Width            = e.Width,
        Height           = e.Height,
        CreatedTimestamp = e.CreatedTimestamp,
        UpdatedTimestamp = e.UpdatedTimestamp
    };

    internal static ComicCharacterRes ToRes(CharacterEntity e) => new() {
        Id               = e.Id,
        SeriesId         = e.SeriesId,
        Name             = e.Name,
        Description      = e.Description,
        ImageRef         = e.ImageRef,
        Role             = e.Role,
        CreatedTimestamp = e.CreatedTimestamp,
        UpdatedTimestamp = e.UpdatedTimestamp
    };

    // AlternateTitles for Series are managed by the BeforeUpdate/BeforeUpsert
    // hooks registered in BuildComicApiEndpoints (requires DB context access).

    private static void Apply(ComicSeriesReq r, SeriesEntity e)
    {
        e.Title         = r.Title;
        e.Slug          = r.Slug;
        e.ComicType     = r.ComicType;
        e.Status        = r.Status;
        e.Description   = r.Description;
        e.Language      = r.Language;
        e.PublishedYear = r.PublishedYear;
        e.Author        = r.Author;
        e.Artist        = r.Artist;
        e.Publisher     = r.Publisher;
        e.Source        = r.Source;
        e.CoverImageRef = r.CoverImageRef;
        e.Demographic   = r.Demographic;
    }

    private static void Apply(ComicVolumeReq r, VolumeEntity e)
    {
        e.SeriesId      = r.SeriesId;
        e.VolumeNumber  = r.VolumeNumber;
        e.Title         = r.Title;
        e.CoverImageRef = r.CoverImageRef;
        e.PublishedDate = r.PublishedDate.HasValue ? DateOnly.FromDateTime(r.PublishedDate.Value) : null;
    }

    private static void Apply(ComicChapterReq r, ChapterEntity e)
    {
        e.SeriesId      = r.SeriesId;
        e.VolumeId      = r.VolumeId;
        e.ChapterNumber = r.ChapterNumber;
        e.Title         = r.Title;
        e.Language      = r.Language;
        e.PageCount     = r.PageCount;
        e.PublishedDate = r.PublishedDate.HasValue ? DateOnly.FromDateTime(r.PublishedDate.Value) : null;
        e.Source        = r.Source;
    }

    private static void Apply(ComicPageReq r, PageEntity e)
    {
        e.ChapterId  = r.ChapterId;
        e.PageNumber = r.PageNumber;
        e.ImageRef   = r.ImageRef;
        e.Width      = r.Width;
        e.Height     = r.Height;
    }

    private static void Apply(ComicCharacterReq r, CharacterEntity e)
    {
        e.SeriesId    = r.SeriesId;
        e.Name        = r.Name;
        e.Description = r.Description;
        e.ImageRef    = r.ImageRef;
        e.Role        = r.Role;
    }
}
