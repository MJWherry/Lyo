using System.Diagnostics;

namespace Lyo.Mathematics.Models;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct DescriptiveStatisticsResult(
    double Mean,
    double Median,
    double Minimum,
    double Maximum,
    double Variance,
    double StandardDeviation,
    double Sum,
    int Count)
{
    public override string ToString()
        => $"Mean={Mean}, Median={Median}, Minimum={Minimum}, Maximum={Maximum}, Variance={Variance}, StandardDeviation={StandardDeviation}, Sum={Sum}, Count={Count}";
}