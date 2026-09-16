using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Query.Postgres;

/// <summary>
/// Replaces <see cref="FuzzyMatch.Similarity" /> calls with <see cref="NpgsqlTrigramsDbFunctionsExtensions.TrigramsSimilarity" /> so Npgsql emits SQL <c>similarity()</c>.
/// </summary>
internal sealed class FuzzyToNpgsqlRewriter : ExpressionVisitor
{
    internal static readonly FuzzyToNpgsqlRewriter Instance = new();

    private static readonly MethodInfo TrigramsSimilarityMethod = typeof(NpgsqlTrigramsDbFunctionsExtensions).GetMethod(
        nameof(NpgsqlTrigramsDbFunctionsExtensions.TrigramsSimilarity),
        [typeof(DbFunctions), typeof(string), typeof(string)])!;

    private static readonly Expression EfFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method == FuzzyMatch.SimilarityMethod)
            return Expression.Call(TrigramsSimilarityMethod, EfFunctions, Visit(node.Arguments[0]), Visit(node.Arguments[1]));

        return base.VisitMethodCall(node);
    }
}
