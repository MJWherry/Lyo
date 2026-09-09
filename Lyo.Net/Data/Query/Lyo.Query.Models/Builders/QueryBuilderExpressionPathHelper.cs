using System.Linq.Expressions;
using Lyo.Common.Core;
using Lyo.Query.Models.Attributes;

namespace Lyo.Query.Models.Builders;

/// <summary>Resolves dotted query paths from LINQ expression lambdas, including a <c>Count()</c> suffix mapped to a <c>Count</c> segment.</summary>
internal static class QueryBuilderExpressionPathHelper
{
    /// <summary>Dotted property path from <paramref name="expr" />, honoring <see cref="QueryPropertyNameAttribute" /> on properties.</summary>
    public static string GetPropertyPath(LambdaExpression? expr)
    {
        if (expr == null)
            return string.Empty;

        var body = expr.Body;
        if (body is UnaryExpression ue && ue.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            body = ue.Operand;

        var appendCount = false;
        if (body is MethodCallExpression mce) {
            var methodName = mce.Method.Name;
            if (string.Equals(methodName, "Count", StringComparison.Ordinal) || string.Equals(methodName, "LongCount", StringComparison.Ordinal)) {
                if (mce.Arguments.Count >= 1) {
                    body = mce.Arguments[0];
                    appendCount = true;
                    if (body is UnaryExpression ue2 && ue2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                        body = ue2.Operand;
                }
            }
        }

        var path = body.TryGetMemberPath() ?? string.Empty;
        var names = new List<string>();
        var currentType = expr.Parameters.Count > 0 ? expr.Parameters[0].Type : null;
        foreach (var segment in path.Split('.')) {
            var trimmed = segment.Trim();
            if (trimmed.Length == 0)
                continue;

            if (currentType is not null) {
                var pi = currentType.FindProperty(trimmed);
                if (pi is not null) {
                    var queryAttr = pi.GetAttribute<QueryPropertyNameAttribute>();
                    var queryName = queryAttr?.PropertyName;
                    names.Add(!string.IsNullOrEmpty(queryName) ? queryName : pi.Name);
                    currentType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
                    continue;
                }
            }

            names.Add(trimmed);
        }

        if (appendCount)
            names.Add("Count");

        return string.Join(".", names);
    }
}
