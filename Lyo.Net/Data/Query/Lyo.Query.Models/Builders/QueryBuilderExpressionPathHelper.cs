using System.Linq.Expressions;
using System.Reflection;
using Lyo.Query.Models.Attributes;

namespace Lyo.Query.Models.Builders;

internal static class QueryBuilderExpressionPathHelper
{
    public static string GetPropertyPath(LambdaExpression? expr)
    {
        if (expr == null)
            return string.Empty;

        var body = expr.Body;
        if (body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
            body = ue.Operand;

        var appendCount = false;
        if (body is MethodCallExpression mce) {
            var methodName = mce.Method.Name;
            if (string.Equals(methodName, "Count", StringComparison.Ordinal) || string.Equals(methodName, "LongCount", StringComparison.Ordinal)) {
                if (mce.Arguments.Count >= 1) {
                    body = mce.Arguments[0];
                    appendCount = true;
                    if (body is UnaryExpression ue2 && ue2.NodeType == ExpressionType.Convert)
                        body = ue2.Operand;
                }
            }
        }

        var memberInfos = new List<MemberInfo>();
        while (body is MemberExpression me) {
            memberInfos.Insert(0, me.Member);
            body = me.Expression!;
            if (body is UnaryExpression u2 && u2.NodeType == ExpressionType.Convert)
                body = u2.Operand;
        }

        var names = new List<string>();
        foreach (var mi in memberInfos) {
            if (mi is PropertyInfo pi) {
                var queryAttr = pi.GetCustomAttribute<QueryPropertyNameAttribute>(true);
                if (queryAttr != null && !string.IsNullOrEmpty(queryAttr.PropertyName)) {
                    names.Add(queryAttr.PropertyName);
                    continue;
                }
            }

            names.Add(mi.Name);
        }

        if (appendCount)
            names.Add("Count");

        return string.Join(".", names);
    }
}
