using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using DynamicExpresso;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Algorithms;

/// <summary>DynamicExpresso compiler with a math allowlist. Each formula becomes one <see cref="Func{T,TResult}" /> and the interpreter is dropped.</summary>
public sealed class ExpressionCompiler : IExpressionCompiler
{
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.Ordinal) {
        "Abs",
        "Min",
        "Max",
        "Round",
        "Floor",
        "Ceiling",
        "Pow",
        "Sqrt",
        "Sign",
        "ToDouble",
        "ToDecimal",
        "ToInt32",
        "ToInt64"
    };

    private static readonly HashSet<string> Builtins = new(StringComparer.Ordinal) {
        "Math",
        "Convert",
        "true",
        "false",
        "null"
    };

    private readonly ConcurrentDictionary<CacheKey, object> _cache = new();

    /// <inheritdoc />
    public Result<Unit> Validate(string source)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(source);
        try {
            var lambda = ParseLoose(source);
            return IsAllowed(lambda.Expression, out var error) ? Result<Unit>.Success(default) : Result<Unit>.Failure(error!, "algorithm.expression.denied");
        }
        catch (Exception ex) {
            return Result<Unit>.Failure(ex.Message, "algorithm.expression.parse");
        }
    }

    /// <inheritdoc />
    public Result<IAlgorithm<TContext, TResult>> Compile<TContext, TResult>(string source)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(source);
        var key = new CacheKey(source, typeof(TContext), typeof(TResult));
        if (_cache.TryGetValue(key, out var existing))
            return Result<IAlgorithm<TContext, TResult>>.Success((IAlgorithm<TContext, TResult>)existing);

        try {
            var algorithm = Build<TContext, TResult>(source);
            _cache.TryAdd(key, algorithm);
            return Result<IAlgorithm<TContext, TResult>>.Success((IAlgorithm<TContext, TResult>)_cache[key]);
        }
        catch (ExpressionDeniedException ex) {
            return Result<IAlgorithm<TContext, TResult>>.Failure(ex.Message, "algorithm.expression.denied");
        }
        catch (Exception ex) {
            return Result<IAlgorithm<TContext, TResult>>.Failure(ex.Message, "algorithm.expression.parse");
        }
    }

    private static IAlgorithm<TContext, TResult> Build<TContext, TResult>(string source)
    {
        if (typeof(TContext) == typeof(AlgorithmContextBag))
            return BuildBag<TContext, TResult>(source);

        var properties = typeof(TContext).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && AlgorithmContextBag.IsIdentifier(p.Name) && p.GetGetMethod() is not null)
            .ToArray();

        var interpreter = CreateInterpreter();
        var parameters = properties.Select(p => new Parameter(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType)).ToArray();
        var lambda = interpreter.Parse(source, parameters);
        if (!IsAllowed(lambda.Expression, out var error))
            throw new ExpressionDeniedException(error!);

        var context = Expression.Parameter(typeof(TContext), "context");
        var map = new Dictionary<ParameterExpression, Expression>(parameters.Length);
        for (var i = 0; i < parameters.Length; i++) {
            Expression access = Expression.Property(context, properties[i]);
            if (access.Type != parameters[i].Type)
                access = Expression.Convert(access, parameters[i].Type);

            map[parameters[i].Expression] = access;
        }

        var body = new ParameterRewriter(map).Visit(lambda.Expression)!;
        return CompileBody<TContext, TResult>(source, context, body);
    }

    private static IAlgorithm<TContext, TResult> BuildBag<TContext, TResult>(string source)
    {
        var interpreter = CreateInterpreter();
        var names = UnknownNames(interpreter, source);
        var parameters = names.Select(ParameterFor).ToArray();
        var lambda = interpreter.Parse(source, parameters);
        if (!IsAllowed(lambda.Expression, out var error))
            throw new ExpressionDeniedException(error!);

        var context = Expression.Parameter(typeof(TContext), "context");
        var bag = Expression.Convert(context, typeof(AlgorithmContextBag));
        var map = new Dictionary<ParameterExpression, Expression>(parameters.Length);
        var readDouble = typeof(AlgorithmContextBag).GetMethod(nameof(AlgorithmContextBag.ReadDouble), BindingFlags.Public | BindingFlags.Static)!;
        var readString = typeof(AlgorithmContextBag).GetMethod(nameof(AlgorithmContextBag.ReadString), BindingFlags.Public | BindingFlags.Static)!;
        foreach (var parameter in parameters) {
            var read = parameter.Type == typeof(string) ? readString : readDouble;
            map[parameter.Expression] = Expression.Call(read, bag, Expression.Constant(parameter.Name));
        }

        var body = new ParameterRewriter(map).Visit(lambda.Expression)!;
        return CompileBody<TContext, TResult>(source, context, body);
    }

    private static CompiledAlgorithm<TContext, TResult> CompileBody<TContext, TResult>(string key, ParameterExpression context, Expression body)
    {
        var result = body.Type == typeof(TResult) ? body : Expression.Convert(body, typeof(TResult));
        var del = Expression.Lambda<Func<TContext, TResult>>(result, context).Compile();
        var callsChildren = key.IndexOf("Algo(\"", StringComparison.Ordinal) >= 0 || key.IndexOf("score(\"", StringComparison.Ordinal) >= 0;
        return new(key, del, callsChildren);
    }

    private static Lambda ParseLoose(string source)
    {
        var interpreter = CreateInterpreter();
        var names = UnknownNames(interpreter, source);
        var parameters = names.Select(ParameterFor).ToArray();
        return interpreter.Parse(source, parameters);
    }

    private static Parameter ParameterFor(string name) => new(name, name.EndsWith("_key", StringComparison.Ordinal) ? typeof(string) : typeof(double));

    private static List<string> UnknownNames(Interpreter interpreter, string source)
    {
        var info = interpreter.DetectIdentifiers(source);
        var names = new List<string>();
        foreach (var name in info.UnknownIdentifiers) {
            if (Builtins.Contains(name) || !AlgorithmContextBag.IsIdentifier(name))
                continue;

            names.Add(name);
        }

        return names;
    }

    private static Interpreter CreateInterpreter()
        => new Interpreter(InterpreterOptions.Default).Reference(typeof(Math))
            .Reference(typeof(Convert))
            .SetFunction("Algo", (Func<string, double>)AlgorithmCallScope.Call)
            .SetFunction("score", (Func<string, double>)AlgorithmCallScope.Call);

    private static bool IsAllowed(Expression expression, out string? error)
    {
        var visitor = new AllowlistVisitor();
        visitor.Visit(expression);
        error = visitor.Rejected;
        return error is null;
    }

    private readonly record struct CacheKey(string Source, Type Context, Type Result);

    private sealed class ExpressionDeniedException(string message)
        : Exception(message);

    private sealed class ParameterRewriter(Dictionary<ParameterExpression, Expression> map) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => map.TryGetValue(node, out var replacement) ? replacement : base.VisitParameter(node);
    }

    private sealed class AllowlistVisitor : ExpressionVisitor
    {
        public string? Rejected { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (Rejected is not null)
                return node;

            var name = node.Method.Name;
            if (name is "Algo" or "score")
                return base.VisitMethodCall(node);

            if (node.Method.IsSpecialName && (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("op_", StringComparison.Ordinal)))
                return base.VisitMethodCall(node);

            if (!AllowedMethods.Contains(name)) {
                Rejected = $"Method '{name}' is not allowed in algorithm expressions.";
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (Rejected is not null)
                return node;

            var declaring = node.Member.DeclaringType;
            if (declaring != typeof(Math) && declaring != typeof(Convert)) {
                Rejected = $"Member '{node.Member.Name}' is not allowed in algorithm expressions.";
                return node;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (Rejected is not null)
                return node;

            var type = node.Type;
            if (type == typeof(decimal) || type.IsPrimitive)
                return base.VisitNew(node);

            Rejected = $"Creating '{type.Name}' is not allowed in algorithm expressions.";
            return node;
        }
    }

    private sealed class CompiledAlgorithm<TContext, TResult>(string key, Func<TContext, TResult> execute, bool callsChildren) : IAlgorithm<TContext, TResult>, ICallsChildren
    {
        public string Key { get; } = key;

        public TResult Execute(TContext context) => execute(context);

        public bool CallsChildren { get; } = callsChildren;
    }
}

internal interface ICallsChildren
{
    bool CallsChildren { get; }
}