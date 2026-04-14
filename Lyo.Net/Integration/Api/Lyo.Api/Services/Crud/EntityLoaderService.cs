using System.Linq;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lyo.Api.Services.Crud;

public class EntityLoaderService : IEntityLoaderService
{
    public async Task LoadIncludes<TContext, TDbModel>(TContext context, TDbModel entity, IEnumerable<string>? includes, CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class
    {
        if (includes == null)
            return;

        foreach (var include in includes) {
            if (!string.IsNullOrWhiteSpace(include))
                await LoadNestedCollectionsAsync(context, entity, include, ct);
        }
    }

    public IQueryable<TDbModel> LoadNestedCollections<TContext, TDbModel>(TContext context, IQueryable<TDbModel> queryable, IEnumerable<string> collectionPaths)
        where TContext : DbContext where TDbModel : class
    {
        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        if (entityType == null)
            return queryable;

        foreach (var rawPath in collectionPaths.Where(p => !string.IsNullOrWhiteSpace(p))) {
            var correctedPath = FixIncludePathCase(entityType, rawPath);
            if (correctedPath != null)
                queryable = queryable.Include(correctedPath);
        }

        return queryable;
    }

    public List<Type> GetReferencedTypes<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class
    {
        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        if (entityType == null)
            return [];

        var result = new HashSet<Type>();
        foreach (var include in includes.Where(i => !string.IsNullOrWhiteSpace(i))) {
            var currentType = entityType;
            var segments = include.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments) {
                var nav = currentType?.GetNavigations().FirstOrDefault(n => string.Equals(n.Name, segment, StringComparison.OrdinalIgnoreCase));
                if (nav == null)
                    break;

                result.Add(nav.TargetEntityType.ClrType);
                currentType = nav.TargetEntityType;
            }
        }

        return result.ToList();
    }

    public void ValidateIncludePaths<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class
    {
        var errors = CollectIncludePathErrors<TContext, TDbModel>(context, includes);
        if (errors.Count > 0)
            throw new ApiErrorException(
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Constants.ApiErrorCodes.InvalidInclude)
                    .WithMessage("One or more include paths are invalid.")
                    .AddErrors(errors.Select(e => new ApiError(e.Code, e.Message, e.StackTrace)))
                    .Build());
    }

    public IReadOnlyList<Error> CollectIncludePathErrors<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class
    {
        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        if (entityType == null)
            return [];

        var list = new List<Error>();
        foreach (var include in includes.Where(i => !string.IsNullOrWhiteSpace(i))) {
            var corrected = FixIncludePathCase(entityType, include);
            if (corrected == null) {
                list.Add(new Error(
                    $"Include path {ValidationFieldFormatter.Quote(include)} is not a valid navigation path on type '{typeof(TDbModel).Name}'.",
                    Constants.ApiErrorCodes.InvalidInclude));
            }
        }

        return list;
    }

    private static async Task LoadNestedCollectionsAsync(DbContext context, object entity, string collectionPath, CancellationToken ct = default)
    {
        var properties = collectionPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        await LoadRecursive(context, entity, properties, 0, ct);
    }

    private static async Task LoadRecursive(DbContext context, object? entity, string[] properties, int index, CancellationToken ct = default)
    {
        if (entity == null || index >= properties.Length)
            return;

        var propertyName = properties[index];
        var entry = context.Entry(entity);
        var navMetadata = entry.Metadata.GetNavigations().FirstOrDefault(n => string.Equals(n.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (navMetadata == null)
            return;

        var navigation = entry.Navigation(navMetadata.Name);
        if (!navigation.IsLoaded)
            await navigation.LoadAsync(ct);

        if (navigation.CurrentValue is IEnumerable<object> collection) {
            foreach (var item in collection)
                await LoadRecursive(context, item, properties, index + 1, ct);
        }
        else
            await LoadRecursive(context, navigation.CurrentValue, properties, index + 1, ct);
    }

    private static string? FixIncludePathCase(IEntityType entityType, string rawPath)
    {
        var segments = rawPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var correctedSegments = new List<string>();
        ITypeBase current = entityType;
        foreach (var segment in segments) {
            if (current is not IEntityType currentEntityType)
                return null;

            var match = currentEntityType.GetNavigations().FirstOrDefault(n => string.Equals(n.Name, segment, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                return null;

            correctedSegments.Add(match.Name);
            current = match.TargetEntityType;
        }

        return string.Join('.', correctedSegments);
    }
}