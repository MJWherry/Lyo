using Lyo.Api.Mapping;
using Lyo.Recipe.Models.Enums;
using Lyo.Recipe.Models.Request;
using Lyo.Recipe.Models.Response;
using Lyo.Recipe.Postgres.Database;

namespace Lyo.Recipe.Postgres.Mapping;

/// <summary>Hand-rolled <see cref="ILyoMapper"/> for Recipe Req/entity/Res. Explicit property assignments.</summary>
public sealed class RecipeLyoMapper : ILyoMapper
{
    /// <inheritdoc />
    public TResult Map<TResult>(object source)
        => source switch {
            RecipeCategoryReq req when typeof(TResult) == typeof(RecipeCategoryEntity) => (TResult)(object)ReqToNew(req),
            IngredientReq req when typeof(TResult) == typeof(IngredientEntity) => (TResult)(object)ReqToNew(req),
            RecipeReq req when typeof(TResult) == typeof(RecipeEntity) => (TResult)(object)ReqToNew(req),
            RecipeAttachmentReq req when typeof(TResult) == typeof(RecipeAttachmentEntity) => (TResult)(object)ReqToNew(req),
            RecipeIngredientLineReq req when typeof(TResult) == typeof(RecipeIngredientLineEntity) => (TResult)(object)ReqToNew(req),
            RecipeStepReq req when typeof(TResult) == typeof(RecipeStepEntity) => (TResult)(object)ReqToNew(req),
            RecipeCategoryEntity e when typeof(TResult) == typeof(RecipeCategoryRes) => (TResult)(object)ToRes(e),
            IngredientEntity e when typeof(TResult) == typeof(IngredientRes) => (TResult)(object)ToRes(e),
            RecipeEntity e when typeof(TResult) == typeof(RecipeRes) => (TResult)(object)ToRes(e),
            RecipeAttachmentEntity e when typeof(TResult) == typeof(RecipeAttachmentRes) => (TResult)(object)ToRes(e),
            RecipeIngredientLineEntity e when typeof(TResult) == typeof(RecipeIngredientLineRes) => (TResult)(object)ToRes(e),
            RecipeStepEntity e when typeof(TResult) == typeof(RecipeStepRes) => (TResult)(object)ToRes(e),
            RecipeAttachmentRecord r when typeof(TResult) == typeof(RecipeAttachmentRes) => (TResult)(object)ToRes(r),
            RecipeIngredientLineRecord r when typeof(TResult) == typeof(RecipeIngredientLineRes) => (TResult)(object)ToRes(r),
            RecipeStepRecord r when typeof(TResult) == typeof(RecipeStepRes) => (TResult)(object)ToRes(r),
            var _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (RecipeCategoryReq req, RecipeCategoryEntity e):
                Apply(req, e);
                break;
            case (IngredientReq req, IngredientEntity e):
                Apply(req, e);
                break;
            case (RecipeReq req, RecipeEntity e):
                Apply(req, e);
                break;
            case (RecipeAttachmentReq req, RecipeAttachmentEntity e):
                Apply(req, e);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    private static InvalidOperationException Unmapped(Type source, Type dest)
        => new($"No mapping configured from {source.Name} to {dest.Name}.");

    private static DateTime UtcNow() => DateTime.UtcNow;

    internal static RecipeCategoryEntity ReqToNew(RecipeCategoryReq r)
    {
        var dest = new RecipeCategoryEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static IngredientEntity ReqToNew(IngredientReq r)
    {
        var dest = new IngredientEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static RecipeEntity ReqToNew(RecipeReq r)
    {
        var dest = new RecipeEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static RecipeAttachmentEntity ReqToNew(RecipeAttachmentReq r)
    {
        var dest = new RecipeAttachmentEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static RecipeIngredientLineEntity ReqToNew(RecipeIngredientLineReq r)
    {
        var dest = new RecipeIngredientLineEntity();
        dest.IngredientId = r.IngredientId;
        dest.IngredientText = r.IngredientText;
        dest.SortOrder = r.SortOrder;
        dest.Quantity = r.Quantity;
        dest.Unit = r.Unit;
        dest.Preparation = r.Preparation;
        dest.IsOptional = r.IsOptional;
        dest.GroupLabel = r.GroupLabel;
        return dest;
    }

    internal static RecipeStepEntity ReqToNew(RecipeStepReq r)
    {
        var dest = new RecipeStepEntity();
        dest.SortOrder = r.SortOrder;
        dest.Instruction = r.Instruction;
        dest.DurationMinutes = r.DurationMinutes;
        dest.GroupLabel = r.GroupLabel;
        return dest;
    }

    internal static RecipeCategoryRes ToRes(RecipeCategoryEntity e)
        => new() {
            Id = e.Id,
            ParentId = e.ParentId,
            Name = e.Name,
            Slug = e.Slug,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    internal static IngredientRes ToRes(IngredientEntity e)
        => new() {
            Id = e.Id,
            Name = e.Name,
            Slug = e.Slug,
            DefaultUnit = e.DefaultUnit,
            Notes = e.Notes,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    internal static RecipeRes ToRes(RecipeEntity e)
        => new() {
            Id = e.Id,
            Title = e.Title,
            Slug = e.Slug,
            Description = e.Description,
            Servings = e.Servings,
            PrepMinutes = e.PrepMinutes,
            CookMinutes = e.CookMinutes,
            Difficulty = (RecipeDifficulty)e.Difficulty,
            Status = (RecipeStatus)e.Status,
            SourceName = e.SourceName,
            SourceUrl = e.SourceUrl,
            YieldLabel = e.YieldLabel,
            CategoryId = e.CategoryId,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    internal static RecipeAttachmentRes ToRes(RecipeAttachmentEntity e)
        => new() {
            Id = e.Id,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            FileId = e.FileId,
            Role = (RecipeAttachmentRole)e.Role,
            Caption = e.Caption,
            IsPrimary = e.IsPrimary,
            SortOrder = e.SortOrder,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    internal static RecipeIngredientLineRes ToRes(RecipeIngredientLineEntity e)
        => new() {
            Id = e.Id,
            RecipeId = e.RecipeId,
            IngredientId = e.IngredientId,
            IngredientText = e.IngredientText,
            SortOrder = e.SortOrder,
            Quantity = e.Quantity,
            Unit = e.Unit,
            Preparation = e.Preparation,
            IsOptional = e.IsOptional,
            GroupLabel = e.GroupLabel
        };

    internal static RecipeStepRes ToRes(RecipeStepEntity e)
        => new() {
            Id = e.Id,
            RecipeId = e.RecipeId,
            SortOrder = e.SortOrder,
            Instruction = e.Instruction,
            DurationMinutes = e.DurationMinutes,
            GroupLabel = e.GroupLabel
        };

    internal static RecipeAttachmentRes ToRes(RecipeAttachmentRecord r)
        => new() {
            Id = r.Id,
            EntityType = r.EntityType,
            EntityId = r.EntityId,
            FileId = r.FileId,
            Role = r.Role,
            Caption = r.Caption,
            IsPrimary = r.IsPrimary,
            SortOrder = r.SortOrder,
            CreatedTimestamp = r.CreatedTimestamp,
            UpdatedTimestamp = r.UpdatedTimestamp
        };

    internal static RecipeIngredientLineRes ToRes(RecipeIngredientLineRecord r)
        => new() {
            Id = r.Id,
            RecipeId = r.RecipeId,
            IngredientId = r.IngredientId,
            IngredientText = r.IngredientText,
            SortOrder = r.SortOrder,
            Quantity = r.Quantity,
            Unit = r.Unit,
            Preparation = r.Preparation,
            IsOptional = r.IsOptional,
            GroupLabel = r.GroupLabel,
            StepIds = r.StepIds ?? []
        };

    internal static RecipeStepRes ToRes(RecipeStepRecord r)
        => new() {
            Id = r.Id,
            RecipeId = r.RecipeId,
            SortOrder = r.SortOrder,
            Instruction = r.Instruction,
            DurationMinutes = r.DurationMinutes,
            GroupLabel = r.GroupLabel
        };

    private static void Apply(RecipeCategoryReq r, RecipeCategoryEntity e)
    {
        e.ParentId = r.ParentId;
        e.Name = r.Name;
        e.Slug = r.Slug;
    }

    private static void Apply(IngredientReq r, IngredientEntity e)
    {
        e.Name = r.Name;
        e.Slug = r.Slug;
        e.DefaultUnit = r.DefaultUnit;
        e.Notes = r.Notes;
    }

    private static void Apply(RecipeReq r, RecipeEntity e)
    {
        e.Title = r.Title;
        e.Slug = r.Slug;
        e.Description = r.Description;
        e.Servings = r.Servings;
        e.PrepMinutes = r.PrepMinutes;
        e.CookMinutes = r.CookMinutes;
        e.Difficulty = (int)r.Difficulty;
        e.Status = (int)r.Status;
        e.SourceName = r.SourceName;
        e.SourceUrl = r.SourceUrl;
        e.YieldLabel = r.YieldLabel;
        e.CategoryId = r.CategoryId;
    }

    private static void Apply(RecipeAttachmentReq r, RecipeAttachmentEntity e)
    {
        e.EntityType = r.EntityType;
        e.EntityId = r.EntityId;
        e.FileId = r.FileId;
        e.Role = (int)r.Role;
        e.Caption = r.Caption;
        e.IsPrimary = r.IsPrimary;
        e.SortOrder = r.SortOrder;
    }
}
