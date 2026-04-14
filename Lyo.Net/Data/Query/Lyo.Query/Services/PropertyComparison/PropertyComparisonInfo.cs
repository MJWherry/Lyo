using System.Diagnostics;

namespace Lyo.Query.Services.PropertyComparison;

[DebuggerDisplay("{ToString(),nq}")]
internal record PropertyComparisonInfo(
    string PropertyName,
    Func<object, object?> EntityGetter,
    Func<object, object?> RequestGetter,
    ComparisonStrategy Strategy,
    Type? ConversionType = null)
{
    public override string ToString() => $"{PropertyName} {Strategy.ToString()}";
}