using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;

namespace Lyo.Diff.ObjectGraph;

/// <inheritdoc />
public sealed class ObjectGraphDiffService : IObjectGraphDiffService
{
    private static readonly ObjectGraphDiffOptions DefaultOptions = new();

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <inheritdoc />
    public IReadOnlyList<ObjectGraphDifference> GetDifferences(object? left, object? right, ObjectGraphDiffOptions? options = null)
    {
        options ??= DefaultOptions;
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        var list = new List<ObjectGraphDifference>();
        var path = new PathBuilder(256);
        var stack = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Compare(left, right, 0, path, stack, options, list);
        return list;
    }

    private static void Compare(
        object? left,
        object? right,
        int depth,
        PathBuilder path,
        HashSet<object> stack,
        ObjectGraphDiffOptions options,
        List<ObjectGraphDifference> outList)
    {
        if (depth > options.MaxDepth)
            return;

        if (ReferenceEquals(left, right))
            return;

        if (left == null || right == null) {
            if (!PathAllowed(path, options))
                return;

            outList.Add(new(path.ToString(), left, right));
            return;
        }

        var leftType = left.GetType();
        var rightType = right.GetType();
        if (IsLeaf(left, right, leftType, rightType, options)) {
            if (!PathAllowed(path, options))
                return;

            if (options.CustomEquals?.Invoke(new(path.ToString(), leftType, rightType, left, right)) == true)
                return;

            if (LeafEqual(left, right, leftType, rightType))
                return;

            outList.Add(new(path.ToString(), left, right));
            return;
        }

        if (leftType.IsArray && rightType.IsArray && options.CompareArrayElements) {
            CompareArrays((Array)left, (Array)right, depth, path, stack, options, outList);
            return;
        }

        if (leftType != rightType) {
            if (!PathAllowed(path, options))
                return;

            outList.Add(new(path.ToString(), left, right));
            return;
        }

        if (!leftType.IsValueType) {
            if (!stack.Add(left)) {
                if (!PathAllowed(path, options))
                    return;

                if (ReferenceEquals(left, right))
                    return;

                outList.Add(new(path.ToString(), left, right));
                return;
            }
        }

        try {
            foreach (var prop in GetProperties(leftType, options.PropertyBindingFlags)) {
                if (!prop.CanRead)
                    continue;

                path.Push(prop.Name);
                try {
                    if (!PathAllowed(path, options))
                        continue;

                    object? lv;
                    object? rv;
                    try {
                        lv = prop.GetValue(left);
                        rv = prop.GetValue(right);
                    }
                    catch {
                        path.Pop();
                        continue;
                    }

                    Compare(lv, rv, depth + 1, path, stack, options, outList);
                }
                finally {
                    path.Pop();
                }
            }
        }
        finally {
            if (!leftType.IsValueType)
                stack.Remove(left);
        }
    }

    private static void CompareArrays(
        Array left,
        Array right,
        int depth,
        PathBuilder path,
        HashSet<object> stack,
        ObjectGraphDiffOptions options,
        List<ObjectGraphDifference> outList)
    {
        if (left.Rank != 1 || right.Rank != 1) {
            if (!PathAllowed(path, options))
                return;

            if (!ReferenceEquals(left, right))
                outList.Add(new(path.ToString(), left, right));

            return;
        }

        var n = left.Length;
        var m = right.Length;
        var len = Math.Min(n, m);
        for (var i = 0; i < len; i++) {
            path.PushIndex(i);
            try {
                Compare(left.GetValue(i), right.GetValue(i), depth + 1, path, stack, options, outList);
            }
            finally {
                path.Pop();
            }
        }

        if (n == m)
            return;

        if (!PathAllowed(path, options))
            return;

        for (var i = len; i < Math.Max(n, m); i++) {
            path.PushIndex(i);
            try {
                var lv = n > m ? left.GetValue(i) : null;
                var rv = m > n ? right.GetValue(i) : null;
                Compare(lv, rv, depth + 1, path, stack, options, outList);
            }
            finally {
                path.Pop();
            }
        }
    }

    private static bool PathAllowed(PathBuilder path, ObjectGraphDiffOptions options)
    {
        var s = path.ToString();
        if (options.IncludePath != null && !options.IncludePath(s))
            return false;

        if (options.ExcludePath != null && options.ExcludePath(s))
            return false;

        return true;
    }

    private static bool IsLeaf(object left, object right, Type leftType, Type rightType, ObjectGraphDiffOptions options)
    {
        if (leftType.IsPrimitive || rightType.IsPrimitive)
            return true;

        if (leftType == typeof(string) || rightType == typeof(string))
            return true;

        if (leftType == typeof(decimal) || rightType == typeof(decimal))
            return true;

        if (leftType == typeof(DateTime) || rightType == typeof(DateTime))
            return true;

        if (leftType == typeof(DateTimeOffset) || rightType == typeof(DateTimeOffset))
            return true;

        if (leftType == typeof(TimeSpan) || rightType == typeof(TimeSpan))
            return true;

        if (leftType == typeof(Guid) || rightType == typeof(Guid))
            return true;

        if (leftType.IsEnum || rightType.IsEnum)
            return true;

        if (IsNullableOfEnumOrLeaf(leftType) || IsNullableOfEnumOrLeaf(rightType))
            return true;

        if (leftType.IsArray && rightType.IsArray && options.CompareArrayElements)
            return false;

        return false;
    }

    private static bool IsNullableOfEnumOrLeaf(Type t)
    {
        var u = Nullable.GetUnderlyingType(t);
        return u != null && (u.IsEnum || u.IsPrimitive || u == typeof(decimal) || u == typeof(DateTime) || u == typeof(Guid));
    }

    private static bool LeafEqual(object left, object right, Type leftType, Type rightType)
    {
        if (leftType.IsEnum && rightType.IsEnum)
            return Equals(left, right);

        if (leftType.IsEnum != rightType.IsEnum) {
            if (leftType.IsEnum && rightType == typeof(string))
                return string.Equals(left.ToString(), (string)right, StringComparison.Ordinal);

            if (rightType.IsEnum && leftType == typeof(string))
                return string.Equals(right.ToString(), (string)left, StringComparison.Ordinal);
        }

        return Equals(left, right);
    }

    private static PropertyInfo[] GetProperties(Type type, BindingFlags flags) => PropertyCache.GetOrAdd(type, t => t.GetProperties(flags));

    private sealed class PathBuilder(int capacity)
    {
        private readonly List<string> _segments = new(capacity);

        public void Push(string name) => _segments.Add(name);

        public void PushIndex(int index) => _segments.Add(index.ToString());

        public void Pop()
        {
            if (_segments.Count > 0)
                _segments.RemoveAt(_segments.Count - 1);
        }

        public override string ToString() => string.Join(".", _segments);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}