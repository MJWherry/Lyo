using Lyo.Common;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud;

public interface IEntityLoaderService
{
    Task LoadIncludes<TContext, TDbModel>(TContext context, TDbModel entity, IEnumerable<string>? includes, CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;

    IQueryable<TDbModel> LoadNestedCollections<TContext, TDbModel>(TContext context, IQueryable<TDbModel> queryable, IEnumerable<string> collectionPaths)
        where TContext : DbContext where TDbModel : class;

    List<Type> GetReferencedTypes<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class;

    /// <summary>Validates that all include paths are valid navigation paths. Throws <see cref="ArgumentException" /> for any invalid path.</summary>
    void ValidateIncludePaths<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class;

    /// <summary>Returns one <see cref="Error" /> per invalid include path (code <c>InvalidInclude</c>).</summary>
    IReadOnlyList<Error> CollectIncludePathErrors<TContext, TDbModel>(TContext context, IEnumerable<string> includes)
        where TContext : DbContext where TDbModel : class;
}