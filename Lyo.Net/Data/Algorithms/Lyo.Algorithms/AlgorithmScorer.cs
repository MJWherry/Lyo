using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Lyo.Algorithms;

internal static class AlgorithmScorer
{
    public static IAlgorithm<TContext, TResult> Compile<TContext, TResult>(
        string key,
        AlgorithmKind kind,
        string payload,
        IExpressionCompiler compiler,
        Func<TContext, string, double> invokeChild)
    {
        var score = kind switch {
            AlgorithmKind.DiscreteMap => Map<TContext>(key, AlgorithmPayload.ParseMap(payload)),
            AlgorithmKind.RangeBand => Bands<TContext>(key, AlgorithmPayload.ParseBands(payload)),
            AlgorithmKind.Linear => Line<TContext>(key, AlgorithmPayload.ParseLinear(payload)),
            AlgorithmKind.Composite => Combine(key, AlgorithmPayload.ParseComposite(payload), compiler, invokeChild),
            var _ => AlgorithmHelpers.Throw<Func<TContext, double>>($"Algorithm kind '{kind}' is not a scorer.")
        };

        return new Scorer<TContext, TResult>(key, kind, score);
    }

    private static Func<TContext, double> Map<TContext>(string key, DiscreteMapBody body)
    {
        var lookup = new Dictionary<string, double>(body.Entries.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in body.Entries)
            lookup[entry.Key] = entry.Points;

        var read = ReadText<TContext>(body.Input);
        return ctx => {
            var text = read(ctx);
            if (lookup.TryGetValue(text, out var points))
                return Trace(key, AlgorithmKind.DiscreteMap, text, () => points);

            if (body.Missing == MissingKeyPolicy.Reject)
                AlgorithmHelpers.Throw($"'{text}' is not in discrete map '{key}'.");

            points = 0;
            return Trace(key, AlgorithmKind.DiscreteMap, text, () => points);
        };
    }

    private static Func<TContext, double> Bands<TContext>(string key, RangeBandBody body)
    {
        var bands = body.Bands;
        var read = ReadNumber<TContext>(body.Input);
        return ctx => {
            var value = read(ctx);
            double points = 0;
            var matched = false;
            foreach (var band in bands) {
                if (value >= band.Min && value < band.Max) {
                    points = band.Points;
                    matched = true;
                    break;
                }
            }

            if (!matched) {
                points = body.OutOfRange switch {
                    OutOfRangePolicy.Reject => AlgorithmHelpers.Throw<double>($"'{value.ToString(CultureInfo.InvariantCulture)}' is outside range band '{key}'."),
                    OutOfRangePolicy.Nearest => Nearest(bands, value),
                    var _ => 0
                };
            }

            return Trace(key, AlgorithmKind.RangeBand, value.ToString(CultureInfo.InvariantCulture), () => points);
        };
    }

    private static double Nearest(List<RangeBandEntry> bands, double value)
    {
        var best = bands[0];
        var bestDistance = Distance(value, best);
        for (var i = 1; i < bands.Count; i++) {
            var distance = Distance(value, bands[i]);
            if (!(distance < bestDistance))
                continue;

            best = bands[i];
            bestDistance = distance;
        }

        return best.Points;
    }

    private static double Distance(double value, RangeBandEntry band)
    {
        if (value < band.Min)
            return band.Min - value;

        if (value >= band.Max)
            return value - band.Max;

        return 0;
    }

    private static Func<TContext, double> Line<TContext>(string key, LinearBody body)
    {
        var read = ReadNumber<TContext>(body.Input);
        var span = body.InputMax - body.InputMin;
        var pointSpan = body.PointsMax - body.PointsMin;
        return ctx => {
            var value = read(ctx);
            var t = (value - body.InputMin) / span;
            if (body.Clamp) {
                if (t < 0)
                    t = 0;
                else if (t > 1)
                    t = 1;
            }

            var points = body.PointsMin + t * pointSpan;
            return Trace(key, AlgorithmKind.Linear, value.ToString(CultureInfo.InvariantCulture), () => points);
        };
    }

    private static Func<TContext, double> Combine<TContext>(string key, CompositeBody body, IExpressionCompiler compiler, Func<TContext, string, double> invokeChild)
    {
        var inline = new Dictionary<string, Func<TContext, double>>(StringComparer.Ordinal);
        var parts = new List<(Func<TContext, double> Run, double Weight)>(body.Children.Count);
        foreach (var child in body.Children) {
            Func<TContext, double> run;
            if (!string.IsNullOrWhiteSpace(child.Ref))
                run = ctx => invokeChild(ctx, child.Ref!);
            else {
                var childKey = child.Key!;
                var compiled = Compile<TContext, double>(childKey, child.Kind!.Value, child.Payload.GetRawText(), compiler, invokeChild);
                run = compiled.Execute;
                inline[childKey] = run;
            }

            parts.Add((run, child.Weight));
        }

        if (body.Combiner == CombinerKind.Formula) {
            var formula = compiler.Compile<TContext, double>(body.Formula!);
            if (!formula.IsSuccess || formula.Data is null)
                AlgorithmHelpers.Throw(formula.Errors?[0].Message ?? "Composite formula failed to compile.", "algorithm.expression.parse");

            var del = formula.Data;
            return ctx => {
                var previous = AlgorithmCallScope.Invoker;
                AlgorithmCallScope.SetInvoker(name => {
                    if (!inline.TryGetValue(name, out var local))
                        return invokeChild(ctx, name);

                    AlgorithmCallScope.Enter(name);
                    try {
                        return local(ctx);
                    }
                    finally {
                        AlgorithmCallScope.Exit(name);
                    }
                });

                try {
                    return Trace(key, AlgorithmKind.Composite, null, () => del.Execute(ctx));
                }
                finally {
                    AlgorithmCallScope.SetInvoker(previous);
                }
            };
        }

        var weighted = body.Combiner == CombinerKind.WeightedSum;
        return ctx => Trace(
            key, AlgorithmKind.Composite, null, () => {
                var total = 0d;
                foreach (var part in parts)
                    total += part.Run(ctx) * (weighted ? part.Weight : 1d);

                return total;
            });
    }

    private static double Trace(string key, AlgorithmKind kind, string? input, Func<double> execute)
        => AlgorithmTrace.IsActive ? AlgorithmTrace.Run(key, kind, input, execute, static value => value) : execute();

    private static Func<TContext, string> ReadText<TContext>(string input)
    {
        if (typeof(TContext) != typeof(AlgorithmContextBag))
            return CompileProperty<TContext, string>(input, true);

        Func<AlgorithmContextBag, string> read = bag => {
            AlgorithmHelpers.ThrowIf(!bag.TryGetValue(input, out var value) || value is null, $"Expression identifier '{input}' is missing.");
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        };

        return (Func<TContext, string>)(object)read;
    }

    private static Func<TContext, double> ReadNumber<TContext>(string input)
    {
        if (typeof(TContext) != typeof(AlgorithmContextBag))
            return CompileProperty<TContext, double>(input, false);

        Func<AlgorithmContextBag, double> read = bag => AlgorithmContextBag.ReadDouble(bag, input);
        return (Func<TContext, double>)(object)read;
    }

    private static Func<TContext, TValue> CompileProperty<TContext, TValue>(string input, bool asText)
    {
        var property = AlgorithmHelpers.ThrowIfNull(
            typeof(TContext).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, input, StringComparison.OrdinalIgnoreCase) && p.GetGetMethod() is not null),
            $"'{input}' is not a property of {typeof(TContext).Name}.");

        var context = Expression.Parameter(typeof(TContext), "context");
        Expression access = Expression.Property(context, property);
        Expression body = asText
            ? Expression.Call(
                typeof(Convert).GetMethod(nameof(Convert.ToString), [typeof(object), typeof(IFormatProvider)])!, Expression.Convert(access, typeof(object)),
                Expression.Constant(CultureInfo.InvariantCulture, typeof(IFormatProvider)))
            : Expression.Convert(access, typeof(double));

        if (asText && body.Type != typeof(TValue))
            body = Expression.Coalesce(body, Expression.Constant(string.Empty));

        return Expression.Lambda<Func<TContext, TValue>>(body, context).Compile();
    }

    private sealed class Scorer<TContext, TResult>(string key, AlgorithmKind kind, Func<TContext, double> score) : IAlgorithm<TContext, TResult>
    {
        public AlgorithmKind Kind { get; } = kind;

        public string Key { get; } = key;

        public TResult Execute(TContext context)
        {
            var points = score(context);
            if (typeof(TResult) == typeof(double))
                return (TResult)(object)points;

            if (typeof(TResult) == typeof(decimal))
                return (TResult)(object)(decimal)points;

            if (typeof(TResult) == typeof(object))
                return (TResult)(object)points;

            return (TResult)Convert.ChangeType(points, typeof(TResult), CultureInfo.InvariantCulture);
        }
    }
}