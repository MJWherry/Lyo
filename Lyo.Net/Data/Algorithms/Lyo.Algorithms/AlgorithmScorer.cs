using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Lyo.Algorithms;

internal static class AlgorithmScorer
{
    public static IAlgorithm<TContext, TResult> Compile<TContext, TResult>(
        string key, AlgorithmKind kind, string payload, IExpressionCompiler compiler, Func<TContext, string, TResult> invokeChild, bool inheritResult = false)
    {
        var binding = AlgorithmTypeBinding.ParsePayload(payload);
        if (inheritResult && !binding.ResultSpecified)
            binding = binding.WithResult(typeof(TResult));

        AlgorithmHelpers.ThrowIf(binding.Result != typeof(TResult), $"Algorithm '{key}' returns {binding.Result.Name}, not {typeof(TResult).Name}.", "algorithm.result.mismatch");
        var score = kind switch {
            AlgorithmKind.DiscreteMap => Map<TContext, TResult>(key, AlgorithmPayload.ParseMap(payload)),
            AlgorithmKind.RangeBand => Bands<TContext, TResult>(key, AlgorithmPayload.ParseBands(payload), binding.Input),
            AlgorithmKind.Linear => Line<TContext, TResult>(key, AlgorithmPayload.ParseLinear(payload), binding.Input),
            AlgorithmKind.Composite => Combine(key, AlgorithmPayload.ParseComposite(payload), binding, compiler, invokeChild),
            var _ => AlgorithmHelpers.Throw<Func<TContext, TResult>>($"Algorithm kind '{kind}' is not a scorer.")
        };

        return new Scorer<TContext, TResult>(score);
    }

    private static Func<TContext, TResult> Map<TContext, TResult>(string key, DiscreteMapBody body)
    {
        var lookup = new Dictionary<string, TResult>(body.Entries.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in body.Entries)
            lookup[entry.Key] = AlgorithmValues.Convert<TResult>(entry.Points);

        var missing = body.Missing == MissingKeyPolicy.Zero ? Missing<TResult>(body.MissingValue) : default!;
        var read = ReadText<TContext>(body.Input);
        return ctx => {
            var text = read(ctx);
            if (lookup.TryGetValue(text, out var found))
                return Trace(key, AlgorithmKind.DiscreteMap, text, () => found);

            if (body.Missing == MissingKeyPolicy.Reject)
                AlgorithmHelpers.Throw<TResult>($"'{text}' is not in discrete map '{key}'.");

            return Trace(key, AlgorithmKind.DiscreteMap, text, () => missing);
        };
    }

    private static TResult Missing<TResult>(JsonElement missing)
        => AlgorithmTypeBinding.IsNumeric(typeof(TResult)) ? AlgorithmValues.Zero<TResult>() : AlgorithmValues.Convert<TResult>(missing);

    private static Func<TContext, TResult> Bands<TContext, TResult>(string key, RangeBandBody body, Type inputType)
    {
        AlgorithmHelpers.ThrowIf(!AlgorithmTypeBinding.IsNumeric(inputType), "Algorithm input type must be numeric.", "algorithm.payload.invalid");
        if (!AlgorithmTypeBinding.UseDecimal(inputType))
            return BandsDouble<TContext, TResult>(key, body, inputType);

        var compiled = BandsDecimal<TContext, TResult>(key, body, inputType);
        if (typeof(TResult) == typeof(decimal))
            return compiled;

        return compiled;
    }

    private static Func<TContext, TResult> BandsDecimal<TContext, TResult>(string key, RangeBandBody body, Type inputType)
    {
        var edges = new Edge<decimal, TResult>[body.Bands.Count];
        for (var i = 0; i < body.Bands.Count; i++) {
            var band = body.Bands[i];
            AlgorithmValues.ThrowIfNotIntegral(band.Min, inputType);
            AlgorithmValues.ThrowIfNotIntegral(band.Max, inputType);
            edges[i] = new(AlgorithmValues.Decimal(band.Min), AlgorithmValues.Decimal(band.Max), AlgorithmValues.Convert<TResult>(band.Points));
        }

        var read = ReadNumber<TContext, decimal>(body.Input, inputType);
        var missing = body.OutOfRange == OutOfRangePolicy.Zero ? Missing<TResult>(body.MissingValue) : default!;
        return ctx => {
            var value = read(ctx);
            foreach (var edge in edges) {
                if (value >= edge.Min && value < edge.Max)
                    return Trace(key, AlgorithmKind.RangeBand, ActiveText(value), () => edge.Value);
            }

            if (body.OutOfRange == OutOfRangePolicy.Reject)
                AlgorithmHelpers.Throw<TResult>($"'{value.ToString(CultureInfo.InvariantCulture)}' is outside range band '{key}'.");

            if (body.OutOfRange == OutOfRangePolicy.Nearest)
                return Trace(key, AlgorithmKind.RangeBand, ActiveText(value), () => edges[NearestDecimal(edges, value)].Value);

            return Trace(key, AlgorithmKind.RangeBand, ActiveText(value), () => missing);
        };
    }

    private static Func<TContext, TResult> BandsDouble<TContext, TResult>(string key, RangeBandBody body, Type inputType)
    {
        var edges = new Edge<double, TResult>[body.Bands.Count];
        for (var i = 0; i < body.Bands.Count; i++) {
            var band = body.Bands[i];
            edges[i] = new(AlgorithmValues.Double(band.Min), AlgorithmValues.Double(band.Max), AlgorithmValues.Convert<TResult>(band.Points));
        }

        var read = ReadNumber<TContext, double>(body.Input, inputType);
        var missing = body.OutOfRange == OutOfRangePolicy.Zero ? Missing<TResult>(body.MissingValue) : default!;
        return ctx => {
            var value = read(ctx);
            foreach (var edge in edges) {
                if (value >= edge.Min && value < edge.Max)
                    return Trace(key, AlgorithmKind.RangeBand, ActiveText(value), () => edge.Value);
            }

            if (body.OutOfRange == OutOfRangePolicy.Reject)
                AlgorithmHelpers.Throw<TResult>($"'{value.ToString(CultureInfo.InvariantCulture)}' is outside range band '{key}'.");

            if (body.OutOfRange == OutOfRangePolicy.Nearest)
                return Trace(key, AlgorithmKind.RangeBand, ActiveText(value), () => edges[NearestDouble(edges, value)].Value);

            return Trace(key, AlgorithmKind.RangeBand, ActiveText(value), () => missing);
        };
    }

    private static int NearestDecimal<TResult>(Edge<decimal, TResult>[] edges, decimal value)
    {
        var best = 0;
        var bestDistance = DecimalDistance(value, edges[0].Min, edges[0].Max);
        for (var i = 1; i < edges.Length; i++) {
            var distance = DecimalDistance(value, edges[i].Min, edges[i].Max);
            if (distance < bestDistance) {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static int NearestDouble<TResult>(Edge<double, TResult>[] edges, double value)
    {
        var best = 0;
        var bestDistance = DoubleDistance(value, edges[0].Min, edges[0].Max);
        for (var i = 1; i < edges.Length; i++) {
            var distance = DoubleDistance(value, edges[i].Min, edges[i].Max);
            if (distance < bestDistance) {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static decimal DecimalDistance(decimal value, decimal min, decimal max)
    {
        if (value < min)
            return min - value;

        if (value >= max)
            return value - max;

        return 0;
    }

    private static double DoubleDistance(double value, double min, double max)
    {
        if (value < min)
            return min - value;

        if (value >= max)
            return value - max;

        return 0;
    }

    private static Func<TContext, TResult> Line<TContext, TResult>(string key, LinearBody body, Type inputType)
    {
        AlgorithmHelpers.ThrowIf(!AlgorithmTypeBinding.IsNumeric(typeof(TResult)), "Algorithm result type must be numeric.", "algorithm.payload.invalid");
        AlgorithmHelpers.ThrowIf(!AlgorithmTypeBinding.IsNumeric(inputType), "Algorithm input type must be numeric.", "algorithm.payload.invalid");
        AlgorithmValues.ThrowIfNotIntegral(body.InputMin, inputType);
        AlgorithmValues.ThrowIfNotIntegral(body.InputMax, inputType);
        if (AlgorithmTypeBinding.UseDecimal(typeof(TResult)))
            return LineDecimal<TContext, TResult>(key, body, inputType);

        return LineDouble<TContext, TResult>(key, body, inputType);
    }

    private static Func<TContext, TResult> LineDecimal<TContext, TResult>(string key, LinearBody body, Type inputType)
    {
        var inputMin = AlgorithmValues.Decimal(body.InputMin);
        var inputMax = AlgorithmValues.Decimal(body.InputMax);
        var pointsMin = AlgorithmValues.Decimal(body.PointsMin);
        var pointsMax = AlgorithmValues.Decimal(body.PointsMax);
        var span = inputMax - inputMin;
        var pointSpan = pointsMax - pointsMin;
        var read = ReadNumber<TContext, decimal>(body.Input, inputType);
        if (typeof(TResult) == typeof(decimal)) {
            Func<TContext, decimal> core = ctx => {
                var value = read(ctx);
                var points = Scale(value, inputMin, span, pointsMin, pointSpan, body.Clamp);
                return Trace(key, AlgorithmKind.Linear, ActiveText(value), () => points);
            };

            return (Func<TContext, TResult>)(object)core;
        }

        var finish = Finish<TResult, decimal>();
        return ctx => {
            var value = read(ctx);
            var result = finish(Scale(value, inputMin, span, pointsMin, pointSpan, body.Clamp));
            return Trace(key, AlgorithmKind.Linear, ActiveText(value), () => result);
        };
    }

    private static Func<TContext, TResult> LineDouble<TContext, TResult>(string key, LinearBody body, Type inputType)
    {
        var inputMin = AlgorithmValues.Double(body.InputMin);
        var inputMax = AlgorithmValues.Double(body.InputMax);
        var pointsMin = AlgorithmValues.Double(body.PointsMin);
        var pointsMax = AlgorithmValues.Double(body.PointsMax);
        var span = inputMax - inputMin;
        var pointSpan = pointsMax - pointsMin;
        var read = ReadNumber<TContext, double>(body.Input, inputType);
        if (typeof(TResult) == typeof(double)) {
            Func<TContext, double> core = ctx => {
                var value = read(ctx);
                var points = Scale(value, inputMin, span, pointsMin, pointSpan, body.Clamp);
                return Trace(key, AlgorithmKind.Linear, ActiveText(value), () => points);
            };

            return (Func<TContext, TResult>)(object)core;
        }

        var finish = Finish<TResult, double>();
        return ctx => {
            var value = read(ctx);
            var result = finish(Scale(value, inputMin, span, pointsMin, pointSpan, body.Clamp));
            return Trace(key, AlgorithmKind.Linear, ActiveText(value), () => result);
        };
    }

    private static decimal Scale(decimal value, decimal inputMin, decimal span, decimal pointsMin, decimal pointSpan, bool clamp)
    {
        var ratio = (value - inputMin) / span;
        if (clamp) {
            if (ratio < 0)
                ratio = 0;
            else if (ratio > 1)
                ratio = 1;
        }

        return pointsMin + ratio * pointSpan;
    }

    private static double Scale(double value, double inputMin, double span, double pointsMin, double pointSpan, bool clamp)
    {
        var ratio = (value - inputMin) / span;
        if (clamp) {
            if (ratio < 0)
                ratio = 0;
            else if (ratio > 1)
                ratio = 1;
        }

        return pointsMin + ratio * pointSpan;
    }

    private static Func<TContext, TResult> Combine<TContext, TResult>(
        string key, CompositeBody body, AlgorithmTypeBinding binding, IExpressionCompiler compiler, Func<TContext, string, TResult> invokeChild)
    {
        if (body.Combiner == CombinerKind.Formula)
            return Formula(key, body, binding, compiler, invokeChild);

        AlgorithmHelpers.ThrowIf(!AlgorithmTypeBinding.IsNumeric(typeof(TResult)), "Algorithm result type must be numeric.", "algorithm.payload.invalid");
        var runs = new Func<TContext, TResult>[body.Children.Count];
        var weights = new JsonElement[body.Children.Count];
        for (var i = 0; i < body.Children.Count; i++) {
            var child = body.Children[i];
            weights[i] = child.Weight;
            if (!string.IsNullOrWhiteSpace(child.Ref)) {
                var childKey = child.Ref!;
                runs[i] = ctx => invokeChild(ctx, childKey);
            }
            else {
                runs[i] = Compile<TContext, TResult>(child.Key!, child.Kind!.Value, child.Payload.GetRawText(), compiler, invokeChild, true).Execute;
            }
        }

        var weighted = body.Combiner == CombinerKind.WeightedSum;
        var sum = Sum(runs, weights, weighted);
        return ctx => Trace(key, AlgorithmKind.Composite, null, () => sum(ctx));
    }

    private static Func<TContext, TResult> Formula<TContext, TResult>(
        string key, CompositeBody body, AlgorithmTypeBinding binding, IExpressionCompiler compiler, Func<TContext, string, TResult> invokeChild)
    {
        var inline = new Dictionary<string, Func<TContext, TResult>>(StringComparer.Ordinal);
        foreach (var child in body.Children) {
            if (string.IsNullOrWhiteSpace(child.Key))
                continue;

            inline[child.Key!] = Compile<TContext, TResult>(child.Key!, child.Kind!.Value, child.Payload.GetRawText(), compiler, invokeChild, true).Execute;
        }

        var calls = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var childKey in AlgorithmCalls.Keys(body.Formula))
            calls[childKey] = inline.ContainsKey(childKey) ? typeof(TResult) : binding.CallType(childKey);

        var formula = compiler.Compile<TContext, TResult>(body.Formula!, binding.WithCalls(calls).WithResult(typeof(TResult)));
        if (!formula.IsSuccess || formula.Data is null)
            AlgorithmHelpers.Throw(formula.Errors?[0].Message ?? "Composite formula failed to compile.", formula.Errors?[0].Code ?? "algorithm.expression.parse");

        var del = formula.Data;
        return ctx => {
            var previous = AlgorithmCallScope.Invoker;
            AlgorithmCallScope.SetInvoker(new InlineInvoker<TContext, TResult>(inline, previous, ctx));
            try {
                return Trace(key, AlgorithmKind.Composite, null, () => del.Execute(ctx));
            }
            finally {
                AlgorithmCallScope.SetInvoker(previous);
            }
        };
    }

    private static Func<TContext, TResult> Sum<TContext, TResult>(Func<TContext, TResult>[] runs, JsonElement[] weights, bool weighted)
    {
        var decimalMath = AlgorithmTypeBinding.UseDecimal(typeof(TResult));
        var totalType = decimalMath ? typeof(decimal) : typeof(double);
        var context = Expression.Parameter(typeof(TContext), "context");
        var total = Expression.Variable(totalType, "total");
        var steps = new List<Expression> { Expression.Assign(total, Expression.Constant(decimalMath ? 0m : (object)0d, totalType)) };
        for (var i = 0; i < runs.Length; i++) {
            Expression value = Expression.Invoke(Expression.Constant(runs[i]), context);
            if (value.Type != totalType)
                value = Expression.Convert(value, totalType);

            if (weighted) {
                object coefficient = decimalMath ? AlgorithmValues.WeightDecimal(weights[i]) : AlgorithmValues.WeightDouble(weights[i]);
                value = Expression.Multiply(value, Expression.Constant(coefficient, totalType));
            }

            steps.Add(Expression.Assign(total, Expression.Add(total, value)));
        }

        steps.Add(typeof(TResult) == totalType ? total : Expression.Call(FinishMethod(typeof(TResult), totalType), total));
        return Expression.Lambda<Func<TContext, TResult>>(Expression.Block([total], steps), context).Compile();
    }

    private static MethodInfo FinishMethod(Type result, Type source)
    {
        var name = result == typeof(int) ? nameof(Convert.ToInt32) :
            result == typeof(long) ? nameof(Convert.ToInt64) :
            result == typeof(short) ? nameof(Convert.ToInt16) :
            result == typeof(byte) ? nameof(Convert.ToByte) :
            result == typeof(sbyte) ? nameof(Convert.ToSByte) :
            result == typeof(uint) ? nameof(Convert.ToUInt32) :
            result == typeof(ulong) ? nameof(Convert.ToUInt64) :
            result == typeof(ushort) ? nameof(Convert.ToUInt16) :
            result == typeof(float) ? nameof(Convert.ToSingle) :
            result == typeof(double) ? nameof(Convert.ToDouble) : nameof(Convert.ToDecimal);

        return typeof(Convert).GetMethod(name, [source])!;
    }

    private static Func<TNumber, TResult> Finish<TResult, TNumber>()
    {
        var method = FinishMethod(typeof(TResult), typeof(TNumber));
        var input = Expression.Parameter(typeof(TNumber), "value");
        return Expression.Lambda<Func<TNumber, TResult>>(Expression.Call(method, input), input).Compile();
    }

    private static TResult Trace<TResult>(string key, AlgorithmKind kind, string? input, Func<TResult> execute)
        => AlgorithmTrace.IsActive ? AlgorithmTrace.Run(key, kind, input, execute, AlgorithmValues.Points, AlgorithmValues.Box) : execute();

    private static string? ActiveText<T>(T value) => AlgorithmTrace.IsActive ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static Func<TContext, string> ReadText<TContext>(string input)
    {
        if (typeof(TContext) != typeof(AlgorithmContextBag))
            return CompileText<TContext>(input);

        Func<AlgorithmContextBag, string> read = bag => {
            AlgorithmHelpers.ThrowIf(!bag.TryGetValue(input, out var value) || value is null, $"Expression identifier '{input}' is missing.");
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        };

        return (Func<TContext, string>)(object)read;
    }

    private static Func<TContext, TNumber> ReadNumber<TContext, TNumber>(string input, Type inputType)
    {
        var context = Expression.Parameter(typeof(TContext), "context");
        Expression read;
        if (typeof(TContext) == typeof(AlgorithmContextBag)) {
            var method = typeof(AlgorithmContextBag).GetMethod(nameof(AlgorithmContextBag.Read))!.MakeGenericMethod(inputType);
            read = Expression.Call(method, Expression.Convert(context, typeof(AlgorithmContextBag)), Expression.Constant(input));
        }
        else {
            var property = AlgorithmHelpers.ThrowIfNull(
                typeof(TContext).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(p => string.Equals(p.Name, input, StringComparison.OrdinalIgnoreCase) && p.GetGetMethod() is not null),
                $"'{input}' is not a property of {typeof(TContext).Name}.");

            read = Expression.Property(context, property);
            if (read.Type != inputType)
                read = Expression.Convert(read, inputType);
        }

        if (read.Type != typeof(TNumber))
            read = Expression.Convert(read, typeof(TNumber));

        return Expression.Lambda<Func<TContext, TNumber>>(read, context).Compile();
    }

    private static Func<TContext, string> CompileText<TContext>(string input)
    {
        var property = AlgorithmHelpers.ThrowIfNull(
            typeof(TContext).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, input, StringComparison.OrdinalIgnoreCase) && p.GetGetMethod() is not null),
            $"'{input}' is not a property of {typeof(TContext).Name}.");

        var context = Expression.Parameter(typeof(TContext), "context");
        var call = Expression.Call(
            typeof(Convert).GetMethod(nameof(Convert.ToString), [typeof(object), typeof(IFormatProvider)])!, Expression.Convert(Expression.Property(context, property), typeof(object)),
            Expression.Constant(CultureInfo.InvariantCulture, typeof(IFormatProvider)));

        var body = Expression.Coalesce(call, Expression.Constant(string.Empty));
        return Expression.Lambda<Func<TContext, string>>(body, context).Compile();
    }

    private readonly record struct Edge<TNumber, TResult>(TNumber Min, TNumber Max, TResult Value);

    private sealed class Scorer<TContext, TResult>(Func<TContext, TResult> score) : IAlgorithm<TContext, TResult>
    {
        public string Key => string.Empty;

        public TResult Execute(TContext context) => score(context);
    }

    private sealed class InlineInvoker<TContext, TResult>(Dictionary<string, Func<TContext, TResult>> inline, IAlgorithmInvoker? fallback, TContext context) : IAlgorithmInvoker
    {
        public T Invoke<T>(string key)
        {
            if (!inline.TryGetValue(key, out var local))
                return AlgorithmHelpers.ThrowIfNull(fallback, $"Algorithm '{key}' is not compiled.").Invoke<T>(key);

            AlgorithmHelpers.ThrowIf(typeof(T) != typeof(TResult), $"Algorithm '{key}' returns {typeof(TResult).Name}, not {typeof(T).Name}.", "algorithm.result.mismatch");
            AlgorithmCallScope.Enter(key);
            try {
                return ((Func<TContext, T>)(object)local)(context);
            }
            finally {
                AlgorithmCallScope.Exit(key);
            }
        }
    }
}
