using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Lyo.Search.Postgres;

/// <summary>Maps one stored <c>tsvector</c> column and filters rows with <c>plainto_tsquery</c>.</summary>
public static class PostgresTextSearch
{
    /// <summary>English vector over a display name: preferred name when set, otherwise given name and family name.</summary>
    public const string EnglishDisplayName = "to_tsvector('english', COALESCE(NULLIF(preferred_name, ''), btrim(COALESCE(first_name, '') || ' ' || COALESCE(last_name, ''))))";

    /// <summary>English vector over one text column. <paramref name="column" /> is the database column name.</summary>
    public static string EnglishColumn(string column) => $"to_tsvector('english', COALESCE({column}, ''))";

    /// <summary>Adds <c>search_vector</c> as a stored computed column and a GIN index.</summary>
    public static void Map<TEntity>(EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, NpgsqlTsVector>> vector, string computedSql, string indexName)
        where TEntity : class
    {
        builder.Property(vector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(computedSql, true);
        var property = vector.Body is MemberExpression member ? member.Member.Name : "SearchVector";
        builder.HasIndex(property).HasMethod("GIN").HasDatabaseName(indexName);
    }

    /// <summary>Keeps rows whose <c>SearchVector</c> matches <paramref name="term" />.</summary>
    public static IQueryable<TEntity> WhereMatches<TEntity>(this IQueryable<TEntity> query, string term)
        where TEntity : class
        => query.Where(entity => EF.Property<NpgsqlTsVector>(entity, "SearchVector").Matches(EF.Functions.PlainToTsQuery("english", term)));
}