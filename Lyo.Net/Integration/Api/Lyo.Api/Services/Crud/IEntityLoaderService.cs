using Lyo.Result;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud;

/// <summary>EF navigation loading, include path validation, and referenced-type discovery for query caching and includes.</summary>
public interface IEntityLoaderService
{
    /// <summary>Eager-loads navigation paths on an already-attached or tracked entity.</summary>
    Task LoadIncludes<TContext, TDbModel>(TContext context, TDbModel entity, IEnumerable<string>? includes, CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;

    /// <summary>Applies split-query style loading for nested collection paths on an <see cref="IQueryable{T}" />.</summary>
    IQueryable<TDbModel> LoadNestedCollections<TContext, TDbModel>(TContext context, IQueryable<TDbModel> queryable, IEnumerable<string> collectionPaths)
        where TContext : DbContext where TDbModel : class;

    /// <summary>Returns distinct EF entity CLR types reachable from <paramref name="includes" /> (for cache tags and invalidation).</summary>
    List<Type> GetReferencedTypes<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class;

    /// <summary>Validates that all include paths are valid navigation paths. Throws <see cref="ArgumentException" /> for any invalid path.</summary>
    void ValidateIncludePaths<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class;

    /// <summary>Returns one <see cref="Error" /> per invalid include path (code <c>InvalidInclude</c>).</summary>
    IReadOnlyList<Error> CollectIncludePathErrors<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class;
}