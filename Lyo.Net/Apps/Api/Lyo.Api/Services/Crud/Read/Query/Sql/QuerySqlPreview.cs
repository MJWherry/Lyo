using Lyo.Api.Models.Common.Response;
using Lyo.Api.Services.Crud.Read.Query.Root;
using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lyo.Api.Services.Crud.Read.Query.Sql;

/// <summary>Builds a <see cref="QuerySqlRes" /> from a query request and EF table metadata. Does not execute the statement.</summary>
public static class QuerySqlPreview
{
    private static readonly SqlQueryTranslator Translator = new();

    /// <summary>Prints a concrete query for <paramref name="entity" />.</summary>
    public static QuerySqlRes Translate(QueryConcreteReq request, IEntityType entity)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(entity);
        return ToResponse(Translator.Translate(request, EfSqlColumnMap.FromEntity(entity)));
    }

    /// <summary>Prints a projection query for <paramref name="entity" />.</summary>
    public static QuerySqlRes Translate(ProjectionQueryReq request, IEntityType entity)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(entity);
        return ToResponse(Translator.Translate(request, EfSqlColumnMap.FromEntity(entity)));
    }

    /// <summary>Prints a root query using every entity in <paramref name="registry" />.</summary>
    public static QuerySqlRes Translate(QueryReq request, RootQueryEntityRegistry registry)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(registry);
        var catalog = new Dictionary<string, SqlColumnMap>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in registry.EntityTypeNames) {
            if (!registry.TryGet(name, out var entry))
                continue;

            catalog[name] = EfSqlColumnMap.FromEntity(entry.EfEntityType);
        }

        return ToResponse(Translator.Translate(request, catalog));
    }

    /// <summary>Schema, table, and store column names for <paramref name="entity" />.</summary>
    public static SqlColumnMap ColumnMap(IEntityType entity) => EfSqlColumnMap.FromEntity(entity);

    private static QuerySqlRes ToResponse(SqlQuery query)
    {
        var parameters = new List<QuerySqlParameter>(query.Parameters.Count);
        foreach (var parameter in query.Parameters)
            parameters.Add(new() { Name = parameter.Name, Value = parameter.Value });

        return new() { Sql = query.Text, Parameters = parameters };
    }
}

/// <summary>Reads schema, table, and column names from an EF entity type. Does not create an <c>IQueryable</c>.</summary>
internal static class EfSqlColumnMap
{
    public static SqlColumnMap FromEntity(IEntityType entity)
    {
        ArgumentHelpers.ThrowIfNull(entity);
        var table = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);
        if (table is null)
            throw new SqlTranslationException($"Entity '{entity.ClrType.Name}' has no table mapping.");

        var columns = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in entity.GetProperties()) {
            var column = property.GetColumnName(table.Value);
            if (string.IsNullOrEmpty(column))
                continue;

            columns[property.Name] = column;
        }

        return new(table.Value.Schema ?? "", table.Value.Name, columns);
    }
}
