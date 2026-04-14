using Lyo.Scientific.Chemistry;

namespace Lyo.Scientific;

/// <summary>Shared helpers for concise <see cref="object.ToString" /> output on scientific domain types (debugger and logs).</summary>
internal static class ScientificModelDisplay
{
    public static string NullProp<T>(T? value, Func<T, string> format)
        where T : class
        => value is null ? "null" : format(value);

    public static string NullProp<T>(T? value, Func<T, string> format)
        where T : struct
        => value is null ? "null" : format(value.Value);

    public static string JoinArrow(IEnumerable<string> reactants, IEnumerable<string> products) => $"{string.Join(" + ", reactants)} -> {string.Join(" + ", products)}";

    public static string BalancedFormula(BalancedReactionComponent c) => c.Coefficient == 1 ? c.Formula : $"{c.Coefficient}{c.Formula}";
}