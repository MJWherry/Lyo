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
    private static readonly HashSet<string> AllowedMethods = new(AlgorithmExpressionHelp.MathMethods.Concat(AlgorithmExpressionHelp.ConvertMethods), StringComparer.Ordinal);

    private static readonly HashSet<string> DateMethods = new(AlgorithmExpressionHelp.DateMethods, StringComparer.Ordinal);

    private static readonly HashSet<string> DateMembers = new(AlgorithmExpressionHelp.DateMembers.Where(static name => name != "UtcDateTime"), StringComparer.Ordinal);

    private static readonly HashSet<string> TimeSpanMembers = new(AlgorithmExpressionHelp.TimeSpanMembers, StringComparer.Ordinal);

    private static readonly HashSet<string> CollectionMembers = new(AlgorithmExpressionHelp.CollectionMembers, StringComparer.Ordinal);

    private static readonly HashSet<string> Builtins = new(StringComparer.Ordinal) { "Math", "Convert", "true", "false", "null" };

    private static readonly MethodInfo CallOpen = typeof(AlgorithmCallScope).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method.Name == nameof(AlgorithmCallScope.Call) && method.IsGenericMethodDefinition);

    private readonly ConcurrentDictionary<CacheKey, object> _cache = new();

    /// <inheritdoc />
    public Result<Unit> Validate(string source) => Validate(source, AlgorithmTypeBinding.Untyped);

    /// <inheritdoc />
    public Result<Unit> Validate(string source, AlgorithmTypeBinding binding)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(source);
        ArgumentHelpers.ThrowIfNull(binding);
        try {
            var parsed = Parse(source, binding, typeof(object));
            return IsAllowed(parsed.Lambda.Expression, out var error) ? Result<Unit>.Success(default) : Result<Unit>.Failure(error!, "algorithm.expression.denied");
        }
        catch (ExpressionDeniedException ex) {
            return Result<Unit>.Failure(ex.Message, "algorithm.expression.denied");
        }
        catch (AlgorithmException ex) {
            return Result<Unit>.Failure(ex.Message, ex.Code ?? "algorithm.expression.parse");
        }
        catch (Exception ex) {
            return Result<Unit>.Failure(ex.Message, "algorithm.expression.parse");
        }
    }

    /// <inheritdoc />
    public Result<IAlgorithm<TContext, TResult>> Compile<TContext, TResult>(string source) => Compile<TContext, TResult>(source, AlgorithmTypeBinding.Untyped);

    /// <inheritdoc />
    public Result<IAlgorithm<TContext, TResult>> Compile<TContext, TResult>(string source, AlgorithmTypeBinding binding)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(source);
        ArgumentHelpers.ThrowIfNull(binding);
        var key = new CacheKey(source, typeof(TContext), typeof(TResult), binding.Canonical);
        if (_cache.TryGetValue(key, out var existing))
            return Result<IAlgorithm<TContext, TResult>>.Success((IAlgorithm<TContext, TResult>)existing);

        try {
            var algorithm = Build<TContext, TResult>(source, binding);
            _cache.TryAdd(key, algorithm);
            return Result<IAlgorithm<TContext, TResult>>.Success((IAlgorithm<TContext, TResult>)_cache[key]);
        }
        catch (ExpressionDeniedException ex) {
            return Result<IAlgorithm<TContext, TResult>>.Failure(ex.Message, "algorithm.expression.denied");
        }
        catch (AlgorithmException ex) {
            return Result<IAlgorithm<TContext, TResult>>.Failure(ex.Message, ex.Code ?? "algorithm.expression.parse");
        }
        catch (Exception ex) {
            return Result<IAlgorithm<TContext, TResult>>.Failure(ex.Message, "algorithm.expression.parse");
        }
    }

    private static IAlgorithm<TContext, TResult> Build<TContext, TResult>(string source, AlgorithmTypeBinding binding)
    {
        var parsed = Parse(source, binding, typeof(TContext));
        if (!IsAllowed(parsed.Lambda.Expression, out var error))
            throw new ExpressionDeniedException(error!);

        var context = Expression.Parameter(typeof(TContext), "context");
        var map = new Dictionary<ParameterExpression, Expression>();
        if (typeof(TContext) == typeof(AlgorithmContextBag))
            MapBag(parsed, context, map);
        else
            MapProperties<TContext>(parsed, context, map);

        MapCalls(parsed, map);
        var body = new ParameterRewriter(map).Visit(parsed.Lambda.Expression)!;
        return CompileBody<TContext, TResult>(source, context, body);
    }

    private static ParsedExpression Parse(string source, AlgorithmTypeBinding binding, Type context)
    {
        var sites = new List<AlgorithmCallSite>();
        var rewritten = AlgorithmCalls.Placeholders(source, binding.CallType, sites);
        var interpreter = CreateInterpreter();
        Parameter[] parameters;
        PropertyInfo[]? properties = null;
        if (context == typeof(AlgorithmContextBag) || context == typeof(object)) {
            var names = UnknownNames(interpreter, rewritten, sites);
            parameters = new Parameter[names.Count + sites.Count];
            for (var i = 0; i < names.Count; i++)
                parameters[i] = new Parameter(names[i], binding.ParameterType(names[i]));

            for (var i = 0; i < sites.Count; i++)
                parameters[names.Count + i] = new Parameter(sites[i].Name, sites[i].Type);
        }
        else {
            properties = context.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && AlgorithmContextBag.IsIdentifier(p.Name) && p.GetGetMethod() is not null)
                .ToArray();

            parameters = new Parameter[properties.Length + sites.Count];
            for (var i = 0; i < properties.Length; i++)
                parameters[i] = new Parameter(properties[i].Name, Nullable.GetUnderlyingType(properties[i].PropertyType) ?? properties[i].PropertyType);

            for (var i = 0; i < sites.Count; i++)
                parameters[properties.Length + i] = new Parameter(sites[i].Name, sites[i].Type);
        }

        return new(interpreter.Parse(rewritten, parameters), parameters, sites, properties);
    }

    private static void MapBag(ParsedExpression parsed, ParameterExpression context, Dictionary<ParameterExpression, Expression> map)
    {
        var bag = Expression.Convert(context, typeof(AlgorithmContextBag));
        var read = typeof(AlgorithmContextBag).GetMethod(nameof(AlgorithmContextBag.Read))!;
        foreach (var parameter in parsed.Parameters) {
            if (parsed.Sites.Any(site => site.Name == parameter.Name))
                continue;

            map[parameter.Expression] = Expression.Call(read.MakeGenericMethod(parameter.Type), bag, Expression.Constant(parameter.Name));
        }
    }

    private static void MapProperties<TContext>(ParsedExpression parsed, ParameterExpression context, Dictionary<ParameterExpression, Expression> map)
    {
        var properties = parsed.Properties ?? [];
        foreach (var parameter in parsed.Parameters) {
            if (parsed.Sites.Any(site => site.Name == parameter.Name))
                continue;

            var property = properties.First(p => p.Name == parameter.Name);
            Expression access = Expression.Property(context, property);
            if (access.Type != parameter.Type)
                access = Expression.Convert(access, parameter.Type);

            map[parameter.Expression] = access;
        }
    }

    private static void MapCalls(ParsedExpression parsed, Dictionary<ParameterExpression, Expression> map)
    {
        foreach (var site in parsed.Sites) {
            var parameter = parsed.Parameters.First(p => p.Name == site.Name);
            map[parameter.Expression] = Expression.Call(CallOpen.MakeGenericMethod(site.Type), Expression.Constant(site.Key));
        }
    }

    private static CompiledAlgorithm<TContext, TResult> CompileBody<TContext, TResult>(string key, ParameterExpression context, Expression body)
    {
        var result = body.Type == typeof(TResult) ? body : Expression.Convert(body, typeof(TResult));
        var del = Expression.Lambda<Func<TContext, TResult>>(result, context).Compile();
        var callsChildren = key.IndexOf("Algo(\"", StringComparison.Ordinal) >= 0 || key.IndexOf("score(\"", StringComparison.Ordinal) >= 0;
        return new(key, del, callsChildren);
    }

    private static List<string> UnknownNames(Interpreter interpreter, string source, List<AlgorithmCallSite> sites)
    {
        var reserved = new HashSet<string>(sites.Select(site => site.Name), StringComparer.Ordinal);
        var info = interpreter.DetectIdentifiers(source);
        var names = new List<string>();
        foreach (var name in info.UnknownIdentifiers) {
            if (Builtins.Contains(name) || reserved.Contains(name) || !AlgorithmContextBag.IsIdentifier(name))
                continue;

            names.Add(name);
        }

        return names;
    }

    private static Interpreter CreateInterpreter()
        => new Interpreter(InterpreterOptions.Default).Reference(typeof(Math)).Reference(typeof(Convert)).Reference(typeof(DateTime)).Reference(typeof(DateTimeOffset)).Reference(typeof(TimeSpan));

    private static bool IsAllowed(Expression expression, out string? error)
    {
        var visitor = new AllowlistVisitor();
        visitor.Visit(expression);
        error = visitor.Rejected;
        return error is null;
    }

    private readonly record struct CacheKey(string Source, Type Context, Type Result, string Binding);

    private sealed class ExpressionDeniedException(string message)
        : Exception(message);

    private sealed class ParsedExpression(Lambda lambda, Parameter[] parameters, List<AlgorithmCallSite> sites, PropertyInfo[]? properties)
    {
        public Lambda Lambda { get; } = lambda;

        public Parameter[] Parameters { get; } = parameters;

        public List<AlgorithmCallSite> Sites { get; } = sites;

        public PropertyInfo[]? Properties { get; } = properties;
    }

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
            var declaring = node.Method.DeclaringType;
            if (node.Method.IsSpecialName && name.StartsWith("op_", StringComparison.Ordinal))
                return base.VisitMethodCall(node);

            if (node.Method.IsSpecialName && name == "get_Item" && IsCollection(declaring))
                return base.VisitMethodCall(node);

            if (AllowedMethods.Contains(name) && declaring is not null && (declaring == typeof(Math) || declaring == typeof(Convert)))
                return base.VisitMethodCall(node);

            if (DateMethods.Contains(name) && (declaring == typeof(DateTime) || declaring == typeof(DateTimeOffset)))
                return base.VisitMethodCall(node);

            Rejected = $"Method '{name}' is not allowed in algorithm expressions.";
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (Rejected is not null)
                return node;

            if (IsAllowedMember(node.Member.DeclaringType, node.Member.Name))
                return base.VisitMember(node);

            Rejected = $"Member '{node.Member.Name}' is not allowed in algorithm expressions.";
            return node;
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

        private static bool IsAllowedMember(Type? declaring, string name)
        {
            if (declaring == typeof(Math) || declaring == typeof(Convert))
                return true;

            if ((declaring == typeof(DateTime) || declaring == typeof(DateTimeOffset)) && DateMembers.Contains(name))
                return true;

            if (declaring == typeof(DateTimeOffset) && name == "UtcDateTime")
                return true;

            if (declaring == typeof(TimeSpan) && TimeSpanMembers.Contains(name))
                return true;

            return CollectionMembers.Contains(name) && IsCollection(declaring);
        }

        private static bool IsCollection(Type? type)
        {
            if (type is null || type == typeof(string))
                return false;

            return type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
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
