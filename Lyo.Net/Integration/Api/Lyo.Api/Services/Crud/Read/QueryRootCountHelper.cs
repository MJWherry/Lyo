using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read;

/// <summary>
/// Counts root entities without inflating totals when filters use joins that duplicate parent rows.
/// </summary>
public static class QueryRootCountHelper
{
    /// <summary>
    /// Returns the number of distinct root entities matching <paramref name="queryable" />.
    /// For a single-column primary key this uses <c>SELECT COUNT(*) FROM (SELECT DISTINCT pk ...)</c>.
    /// Composite keys fall back to <see cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource},CancellationToken)" />.
    /// </summary>
    public static Task<int> CountDistinctRootEntitiesAsync<TDbModel>(
        DbContext context,
        IQueryable<TDbModel> queryable,
        CancellationToken ct)
        where TDbModel : class
    {
        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        var pk = entityType?.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1)
            return queryable.CountAsync(ct);

        var keyClrType = pk.Properties[0].ClrType;
        var method = typeof(QueryRootCountHelper).GetMethod(nameof(CountDistinctSinglePkAsync), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(TDbModel), keyClrType);

        if (method == null)
            return queryable.CountAsync(ct);

        var keyName = pk.Properties[0].Name;
        return (Task<int>)method.Invoke(null, [queryable, keyName, ct])!;
    }

    private static Task<int> CountDistinctSinglePkAsync<TDbModel, TKey>(
        IQueryable<TDbModel> queryable,
        string keyName,
        CancellationToken ct)
        where TDbModel : class
    {
        var keySelector = QueryKeyExpressionBuilder.BuildEfKeySelector<TDbModel, TKey>(keyName);
        return queryable.Select(keySelector).Distinct().CountAsync(ct);
    }
}
